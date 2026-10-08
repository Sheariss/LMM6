using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace Atlas.Presentation.ColorPicker
{
    public enum ColorPickerFormat
    {
        HEX,
        RGB,
        HSL,
        HSV,
        OKLCH
    }

    public sealed class ColorPickerEngine
    {
        private readonly List<Color> presets = new();
        private readonly List<Color> customColors = new();

        public Color Current { get; private set; } = Color.white;

        // Normalized HSV values.
        public float Hue { get; private set; }
        public float Saturation { get; private set; }
        public float Value { get; private set; } = 1f;

        public bool AllowAlpha { get; private set; } = true;
        public ColorPickerFormat Format { get; private set; }
            = ColorPickerFormat.RGB;

        public string Error { get; private set; } = string.Empty;
        public bool AlphaError { get; private set; }

        public bool EditingPresets { get; private set; }
        public bool EditingCustom { get; private set; }
        public int PaletteRevision { get; private set; }

        public IReadOnlyList<Color> Presets => presets.AsReadOnly();
        public IReadOnlyList<Color> CustomColors => customColors.AsReadOnly();

        public void Open(Color initial, bool allowAlpha)
        {
            AllowAlpha = allowAlpha;
            EditingPresets = false;
            EditingCustom = false;
            Hue = 0f;
            Saturation = 0f;

            SetColor(initial);
        }

        public void SetFormat(ColorPickerFormat format)
        {
            Format = format;
            ClearError();
        }

        public void SetColor(Color color)
        {
            color = Normalize(color);

            if (!AllowAlpha)
                color.a = 1f;

            Color.RGBToHSV(color, out float h, out float s, out float v);

            // Hue is undefined for gray. Saturation is undefined at black.
            if (v > 0f)
            {
                Saturation = s;

                if (s > 0f)
                    Hue = h;
            }

            Value = v;
            Current = color;
            ClearError();
        }

        public void SetHue(float degrees)
        {
            Hue = Mathf.Clamp01(degrees / 360f);
            UpdateFromHsv();
        }

        public void SetSaturationValue(float saturation, float value)
        {
            Saturation = Mathf.Clamp01(saturation);
            Value = Mathf.Clamp01(value);
            UpdateFromHsv();
        }

        public void SetAlpha(float percent)
        {
            Color color = Current;
            color.a = AllowAlpha ? Mathf.Clamp01(percent / 100f) : 1f;
            Current = color;
            ClearError();
        }

        public bool TrySetAlpha(string text)
        {
            if (!TryNumber(text, out float alpha) || alpha < 0f || alpha > 100f)
                return Fail("Opacity must be between 0 and 100.", true);

            SetAlpha(alpha);
            return true;
        }

        public bool TrySetChannels(string[] text)
        {
            if (Format == ColorPickerFormat.HEX)
                return TrySetHex(text[0]);

            if (text.Length != 3 ||
                !TryNumber(text[0], out float x) ||
                !TryNumber(text[1], out float y) ||
                !TryNumber(text[2], out float z))
            {
                return Fail("Enter three valid numeric values.");
            }

            switch (Format)
            {
                case ColorPickerFormat.RGB:
                    if (!InRange(x, 255f) ||
                        !InRange(y, 255f) ||
                        !InRange(z, 255f))
                    {
                        return Fail("RGB channels must be between 0 and 255.");
                    }

                    SetColor(new Color(x / 255f, y / 255f, z / 255f, Current.a));
                    return true;

                case ColorPickerFormat.HSV:
                    if (!InRange(x, 360f) ||
                        !InRange(y, 100f) ||
                        !InRange(z, 100f))
                    {
                        return Fail("Use H: 0–360, S: 0–100, V: 0–100.");
                    }

                    Hue = x / 360f;
                    Saturation = y / 100f;
                    Value = z / 100f;
                    UpdateFromHsv();
                    return true;

                case ColorPickerFormat.HSL:
                    if (!InRange(x, 360f) ||
                        !InRange(y, 100f) ||
                        !InRange(z, 100f))
                    {
                        return Fail("Use H: 0–360, S: 0–100, L: 0–100.");
                    }

                    Hue = x / 360f;
                    float lightness = z / 100f;
                    float hslSaturation = y / 100f;

                    Value = lightness +
                        hslSaturation * Mathf.Min(lightness, 1f - lightness);

                    Saturation = Value > 0f
                        ? 2f * (1f - lightness / Value)
                        : hslSaturation;

                    UpdateFromHsv();
                    return true;

                case ColorPickerFormat.OKLCH:
                    if (!InRange(x, 100f) || y < 0f || y > 0.5f ||
                        !InRange(z, 360f))
                    {
                        return Fail("Use L: 0–100, C: 0–0.5, H: 0–360.");
                    }

                    SetColor(FromOklch(x / 100f, y, z, Current.a));
                    return true;

                default:
                    return Fail("Unsupported color format.");
            }
        }

        public string[] GetChannels()
        {
            switch (Format)
            {
                case ColorPickerFormat.HEX:
                    return new[]
                    {
                        "#" + (AllowAlpha
                            ? ColorUtility.ToHtmlStringRGBA(Current)
                            : ColorUtility.ToHtmlStringRGB(Current))
                    };

                case ColorPickerFormat.RGB:
                    return new[]
                    {
                        Number(Current.r * 255f),
                        Number(Current.g * 255f),
                        Number(Current.b * 255f)
                    };

                case ColorPickerFormat.HSV:
                    return new[]
                    {
                        Number(Hue * 360f),
                        Number(Saturation * 100f),
                        Number(Value * 100f)
                    };

                case ColorPickerFormat.HSL:
                    float lightness = Value * (1f - Saturation / 2f);
                    float saturation = lightness > 0f && lightness < 1f
                        ? (Value - lightness) /
                          Mathf.Min(lightness, 1f - lightness)
                        : 0f;

                    return new[]
                    {
                        Number(Hue * 360f),
                        Number(saturation * 100f),
                        Number(lightness * 100f)
                    };

                case ColorPickerFormat.OKLCH:
                    Vector3 lch = ToOklch(Current);

                    return new[]
                    {
                        Number(lch.x * 100f),
                        lch.y.ToString("0.#####", CultureInfo.InvariantCulture),
                        Number(lch.z)
                    };

                default:
                    return Array.Empty<string>();
            }
        }

        public void SetPalettes(
            IEnumerable<Color> presetColors,
            IEnumerable<Color> savedColors)
        {
            // Snapshot first: callers may pass our existing collections.
            var nextPresets = CopyColors(presetColors);
            var nextCustom = CopyColors(savedColors);

            presets.Clear();
            presets.AddRange(nextPresets);
            customColors.Clear();
            customColors.AddRange(nextCustom);
            PaletteRevision++;
        }

        public void ToggleEditing(bool preset)
        {
            if (preset)
                EditingPresets = !EditingPresets;
            else
                EditingCustom = !EditingCustom;
        }

        public bool AddCurrent(bool preset)
        {
            List<Color> target = preset ? presets : customColors;
            string key = ColorUtility.ToHtmlStringRGBA(Current);

            foreach (Color color in target)
            {
                if (ColorUtility.ToHtmlStringRGBA(color) == key)
                    return false;
            }

            target.Add(Current);
            PaletteRevision++;
            return true;
        }

        public bool ActivateSwatch(bool preset, int index)
        {
            List<Color> target = preset ? presets : customColors;

            if (index < 0 || index >= target.Count)
                return false;

            bool editing = preset ? EditingPresets : EditingCustom;

            if (editing)
            {
                target.RemoveAt(index);
                PaletteRevision++;
                return true;
            }

            SetColor(target[index]);
            return false;
        }

        private bool TrySetHex(string text)
        {
            string candidate = (text ?? string.Empty).Trim();

            if (!candidate.StartsWith("#", StringComparison.Ordinal))
                candidate = "#" + candidate;

            bool validLength = candidate.Length == 7 ||
                (AllowAlpha && candidate.Length == 9);

            if (!validLength ||
                !ColorUtility.TryParseHtmlString(candidate, out Color color))
            {
                return Fail(AllowAlpha
                    ? "Use #RRGGBB or #RRGGBBAA."
                    : "Use #RRGGBB.");
            }

            // Six-digit input changes RGB only.
            if (candidate.Length == 7)
                color.a = Current.a;

            SetColor(color);
            return true;
        }

        private void UpdateFromHsv()
        {
            Color color = Color.HSVToRGB(
                Mathf.Repeat(Hue, 1f), Saturation, Value);

            color.a = AllowAlpha ? Current.a : 1f;
            Current = color;
            ClearError();
        }

        private bool Fail(string message, bool alpha = false)
        {
            Error = message;
            AlphaError = alpha;
            return false;
        }

        private void ClearError()
        {
            Error = string.Empty;
            AlphaError = false;
        }

        private static bool TryNumber(string text, out float value)
        {
            bool parsed = float.TryParse(
                text,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out value);

            return parsed && !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static bool InRange(float value, float maximum)
        {
            return value >= 0f && value <= maximum;
        }

        private static string Number(float value)
        {
            return value.ToString("0.##", CultureInfo.InvariantCulture);
        }

        private static float NormalizeChannel(float value)
        {
            return float.IsNaN(value) || float.IsInfinity(value)
                ? 0f
                : Mathf.Clamp01(value);
        }

        private static Color Normalize(Color color)
        {
            return new Color(
                NormalizeChannel(color.r),
                NormalizeChannel(color.g),
                NormalizeChannel(color.b),
                NormalizeChannel(color.a));
        }

        private static List<Color> CopyColors(IEnumerable<Color> source)
        {
            List<Color> result = new();

            if (source != null)
            {
                foreach (Color color in source)
                    result.Add(Normalize(color));
            }

            return result;
        }

        private static float ToLinear(float value)
        {
            return value <= 0.04045f
                ? value / 12.92f
                : Mathf.Pow((value + 0.055f) / 1.055f, 2.4f);
        }

        private static float ToSrgb(float value)
        {
            return value <= 0.0031308f
                ? value * 12.92f
                : 1.055f * Mathf.Pow(value, 1f / 2.4f) - 0.055f;
        }

        private static Vector3 ToOklch(Color color)
        {
            float r = ToLinear(color.r);
            float g = ToLinear(color.g);
            float b = ToLinear(color.b);

            float l = Mathf.Pow(
                0.4122214708f * r + 0.5363325363f * g + 0.0514459929f * b,
                1f / 3f);
            float m = Mathf.Pow(
                0.2119034982f * r + 0.6806995451f * g + 0.1073969566f * b,
                1f / 3f);
            float s = Mathf.Pow(
                0.0883024619f * r + 0.2817188376f * g + 0.6299787005f * b,
                1f / 3f);

            float lightness =
                0.2104542553f * l + 0.7936177850f * m - 0.0040720468f * s;
            float a =
                1.9779984951f * l - 2.4285922050f * m + 0.4505937099f * s;
            float labB =
                0.0259040371f * l + 0.7827717662f * m - 0.8086757660f * s;

            float chroma = Mathf.Sqrt(a * a + labB * labB);
            float hue = chroma < 0.00001f
                ? 0f
                : Mathf.Repeat(Mathf.Atan2(labB, a) * Mathf.Rad2Deg, 360f);

            return new Vector3(lightness, chroma, hue);
        }

        private static Color FromOklch(
            float lightness, float chroma, float hue, float alpha)
        {
            Vector3 linear = OklchToLinear(lightness, chroma, hue);

            if (!InGamut(linear))
            {
                float low = 0f;
                float high = chroma;

                for (int i = 0; i < 24; i++)
                {
                    float middle = (low + high) * 0.5f;

                    if (InGamut(OklchToLinear(lightness, middle, hue)))
                        low = middle;
                    else
                        high = middle;
                }

                linear = OklchToLinear(lightness, low, hue);
            }

            return new Color(
                Mathf.Clamp01(ToSrgb(linear.x)),
                Mathf.Clamp01(ToSrgb(linear.y)),
                Mathf.Clamp01(ToSrgb(linear.z)),
                alpha);
        }

        private static Vector3 OklchToLinear(float lightness, float chroma, float hue)
        {
            float radians = hue * Mathf.Deg2Rad;
            float a = chroma * Mathf.Cos(radians);
            float b = chroma * Mathf.Sin(radians);

            float l = lightness + 0.3963377774f * a + 0.2158037573f * b;
            float m = lightness - 0.1055613458f * a - 0.0638541728f * b;
            float s = lightness - 0.0894841775f * a - 1.2914855480f * b;

            l = l * l * l;
            m = m * m * m;
            s = s * s * s;

            return new Vector3(
                4.0767416621f * l - 3.3077115913f * m + 0.2309699292f * s,
                -1.2684380046f * l + 2.6097574011f * m - 0.3413193965f * s,
                -0.0041960863f * l - 0.7034186147f * m + 1.7076147010f * s);
        }

        private static bool InGamut(Vector3 color)
        {
            const float tolerance = 0.000001f;

            return color.x >= -tolerance && color.x <= 1f + tolerance &&
                   color.y >= -tolerance && color.y <= 1f + tolerance &&
                   color.z >= -tolerance && color.z <= 1f + tolerance;
        }
    }
}