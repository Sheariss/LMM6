using System.Globalization;
using UnityEngine;

namespace Atlas.Presentation.ColorPicker
{
    public sealed class ColorPickerViewState
    {
        public Color Color { get; }
        public float HueDegrees { get; }
        public float Saturation { get; }
        public float Value { get; }
        public float AlphaPercent { get; }
        public string AlphaText { get; }

        public ColorPickerFormat Format { get; }
        public string[] Channels { get; }
        public string Error { get; }
        public bool AlphaError { get; }
        public bool AllowAlpha { get; }

        public bool EditingPresets { get; }
        public bool EditingCustom { get; }
        public int PaletteRevision { get; }
        public Color[] Presets { get; }
        public Color[] CustomColors { get; }

        public ColorPickerViewState(ColorPickerEngine engine)
        {
            Color = engine.Current;
            HueDegrees = engine.Hue * 360f;
            Saturation = engine.Saturation;
            Value = engine.Value;
            AlphaPercent = engine.Current.a * 100f;
            AlphaText = AlphaPercent.ToString(
                "0.##", CultureInfo.InvariantCulture);

            Format = engine.Format;
            Channels = engine.GetChannels();
            Error = engine.Error;
            AlphaError = engine.AlphaError;
            AllowAlpha = engine.AllowAlpha;

            EditingPresets = engine.EditingPresets;
            EditingCustom = engine.EditingCustom;
            PaletteRevision = engine.PaletteRevision;

            Presets = new Color[engine.Presets.Count];
            CustomColors = new Color[engine.CustomColors.Count];

            for (int i = 0; i < Presets.Length; i++)
                Presets[i] = engine.Presets[i];

            for (int i = 0; i < CustomColors.Length; i++)
                CustomColors[i] = engine.CustomColors[i];
        }
    }

    public sealed class ColorPickerViewBuilder
    {
        public ColorPickerViewState Build(ColorPickerEngine engine)
        {
            return new ColorPickerViewState(engine);
        }
    }
}