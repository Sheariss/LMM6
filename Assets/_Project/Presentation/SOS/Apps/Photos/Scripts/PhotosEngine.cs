using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class PhotosEngine
{
    public const float MinimumZoom = 25f;
    public const float MaximumZoom = 400f;
    public const float ZoomStep = 25f;

    private readonly List<Sprite> photos = new();
    private readonly List<int> rotations = new();
    private readonly IReadOnlyList<Sprite> readOnlyPhotos;

    public IReadOnlyList<Sprite> Photos => readOnlyPhotos;

    public int Count => photos.Count;
    public int CurrentIndex { get; private set; } = -1;

    public bool HasPhoto =>
        CurrentIndex >= 0 &&
        CurrentIndex < photos.Count &&
        photos[CurrentIndex] != null;

    public Sprite CurrentPhoto =>
        HasPhoto ? photos[CurrentIndex] : null;

    public int RotationDegrees =>
        HasPhoto ? rotations[CurrentIndex] : 0;

    public bool CanGoPrevious => HasPhoto && CurrentIndex > 0;
    public bool CanGoNext => HasPhoto && CurrentIndex < Count - 1;

    public bool FitToWindow { get; private set; } = true;
    public float ZoomPercent { get; private set; } = 100f;
    public bool FilmstripVisible { get; private set; }

    public PhotosEngine()
    {
        readOnlyPhotos = photos.AsReadOnly();
    }

    public PhotosEngine(IEnumerable<Sprite> source) : this()
    {
        LoadPhotos(source);
    }

    public void LoadPhotos(IEnumerable<Sprite> source)
    {
        // Materialize first so LoadPhotos(Photos) is also safe.
        List<Sprite> loaded = new();

        if (source != null)
        {
            foreach (Sprite photo in source)
            {
                if (photo != null)
                    loaded.Add(photo);
            }
        }

        photos.Clear();
        rotations.Clear();

        foreach (Sprite photo in loaded)
        {
            photos.Add(photo);
            rotations.Add(0);
        }

        CurrentIndex = Count > 0 ? 0 : -1;
        FitToWindow = true;
        ZoomPercent = 100f;
        FilmstripVisible = false;
    }

    public bool SelectPhoto(int index)
    {
        if (index < 0 || index >= Count)
            return false;

        if (index == CurrentIndex)
            return false;

        CurrentIndex = index;

        // Start each newly selected photo fitted to the viewport.
        FitToWindow = true;
        ZoomPercent = 100f;

        return true;
    }

    public bool MovePrevious()
    {
        return CanGoPrevious && SelectPhoto(CurrentIndex - 1);
    }

    public bool MoveNext()
    {
        return CanGoNext && SelectPhoto(CurrentIndex + 1);
    }

    public void RotateClockwise()
    {
        if (!HasPhoto)
            return;

        rotations[CurrentIndex] =
            (rotations[CurrentIndex] + 90) % 360;
    }

    public void SetFitToWindow()
    {
        FitToWindow = true;
    }

    public void SetActualSize()
    {
        SetZoom(100f);
    }

    public void SetZoom(float percentage)
    {
        if (!HasPhoto ||
            float.IsNaN(percentage) ||
            float.IsInfinity(percentage))
        {
            return;
        }

        ZoomPercent = Mathf.Clamp(
            Mathf.Round(percentage),
            MinimumZoom,
            MaximumZoom);

        FitToWindow = false;
    }

    public void ZoomIn(float displayedPercentage)
    {
        SetZoom(displayedPercentage + ZoomStep);
    }

    public void ZoomOut(float displayedPercentage)
    {
        SetZoom(displayedPercentage - ZoomStep);
    }

    public void ToggleFilmstrip()
    {
        if (Count == 0)
            return;

        FilmstripVisible = !FilmstripVisible;
    }
}