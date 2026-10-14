using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class PhotosViewState
{
    public Sprite Photo { get; internal set; }
    public bool HasPhoto { get; internal set; }

    public string PhotoName { get; internal set; }
    public string CounterText { get; internal set; }
    public string FileTypeText { get; internal set; }
    public string DimensionsText { get; internal set; }
    public string FileSizeText { get; internal set; }

    public int CurrentIndex { get; internal set; }
    public int PhotoCount { get; internal set; }
    public int RotationDegrees { get; internal set; }

    public bool CanGoPrevious { get; internal set; }
    public bool CanGoNext { get; internal set; }
    public bool FilmstripVisible { get; internal set; }
    public bool FitToWindow { get; internal set; }

    public float ImageScale { get; internal set; }
    public float DisplayedZoomPercent { get; internal set; }
    public string ZoomText { get; internal set; }
}

public sealed class PhotosViewBuilder
{
    public const string FitChoice = "Fit";

    private static readonly int[] ZoomPresets =
    {
        25, 50, 75, 100, 125, 150, 200, 400
    };

    public PhotosViewState Build(
        PhotosEngine engine,
        Vector2 viewportSize)
    {
        if (engine == null)
            throw new ArgumentNullException(nameof(engine));

        Sprite photo = engine.CurrentPhoto;
        bool hasPhoto = photo != null;

        float scale = hasPhoto
            ? CalculateScale(engine, viewportSize)
            : 1f;

        return new PhotosViewState
        {
            Photo = photo,
            HasPhoto = hasPhoto,
            PhotoName = hasPhoto
                ? GetPhotoName(photo)
                : "No image loaded",

            CounterText = hasPhoto
                ? $"{engine.CurrentIndex + 1} / {engine.Count}"
                : "0 / 0",

            FileTypeText = "—",
            FileSizeText = "—",

            // Metadata describes the source sprite.
            DimensionsText = hasPhoto
                ? $"{Mathf.RoundToInt(photo.rect.width)} × " +
                  $"{Mathf.RoundToInt(photo.rect.height)}"
                : "— × —",

            CurrentIndex = engine.CurrentIndex,
            PhotoCount = engine.Count,
            RotationDegrees = engine.RotationDegrees,
            CanGoPrevious = engine.CanGoPrevious,
            CanGoNext = engine.CanGoNext,
            FilmstripVisible = engine.FilmstripVisible,
            FitToWindow = engine.FitToWindow,
            ImageScale = scale,
            DisplayedZoomPercent = scale * 100f,

            ZoomText = engine.FitToWindow
                ? FitChoice
                : FormatZoom(engine.ZoomPercent)
        };
    }

    public List<string> BuildZoomChoices(string currentChoice)
    {
        List<string> choices = new() { FitChoice };

        foreach (int percentage in ZoomPresets)
            choices.Add(FormatZoom(percentage));

        // Slider values can fall between the fixed presets.
        if (!string.IsNullOrEmpty(currentChoice) &&
            !choices.Contains(currentChoice))
        {
            choices.Add(currentChoice);
        }

        return choices;
    }

    public Button BuildThumbnail(
        Sprite photo,
        int index,
        Action<int> onSelected)
    {
        if (photo == null)
            throw new ArgumentNullException(nameof(photo));

        Button button = new()
        {
            text = string.Empty,
            tooltip = GetPhotoName(photo),
            userData = index
        };

        button.AddToClassList("spacing-reset");
        button.AddToClassList("photos-button");
        button.AddToClassList("photos-thumbnail");

        Image image = new()
        {
            sprite = photo,
            scaleMode = ScaleMode.ScaleToFit,
            pickingMode = PickingMode.Ignore
        };

        image.AddToClassList("photos-thumbnail-image");
        button.Add(image);

        button.clicked += () => onSelected?.Invoke(index);

        return button;
    }

    private static float CalculateScale(
        PhotosEngine engine,
        Vector2 viewportSize)
    {
        if (!engine.FitToWindow)
            return engine.ZoomPercent / 100f;

        Sprite photo = engine.CurrentPhoto;

        if (photo == null ||
            viewportSize.x <= 0f ||
            viewportSize.y <= 0f ||
            float.IsNaN(viewportSize.x) ||
            float.IsNaN(viewportSize.y))
        {
            return 1f;
        }

        float width = photo.rect.width;
        float height = photo.rect.height;

        if (engine.RotationDegrees == 90 ||
            engine.RotationDegrees == 270)
        {
            (width, height) = (height, width);
        }

        if (width <= 0f || height <= 0f)
            return 1f;

        // Fit avoids enlarging small images automatically.
        return Mathf.Min(
            1f,
            viewportSize.x / width,
            viewportSize.y / height);
    }

    private static string GetPhotoName(Sprite photo)
    {
        return string.IsNullOrWhiteSpace(photo.name)
            ? "Unnamed image"
            : photo.name;
    }

    private static string FormatZoom(float percentage)
    {
        return percentage.ToString(
            "0",
            CultureInfo.InvariantCulture) + "%";
    }
}