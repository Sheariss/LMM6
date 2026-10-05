using System;
using QRCoder;
using UnityEngine;

namespace Atlas.Presentation.SOS.BlueScreen
{
    public sealed class QRCodeEngine
    {
        public Texture2D Generate(
            string payload,
            int pixelsPerModule = 8)
        {
            if (string.IsNullOrWhiteSpace(payload))
            {
                throw new ArgumentException(
                    "A QR payload is required.",
                    nameof(payload));
            }

            if (pixelsPerModule < 1 || pixelsPerModule > 16)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(pixelsPerModule));
            }

            using (var generator = new QRCodeGenerator())
            using (var data = generator.CreateQrCode(
                payload,
                QRCodeGenerator.ECCLevel.Q))
            {
                // QRCoder includes the quiet zone in this matrix.
                int moduleCount = data.ModuleMatrix.Count;
                int size = moduleCount * pixelsPerModule;

                var pixels = new Color32[size * size];

                var dark = new Color32(0, 120, 215, 255);
                var light = new Color32(255, 255, 255, 255);

                for (int y = 0; y < size; y++)
                {
                    // QR rows run downward; texture rows run upward.
                    int moduleY = (size - 1 - y) / pixelsPerModule;

                    for (int x = 0; x < size; x++)
                    {
                        int moduleX = x / pixelsPerModule;

                        pixels[y * size + x] =
                            data.ModuleMatrix[moduleY][moduleX]
                                ? dark
                                : light;
                    }
                }

                var texture = new Texture2D(
                    size,
                    size,
                    TextureFormat.RGBA32,
                    false)
                {
                    name = "BlueScreen QR",
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp
                };

                try
                {
                    texture.SetPixels32(pixels);
                    texture.Apply(false, true);
                    return texture;
                }
                catch
                {
                    UnityEngine.Object.Destroy(texture);
                    throw;
                }
            }
        }
    }
}