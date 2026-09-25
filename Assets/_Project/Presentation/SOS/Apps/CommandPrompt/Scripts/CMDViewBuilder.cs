using System.Collections.Generic;
using UnityEngine;

namespace Atlas.Presentation.SOS.CommandPrompt
{
    public sealed class CMDViewState
    {
        public IReadOnlyList<string> OutputLines { get; }
        public Color BackgroundColor { get; }
        public Color ForegroundColor { get; }

        public CMDViewState(
            IReadOnlyList<string> outputLines,
            Color backgroundColor,
            Color foregroundColor)
        {
            OutputLines = outputLines;
            BackgroundColor = backgroundColor;
            ForegroundColor = foregroundColor;
        }
    }

    public sealed class CMDViewBuilder
    {
        public CMDViewState Build(CMDEngine engine)
        {
            if (engine == null)
            {
                return new CMDViewState(
                    new List<string>(),
                    GetColor('0'),
                    GetColor('7'));
            }

            return new CMDViewState(
                outputLines: engine.OutputLines,
                backgroundColor: GetColor(engine.BackgroundColor),
                foregroundColor: GetColor(engine.ForegroundColor));
        }

        private Color GetColor(char value)
        {
            return char.ToUpperInvariant(value) switch
            {
                '0' => new Color32(12, 12, 12, 255),
                '1' => new Color32(0, 55, 218, 255),
                '2' => new Color32(19, 161, 14, 255),
                '3' => new Color32(58, 150, 221, 255),
                '4' => new Color32(197, 15, 31, 255),
                '5' => new Color32(136, 23, 152, 255),
                '6' => new Color32(193, 156, 0, 255),
                '7' => new Color32(204, 204, 204, 255),
                '8' => new Color32(118, 118, 118, 255),
                '9' => new Color32(59, 120, 255, 255),
                'A' => new Color32(22, 198, 12, 255),
                'B' => new Color32(97, 214, 214, 255),
                'C' => new Color32(231, 72, 86, 255),
                'D' => new Color32(180, 0, 158, 255),
                'E' => new Color32(249, 241, 165, 255),
                'F' => new Color32(242, 242, 242, 255),
                _ => new Color32(204, 204, 204, 255)
            };
        }
    }
}