using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Atlas.Presentation.ColorPicker
{
    public sealed class ColorPickerView : IDisposable
    {
        private const string HiddenClass = "color-picker__hidden";
        private const string InvalidClass = "color-picker__field--invalid";
        private const string SelectedClass = "color-picker__swatch--selected";

        private readonly ColorPickerBinder binder;
        private readonly List<Button> presetButtons = new();
        private readonly List<Button> customButtons = new();

        private readonly Texture2D svTexture;
        private readonly Texture2D hueTexture;
        private readonly Texture2D alphaTexture;
        private readonly Texture2D checkerTexture;

        private int paletteRevision = -1;
        private float renderedHue = float.NaN;
        private Color renderedColor;
        private bool hasRenderedColor;

        public event Action<bool, int> SwatchActivated;

        public ColorPickerView(ColorPickerBinder binder)
        {
            this.binder = binder;

            svTexture = CreateTexture(128, 128, "ColorPicker SV");
            hueTexture = CreateTexture(360, 1, "ColorPicker Hue");
            alphaTexture = CreateTexture(128, 1, "ColorPicker Alpha");
            checkerTexture = CreateTexture(360, 28, "ColorPicker Checker");

            Paint(hueTexture, (x, y) =>
                Color.HSVToRGB(x / 359f, 1f, 1f));

            Paint(checkerTexture, (x, y) =>
                ((x / 7 + y / 7) & 1) == 0
                    ? new Color(0.75f, 0.75f, 0.75f)
                    : new Color(0.45f, 0.45f, 0.45f));

            binder.SvGradient.style.backgroundImage = new StyleBackground(svTexture);
            binder.HueGradient.style.backgroundImage = new StyleBackground(hueTexture);
            binder.AlphaGradient.style.backgroundImage = new StyleBackground(alphaTexture);
            binder.AlphaCheckerboard.style.backgroundImage =
                new StyleBackground(checkerTexture);
        }

        public void Show(string title)
        {
            binder.Title.text = title;
            binder.Root.style.display = DisplayStyle.Flex;
        }

        public void Hide()
        {
            binder.Root.style.display = DisplayStyle.None;
        }

        public void Render(ColorPickerViewState state)
        {
            binder.FormatDropdown.SetValueWithoutNotify(state.Format.ToString());
            binder.HueSlider.SetValueWithoutNotify(state.HueDegrees);
            binder.AlphaSlider.SetValueWithoutNotify(state.AlphaPercent);

            binder.SvSelector.style.left = Length.Percent(state.Saturation * 100f);
            binder.SvSelector.style.top = Length.Percent((1f - state.Value) * 100f);

            // The existing UXML wraps the opacity slider in a slider section.
            binder.AlphaSlider.parent.parent.style.display =
                state.AllowAlpha ? DisplayStyle.Flex : DisplayStyle.None;

            binder.AlphaInput.style.display =
                state.AllowAlpha ? DisplayStyle.Flex : DisplayStyle.None;

            bool hasError = !string.IsNullOrEmpty(state.Error);

            foreach (var pair in binder.Groups)
                pair.Value.EnableInClassList(HiddenClass, pair.Key != state.Format);

            foreach (var pair in binder.Fields)
            {
                foreach (TextField field in pair.Value)
                {
                    field.EnableInClassList(
                        InvalidClass,
                        hasError && !state.AlphaError && pair.Key == state.Format);
                }
            }

            // Retain invalid submitted text so the user can correct it.
            if (!hasError || state.AlphaError)
            {
                TextField[] fields = binder.Fields[state.Format];

                for (int i = 0; i < fields.Length; i++)
                    fields[i].SetValueWithoutNotify(state.Channels[i]);
            }

            if (!hasError || !state.AlphaError)
                binder.AlphaInput.SetValueWithoutNotify(state.AlphaText);

            binder.AlphaInput.EnableInClassList(
                InvalidClass, hasError && state.AlphaError);

            binder.InputError.text = state.Error;
            binder.InputError.EnableInClassList(HiddenClass, !hasError);

            RenderGradients(state);
            RenderPalettes(state);
        }

        private void RenderGradients(ColorPickerViewState state)
        {
            if (!Mathf.Approximately(renderedHue, state.HueDegrees) ||
                float.IsNaN(renderedHue))
            {
                renderedHue = state.HueDegrees;

                Paint(svTexture, (x, y) => Color.HSVToRGB(
                    Mathf.Repeat(state.HueDegrees / 360f, 1f),
                    x / 127f,
                    y / 127f));
            }

            Color opaque = state.Color;
            opaque.a = 1f;

            if (!hasRenderedColor || !opaque.Equals(renderedColor))
            {
                renderedColor = opaque;
                hasRenderedColor = true;

                Paint(alphaTexture, (x, y) =>
                    new Color(opaque.r, opaque.g, opaque.b, x / 127f));
            }
        }

        private void RenderPalettes(ColorPickerViewState state)
        {
            if (paletteRevision != state.PaletteRevision)
            {
                paletteRevision = state.PaletteRevision;

                BuildSwatches(
                    binder.PresetSwatches,
                    binder.AddPresetButton,
                    presetButtons,
                    state.Presets,
                    true);

                BuildSwatches(
                    binder.CustomSwatches,
                    binder.AddCustomButton,
                    customButtons,
                    state.CustomColors,
                    false);
            }

            UpdateSwatches(
                presetButtons, state.Presets, state.Color,
                state.AllowAlpha, state.EditingPresets);

            UpdateSwatches(
                customButtons, state.CustomColors, state.Color,
                state.AllowAlpha, state.EditingCustom);

            binder.EditPresetsButton.text = state.EditingPresets ? "Done" : "Edit";
            binder.EditCustomButton.text = state.EditingCustom ? "Done" : "Edit";
            binder.AddPresetButton.SetEnabled(!state.EditingPresets);
            binder.AddCustomButton.SetEnabled(!state.EditingCustom);

            binder.CustomEmptyMessage.EnableInClassList(
                HiddenClass, state.CustomColors.Length > 0);
        }

        private void BuildSwatches(
            VisualElement container,
            Button addButton,
            List<Button> buttons,
            Color[] colors,
            bool preset)
        {
            // Preserve the existing add button and its icon.
            addButton.RemoveFromHierarchy();
            container.Clear();
            buttons.Clear();

            for (int i = 0; i < colors.Length; i++)
            {
                int index = i;
                Button button = new(() => SwatchActivated?.Invoke(preset, index));
                button.AddToClassList("color-picker__swatch");
                button.style.backgroundColor = colors[i];

                buttons.Add(button);
                container.Add(button);
            }

            container.Add(addButton);
        }

        private static void UpdateSwatches(
            List<Button> buttons,
            Color[] colors,
            Color current,
            bool allowAlpha,
            bool editing)
        {
            string selected = ColorKey(current, allowAlpha);

            for (int i = 0; i < buttons.Count; i++)
            {
                string hex = "#" + ColorKey(colors[i], allowAlpha);

                buttons[i].text = editing ? "×" : string.Empty;
                buttons[i].tooltip = editing ? $"Remove {hex}" : hex;
                buttons[i].EnableInClassList(
                    SelectedClass,
                    !editing && ColorKey(colors[i], allowAlpha) == selected);
            }
        }

        private static string ColorKey(Color color, bool allowAlpha)
        {
            return allowAlpha
                ? ColorUtility.ToHtmlStringRGBA(color)
                : ColorUtility.ToHtmlStringRGB(color);
        }

        private static Texture2D CreateTexture(int width, int height, string name)
        {
            return new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = name,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };
        }

        private static void Paint(Texture2D texture, Func<int, int, Color> sample)
        {
            Color[] pixels = new Color[texture.width * texture.height];

            for (int y = 0; y < texture.height; y++)
            {
                for (int x = 0; x < texture.width; x++)
                    pixels[y * texture.width + x] = sample(x, y);
            }

            texture.SetPixels(pixels);
            texture.Apply(false, false);
        }

        public void Dispose()
        {
            binder.SvGradient.style.backgroundImage = StyleKeyword.None;
            binder.HueGradient.style.backgroundImage = StyleKeyword.None;
            binder.AlphaGradient.style.backgroundImage = StyleKeyword.None;
            binder.AlphaCheckerboard.style.backgroundImage = StyleKeyword.None;

            DestroyTexture(svTexture);
            DestroyTexture(hueTexture);
            DestroyTexture(alphaTexture);
            DestroyTexture(checkerTexture);

            SwatchActivated = null;
        }

        private static void DestroyTexture(Texture2D texture)
        {
            if (Application.isPlaying)
                UnityEngine.Object.Destroy(texture);
            else
                UnityEngine.Object.DestroyImmediate(texture);
        }
    }
}