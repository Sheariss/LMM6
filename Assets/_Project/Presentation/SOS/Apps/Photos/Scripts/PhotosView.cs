using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class PhotosView
{
    private readonly PhotosBinder binder;
    private readonly List<Button> thumbnails = new();

    private Vector2 panOffset;
    private Vector2 panLimits;

    private Sprite lastPhoto;
    private int lastPhotoIndex = -1;
    private int lastRotation;

    public bool CanPan =>
    panLimits.x > 0f || panLimits.y > 0f;

    public Vector2 PanOffset => panOffset;

    public bool MoreMenuVisible { get; private set; }

    public PhotosView(PhotosBinder binder)
    {
        this.binder = binder ??
            throw new ArgumentNullException(nameof(binder));

        ConfigureUnavailableActions();

        SetMoreMenuVisible(false);
        SetVisible(binder.OpenWithMenu, false);
        SetVisible(binder.SetAsMenu, false);
    }

    public void Render(PhotosViewState state)
    {
        if (state == null)
            throw new ArgumentNullException(nameof(state));

        binder.PhotoImage.sprite = state.Photo;
        binder.PhotoNameLabel.text = state.PhotoName;
        binder.CounterLabel.text = state.CounterText;

        binder.FileTypeLabel.text = state.FileTypeText;
        binder.DimensionsLabel.text = state.DimensionsText;
        binder.FileSizeLabel.text = state.FileSizeText;

        binder.PreviousButton.SetEnabled(state.CanGoPrevious);
        binder.NextButton.SetEnabled(state.CanGoNext);
        binder.RotateButton.SetEnabled(state.HasPhoto);

        binder.FilmstripButton.SetEnabled(state.PhotoCount > 0);
        binder.FilmstripButton.tooltip = state.FilmstripVisible
            ? "Hide filmstrip"
            : "Show filmstrip";

        SetVisible(
            binder.FilmstripContainer,
            state.FilmstripVisible && state.PhotoCount > 0);

        binder.ActualSizeButton.SetEnabled(state.HasPhoto);
        binder.ZoomDropdown.SetEnabled(state.HasPhoto);
        binder.ZoomSlider.SetEnabled(state.HasPhoto);

        binder.ZoomOutButton.SetEnabled(
            state.HasPhoto &&
            state.DisplayedZoomPercent > PhotosEngine.MinimumZoom);

        binder.ZoomInButton.SetEnabled(
            state.HasPhoto &&
            state.DisplayedZoomPercent < PhotosEngine.MaximumZoom);

        binder.ZoomDropdown.SetValueWithoutNotify(state.ZoomText);

        binder.ZoomSlider.SetValueWithoutNotify(
            Mathf.Clamp(
                state.DisplayedZoomPercent,
                PhotosEngine.MinimumZoom,
                PhotosEngine.MaximumZoom));

        binder.ZoomDropdown.tooltip = state.FitToWindow
            ? $"Fit to window ({state.DisplayedZoomPercent:0.#}%)"
            : $"Zoom: {state.DisplayedZoomPercent:0}%";

        RenderImage(state);
        RenderThumbnailSelection(state.CurrentIndex);
    }

    public void AddThumbnail(Button thumbnail)
    {
        if (thumbnail == null)
            throw new ArgumentNullException(nameof(thumbnail));

        thumbnails.Add(thumbnail);
        binder.FilmstripItems.Add(thumbnail);
    }

    public void ClearFilmstrip()
    {
        binder.FilmstripItems.Clear();
        thumbnails.Clear();
    }

    public void SetMoreMenuVisible(bool visible)
    {
        MoreMenuVisible = visible;
        SetVisible(binder.MoreMenu, visible);
    }

    private void RenderImage(PhotosViewState state)
    {
        Image image = binder.PhotoImage;

        SetVisible(image, state.HasPhoto);

        bool resetPan =
            lastPhoto != state.Photo ||
            lastPhotoIndex != state.CurrentIndex ||
            lastRotation != state.RotationDegrees ||
            state.FitToWindow;

        lastPhoto = state.Photo;
        lastPhotoIndex = state.CurrentIndex;
        lastRotation = state.RotationDegrees;

        if (resetPan)
            panOffset = Vector2.zero;

        if (!state.HasPhoto)
        {
            panOffset = Vector2.zero;
            panLimits = Vector2.zero;
            return;
        }

        Vector2 viewport = binder.PhotoViewport.contentRect.size;

        if (viewport.x <= 0f ||
            viewport.y <= 0f ||
            float.IsNaN(viewport.x) ||
            float.IsNaN(viewport.y))
        {
            panLimits = Vector2.zero;
            return;
        }

        float width = state.Photo.rect.width * state.ImageScale;
        float height = state.Photo.rect.height * state.ImageScale;

        bool quarterTurn =
            state.RotationDegrees == 90 ||
            state.RotationDegrees == 270;

        float displayedWidth = quarterTurn ? height : width;
        float displayedHeight = quarterTurn ? width : height;

        panLimits = new Vector2(
            Mathf.Max(0f, (displayedWidth - viewport.x) * 0.5f),
            Mathf.Max(0f, (displayedHeight - viewport.y) * 0.5f));

        panOffset = new Vector2(
            Mathf.Clamp(panOffset.x, -panLimits.x, panLimits.x),
            Mathf.Clamp(panOffset.y, -panLimits.y, panLimits.y));

        image.scaleMode = ScaleMode.ScaleToFit;

        image.style.position = Position.Absolute;
        image.style.right = StyleKeyword.Auto;
        image.style.bottom = StyleKeyword.Auto;

        image.style.width = width;
        image.style.height = height;

        // Use the calculated dimensions immediately; resolvedStyle may
        // still contain the previous size until the next layout pass.
        image.style.left =
            (viewport.x - width) * 0.5f + panOffset.x;

        image.style.top =
            (viewport.y - height) * 0.5f + panOffset.y;

        image.style.transformOrigin = new TransformOrigin(
            Length.Percent(50f),
            Length.Percent(50f),
            0f);

        image.style.rotate = new Rotate(
            new Angle(state.RotationDegrees, AngleUnit.Degree));
    }

    private void RenderThumbnailSelection(int selectedIndex)
    {
        for (int index = 0; index < thumbnails.Count; index++)
        {
            thumbnails[index].EnableInClassList(
                "photos-thumbnail-selected",
                index == selectedIndex);
        }
    }

    private void ConfigureUnavailableActions()
    {
        Disable(binder.EditButton, "Editing is not available yet");
        Disable(binder.DeleteButton, "Photo deletion is not available yet");
        Disable(binder.ShareButton, "Sharing is not available yet");
        Disable(binder.OpenWithButton, "Open with is not available yet");

        Disable(binder.InfoButton, "File information is not available yet");
        Disable(binder.ExtractTextButton, "Text extraction is not available yet");
        Disable(binder.FullscreenButton, "Full screen is not available yet");

        Disable(binder.SaveAsButton, "Save as is not available yet");
        Disable(binder.CopyButton, "Image copying is not available yet");
        Disable(binder.ResizeButton, "Image resizing is not available yet");
        Disable(binder.SetAsButton, "Set as is not available yet");
        Disable(binder.SettingsButton, "Settings are not available yet");

        Disable(
            binder.ChooseAnotherAppButton,
            "App selection is not available yet");

        Disable(
            binder.SetAsBackgroundButton,
            "Setting the background is not available yet");

        Disable(
            binder.SetAsLockScreenButton,
            "Setting the lock screen is not available yet");
    }

    private static void Disable(Button button, string tooltip)
    {
        button.SetEnabled(false);
        button.tooltip = tooltip;
    }

    private static void SetVisible(
        VisualElement element,
        bool visible)
    {
        element.EnableInClassList("photos-hidden", !visible);
    }

    public void SetPanOffset(Vector2 offset)
    {
        panOffset = new Vector2(
            Mathf.Clamp(offset.x, -panLimits.x, panLimits.x),
            Mathf.Clamp(offset.y, -panLimits.y, panLimits.y));

        PositionImage();
    }

    public void ResetPan()
    {
        panOffset = Vector2.zero;
        PositionImage();
    }

    private void PositionImage()
    {
        Image image = binder.PhotoImage;
        Vector2 viewport = binder.PhotoViewport.contentRect.size;

        float width = image.resolvedStyle.width;
        float height = image.resolvedStyle.height;

        if (float.IsNaN(width) || float.IsNaN(height))
            return;

        image.style.left =
            (viewport.x - width) * 0.5f + panOffset.x;

        image.style.top =
            (viewport.y - height) * 0.5f + panOffset.y;
    }
}