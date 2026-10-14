using System;
using System.Collections.Generic;
using System.Globalization;
using Atlas.Presentation.SOS.Apps;
using Atlas.SOS.FileSystem;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class PhotosController :
    AppContentController,
    IFileContentReceiver
{
    [Header("File Content")]
    [SerializeField] private ImageContentCatalog imageContentCatalog;

    private PhotosBinder binder;
    private PhotosEngine engine;
    private PhotosViewBuilder viewBuilder;
    private PhotosView view;

    private int panPointerId = -1;
    private Vector2 panStartPointer;
    private Vector2 panStartOffset;

    // Matches the engine's sprite collection by index.
    private readonly List<FileSystemFileDefinition> openedFiles = new();

    private static readonly string[] SizeUnits =
    {
        "B", "KB", "MB", "GB", "TB", "PB", "EB"
    };

    // -------------------- INITIALIZATION --------------------

    protected override void OnInitialize()
    {
        VisualElement windowRoot = FindWindowRoot(InstanceRoot);

        binder = new PhotosBinder(windowRoot);

        if (!binder.IsValid)
        {
            throw new InvalidOperationException(
                "Photos UI is missing required elements.");
        }

        openedFiles.Clear();

        engine = new PhotosEngine();
        viewBuilder = new PhotosViewBuilder();
        view = new PhotosView(binder);

        binder.Root.focusable = true;

        binder.ZoomSlider.lowValue = PhotosEngine.MinimumZoom;
        binder.ZoomSlider.highValue = PhotosEngine.MaximumZoom;

        RegisterCallbacks();

        RebuildFilmstrip();
        Refresh();
    }

    protected override void OnShutdown()
    {
        // AppContentController has already unregistered callbacks.
        view?.ClearFilmstrip();
        openedFiles.Clear();

        binder = null;
        engine = null;
        viewBuilder = null;
        view = null;
    }

    // -------------------- FILE OPENING --------------------

    public bool TryOpenFile(
        FileOpenRequest request,
        out string error)
    {
        error = null;

        if (!IsInitialized ||
            engine == null ||
            view == null ||
            binder == null ||
            viewBuilder == null)
        {
            error = "Photos is not initialized.";
            return false;
        }

        if (request?.File == null)
        {
            error = "No image file was provided.";
            return false;
        }

        FileSystemFileDefinition selectedFile = request.File;

        if (selectedFile.FileType != FileSystemFileType.Image)
        {
            error = "Photos can only open image files.";
            return false;
        }

        if (selectedFile.Hidden ||
            string.IsNullOrWhiteSpace(selectedFile.Id))
        {
            error = "The selected image file is invalid or hidden.";
            return false;
        }

        if (imageContentCatalog == null)
        {
            error = "Photos has no ImageContentCatalog assigned.";
            return false;
        }

        // Validate before replacing an existing photo collection.
        if (!imageContentCatalog.TryResolve(
            selectedFile.ContentId,
            out Sprite selectedImage,
            out error))
        {
            return false;
        }

        List<Sprite> images = new();
        List<FileSystemFileDefinition> files = new();
        HashSet<string> seenIds = new(StringComparer.Ordinal);

        int selectedIndex = -1;

        foreach (FileSystemFileDefinition related in request.RelatedFiles)
        {
            if (related == null ||
                related.Hidden ||
                related.FileType != FileSystemFileType.Image ||
                string.IsNullOrWhiteSpace(related.Id) ||
                !seenIds.Add(related.Id))
            {
                continue;
            }

            bool isSelected = string.Equals(
                related.Id,
                selectedFile.Id,
                StringComparison.Ordinal);

            FileSystemFileDefinition file =
                isSelected ? selectedFile : related;

            Sprite image;

            if (isSelected)
            {
                image = selectedImage;
            }
            else if (!imageContentCatalog.TryResolve(
                file.ContentId,
                out image,
                out _))
            {
                // Unresolved neighboring files are omitted.
                // The selected image can still open.
                continue;
            }

            if (isSelected)
                selectedIndex = images.Count;

            images.Add(image);
            files.Add(file);
        }

        // Ensure the selected image is included even when no
        // neighboring files were supplied.
        if (selectedIndex < 0)
        {
            selectedIndex = images.Count;

            images.Add(selectedImage);
            files.Add(selectedFile);
        }

        engine.LoadPhotos(images);
        engine.SelectPhoto(selectedIndex);

        openedFiles.Clear();
        openedFiles.AddRange(files);

        view.SetMoreMenuVisible(false);

        RebuildFilmstrip();
        Refresh();

        return true;
    }

    // -------------------- CALLBACK REGISTRATION --------------------

    private void RegisterCallbacks()
    {
        BindClick(binder.PreviousButton, OnPreviousPressed);
        BindClick(binder.NextButton, OnNextPressed);
        BindClick(binder.RotateButton, OnRotatePressed);
        BindClick(binder.FilmstripButton, OnFilmstripPressed);
        BindClick(binder.ActualSizeButton, OnActualSizePressed);
        BindClick(binder.ZoomOutButton, OnZoomOutPressed);
        BindClick(binder.ZoomInButton, OnZoomInPressed);
        BindClick(binder.MoreButton, OnMorePressed);

        BindCallback<ChangeEvent<float>>(
            binder.ZoomSlider,
            OnZoomSliderChanged);

        BindCallback<ChangeEvent<string>>(
            binder.ZoomDropdown,
            OnZoomDropdownChanged);

        BindCallback<GeometryChangedEvent>(
            binder.PhotoViewport,
            OnViewportGeometryChanged);

        BindCallback<KeyDownEvent>(
            binder.Root,
            OnKeyDown);

        BindCallback<PointerDownEvent>(
            binder.Root,
            OnPointerDown,
            TrickleDown.TrickleDown);

        BindCallback<PointerDownEvent>(
            binder.PhotoViewport, OnPanPointerDown);

        BindCallback<PointerMoveEvent>(
            binder.PhotoViewport, OnPanPointerMove);

        BindCallback<PointerUpEvent>(
            binder.PhotoViewport, OnPanPointerUp);

        BindCallback<PointerCancelEvent>(
            binder.PhotoViewport, OnPanPointerCancel);

        BindCallback<PointerCaptureOutEvent>(
            binder.PhotoViewport, OnPanPointerCaptureOut);

        BindCallback<DetachFromPanelEvent>(
            binder.PhotoViewport, OnPanViewportDetached);
    }

    // -------------------- NAVIGATION --------------------

    private void OnPreviousPressed()
    {
        if (engine.MovePrevious())
            Refresh();
    }

    private void OnNextPressed()
    {
        if (engine.MoveNext())
            Refresh();
    }

    private void OnPhotoSelected(int index)
    {
        if (engine.SelectPhoto(index))
            Refresh();
    }

    // -------------------- PHOTO ACTIONS --------------------

    private void OnRotatePressed()
    {
        engine.RotateClockwise();
        Refresh();
    }

    private void OnFilmstripPressed()
    {
        engine.ToggleFilmstrip();
        Refresh();
    }

    private void OnActualSizePressed()
    {
        engine.SetActualSize();
        Refresh();
    }

    private void OnMorePressed()
    {
        view.SetMoreMenuVisible(!view.MoreMenuVisible);
    }

    // -------------------- ZOOM --------------------

    private void OnZoomOutPressed()
    {
        engine.ZoomOut(GetDisplayedZoomPercent());
        Refresh();
    }

    private void OnZoomInPressed()
    {
        engine.ZoomIn(GetDisplayedZoomPercent());
        Refresh();
    }

    private void OnZoomSliderChanged(ChangeEvent<float> evt)
    {
        engine.SetZoom(evt.newValue);
        Refresh();
    }

    private void OnZoomDropdownChanged(ChangeEvent<string> evt)
    {
        if (evt.newValue == PhotosViewBuilder.FitChoice)
        {
            engine.SetFitToWindow();
            Refresh();
            return;
        }

        string text = evt.newValue?
            .Trim()
            .TrimEnd('%');

        if (float.TryParse(
            text,
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out float percentage))
        {
            engine.SetZoom(percentage);
        }

        Refresh();
    }

    private float GetDisplayedZoomPercent()
    {
        return BuildState().DisplayedZoomPercent;
    }

    // -------------------- VIEWPORT --------------------

    private void OnViewportGeometryChanged(GeometryChangedEvent evt)
    {
        if (evt.oldRect.size != evt.newRect.size)
            Refresh();
    }

    // -------------------- POINTER INPUT --------------------

    private void OnPointerDown(PointerDownEvent evt)
    {
        if (binder == null || view == null)
            return;

        VisualElement target = evt.target as VisualElement;

        if (target == null)
            return;

        if (IsWithin(target, binder.MoreButton) ||
            IsWithin(target, binder.MoreMenu))
        {
            return;
        }

        view.SetMoreMenuVisible(false);
    }

    // -------------------- KEYBOARD INPUT --------------------

    private void OnKeyDown(KeyDownEvent evt)
    {
        if (engine == null || view == null)
            return;

        if (evt.ctrlKey || evt.commandKey || evt.altKey)
            return;

        // Preserve field navigation and text input.
        if (IsFieldTarget(evt.target as VisualElement))
            return;

        switch (evt.keyCode)
        {
            case KeyCode.LeftArrow:
                OnPreviousPressed();
                break;

            case KeyCode.RightArrow:
                OnNextPressed();
                break;

            case KeyCode.Escape:
                if (view.MoreMenuVisible)
                {
                    view.SetMoreMenuVisible(false);
                }
                else
                {
                    engine.SetFitToWindow();
                    Refresh();
                }

                break;

            default:
                return;
        }

        evt.StopPropagation();
    }

    // -------------------- FILMSTRIP --------------------

    private void RebuildFilmstrip()
    {
        view.ClearFilmstrip();

        for (int index = 0; index < engine.Count; index++)
        {
            Button thumbnail = viewBuilder.BuildThumbnail(
                engine.Photos[index],
                index,
                OnPhotoSelected);

            if (index < openedFiles.Count &&
                !string.IsNullOrWhiteSpace(openedFiles[index].Name))
            {
                thumbnail.tooltip = openedFiles[index].Name;
            }

            view.AddThumbnail(thumbnail);
        }
    }

    // -------------------- DISPLAY STATE --------------------

    private PhotosViewState BuildState()
    {
        PhotosViewState state = viewBuilder.Build(
            engine,
            binder.PhotoViewport.contentRect.size);

        // Supply filesystem metadata using the current builder API.
        state.FileTypeText = "-";
        state.FileSizeText = "-";

        if (!state.HasPhoto)
        {
            state.DimensionsText = "- x -";
            return state;
        }

        state.DimensionsText =
            $"{Mathf.RoundToInt(state.Photo.rect.width)} x " +
            $"{Mathf.RoundToInt(state.Photo.rect.height)}";

        if (state.CurrentIndex < 0 ||
            state.CurrentIndex >= openedFiles.Count)
        {
            return state;
        }

        FileSystemFileDefinition file =
            openedFiles[state.CurrentIndex];

        if (!string.IsNullOrWhiteSpace(file.Name))
            state.PhotoName = file.Name;

        state.FileTypeText = GetFileTypeText(file.Name);
        state.FileSizeText = FormatFileSize(file.Size);

        return state;
    }

    private void Refresh()
    {
        if (engine == null ||
            binder == null ||
            view == null ||
            viewBuilder == null)
        {
            return;
        }

        PhotosViewState state = BuildState();

        binder.ZoomDropdown.choices =
            viewBuilder.BuildZoomChoices(state.ZoomText);

        view.Render(state);
    }

    // -------------------- METADATA FORMATTING --------------------

    private static string GetFileTypeText(string fileName)
    {
        string extension = System.IO.Path.GetExtension(
            fileName ?? string.Empty);

        return string.IsNullOrWhiteSpace(extension)
            ? "Image"
            : extension.TrimStart('.').ToUpperInvariant();
    }

    private static string FormatFileSize(long bytes)
    {
        if (bytes < 0)
            return "-";

        double value = bytes;
        int unit = 0;

        while (value >= 1024d && unit < SizeUnits.Length - 1)
        {
            value /= 1024d;
            unit++;
        }

        string number = value.ToString(
            unit == 0 ? "0" : "0.#",
            CultureInfo.InvariantCulture);

        return $"{number} {SizeUnits[unit]}";
    }

    // -------------------- UI HELPERS --------------------

    private static VisualElement FindWindowRoot(VisualElement element)
    {
        for (VisualElement current = element;
             current != null;
             current = current.parent)
        {
            if (current.name == "WindowRoot")
                return current;
        }

        throw new InvalidOperationException(
            "Photos must be initialized inside WindowRoot.");
    }

    private static bool IsWithin(
        VisualElement target,
        VisualElement container)
    {
        return target != null &&
               container != null &&
               (target == container || container.Contains(target));
    }

    private static bool IsFieldTarget(VisualElement target)
    {
        for (VisualElement current = target;
             current != null;
             current = current.parent)
        {
            if (current is TextField ||
                current is DropdownField ||
                current is Slider)
            {
                return true;
            }
        }

        return false;
    }

    private void OnPanPointerDown(PointerDownEvent evt)
    {
        if (evt.button != 0 ||
            panPointerId >= 0 ||
            view == null ||
            !view.CanPan)
        {
            return;
        }

        // Navigation buttons must remain clickable.
        for (VisualElement element = evt.target as VisualElement;
             element != null && element != binder.PhotoViewport;
             element = element.parent)
        {
            if (element is Button)
                return;
        }

        panPointerId = evt.pointerId;

        panStartPointer = binder.PhotoViewport.WorldToLocal(
            (Vector2)evt.position);

        panStartOffset = view.PanOffset;

        binder.PhotoViewport.CapturePointer(panPointerId);
        evt.StopPropagation();
    }

    private void OnPanPointerMove(PointerMoveEvent evt)
    {
        if (evt.pointerId != panPointerId || panPointerId < 0)
            return;

        Vector2 pointer = binder.PhotoViewport.WorldToLocal(
            (Vector2)evt.position);

        view.SetPanOffset(
            panStartOffset + pointer - panStartPointer);

        evt.StopPropagation();
    }

    private void OnPanPointerUp(PointerUpEvent evt)
    {
        if (evt.pointerId != panPointerId || panPointerId < 0)
            return;

        EndPan();
        evt.StopPropagation();
    }

    private void OnPanPointerCancel(PointerCancelEvent evt)
    {
        if (evt.pointerId == panPointerId)
            EndPan();
    }

    private void OnPanPointerCaptureOut(PointerCaptureOutEvent evt)
    {
        if (evt.pointerId == panPointerId)
            panPointerId = -1;
    }

    private void OnPanViewportDetached(DetachFromPanelEvent evt)
    {
        EndPan();
    }

    private void EndPan()
    {
        int pointerId = panPointerId;
        panPointerId = -1;

        if (pointerId >= 0 &&
            binder != null &&
            binder.PhotoViewport.HasPointerCapture(pointerId))
        {
            binder.PhotoViewport.ReleasePointer(pointerId);
        }
    }
}