using System;
using System.Collections.Generic;
using Atlas.SOS.FileSystem;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class PdfViewerView : IDisposable
{
    public readonly struct Anchor
    {
        public int Page { get; }
        public Vector2 NormalizedPoint { get; }
        public Vector2 ViewportPoint { get; }

        public Anchor(
            int page,
            Vector2 normalizedPoint,
            Vector2 viewportPoint)
        {
            Page = page;
            NormalizedPoint = normalizedPoint;
            ViewportPoint = viewportPoint;
        }
    }

    private readonly PdfViewerBinder binder;
    private readonly PdfViewerViewBuilder builder = new();
    private readonly Action<int> onPageSelected;

    private readonly List<VisualElement> pages = new();
    private readonly List<Image> images = new();
    private readonly List<Button> thumbnails = new();

    private PdfContentEntry document;

    private float lastScale = -1f;
    private int lastRotation = -1;
    private bool lastPresentation;
    private int lastPresentationPage = -1;

    private IVisualElementScheduledItem positioningTask;
    private Action applyPosition;
    private Vector2 previousContentSize;
    private Vector2 previousViewportSize;
    private int stableFrames;
    private int positioningAttempts;
    private bool disposed;

    public bool IsRebuilding { get; private set; }
    public bool IsPositioning { get; private set; }
    public bool HasPendingJump => applyPosition != null;

    public float CurrentScale { get; private set; } = 1f;

    public Vector2 ScrollOffset =>
        binder.DocumentScrollView.scrollOffset;

    public bool CanPan =>
        document != null &&
        (binder.DocumentScrollView.horizontalScroller.highValue > 0f ||
         binder.DocumentScrollView.verticalScroller.highValue > 0f);

    public PdfViewerView(
        PdfViewerBinder binder,
        Action<int> onPageSelected)
    {
        this.binder = binder ??
            throw new ArgumentNullException(nameof(binder));

        this.onPageSelected = onPageSelected;

        binder.DocumentOutline.selectionType = SelectionType.Single;
        binder.DocumentOutline.fixedItemHeight = 32f;

        binder.DocumentOutline.makeItem = () =>
        {
            Label label = new();
            label.AddToClassList("pdf-outline-label");
            return label;
        };

        binder.DocumentOutline.bindItem = (element, index) =>
        {
            PdfOutlineContent item = binder.DocumentOutline
                .GetItemDataForIndex<PdfOutlineContent>(index);

            Label label = (Label)element;
            label.text = item.Title;
            label.tooltip = $"Go to page {item.PageNumber}";
        };
    }

    public void Render(
        PdfViewerEngine engine,
        bool fullscreen,
        bool presentation,
        bool canFullscreen)
    {
        if (disposed)
            return;

        if (!ReferenceEquals(document, engine.Document))
            RebuildDocument(engine.Document);

        bool hasDocument = engine.HasDocument;

        binder.Root.EnableInClassList(
            "pdf-fullscreen", fullscreen);

        binder.Root.EnableInClassList(
            "pdf-presentation", presentation);

        binder.WindowTitle.text = "PDF Viewer";

        binder.DocumentTitle.text = hasDocument
            ? engine.DocumentTitle
            : "PDF Viewer";

        binder.DocumentTitle.tooltip = binder.WindowTitle.text;

        if (!IsEditing(binder.PageNumberField))
        {
            binder.PageNumberField.SetValueWithoutNotify(
                hasDocument ? engine.CurrentIndex + 1 : 0);
        }

        binder.PageCountLabel.text = $"of {engine.PageCount}";

        binder.PageNumberField.SetEnabled(hasDocument);

        binder.Button("PreviousPageButton").SetEnabled(
            engine.CanGoPrevious);

        binder.Button("NextPageButton").SetEnabled(
            engine.CanGoNext);

        binder.Button("FirstPageButton").SetEnabled(
            engine.CanGoPrevious);

        binder.Button("LastPageButton").SetEnabled(
            engine.CanGoNext);

        binder.Button("CurrentPageButton").SetEnabled(hasDocument);

        binder.Button("SidebarToggleButton").SetEnabled(hasDocument);
        binder.Button("SearchButton").SetEnabled(hasDocument);
        binder.Button("MoreOptionsButton").SetEnabled(hasDocument);

        binder.Button("ViewerFullscreenButton").SetEnabled(
            hasDocument && canFullscreen);

        binder.Button("PresentationModeButton").SetEnabled(
            hasDocument && canFullscreen);

        binder.Button("RotateClockwiseButton").SetEnabled(hasDocument);
        binder.Button("RotateCounterclockwiseButton").SetEnabled(
            hasDocument);

        binder.Button("HandToolButton").SetEnabled(hasDocument);

        bool sidebarVisible =
            hasDocument && engine.SidebarVisible && !presentation;

        Show(binder.Sidebar, sidebarVisible);
        Show(binder.SidebarResizeHandle, sidebarVisible);

        Show(
            binder.ThumbnailsPanel,
            !engine.OutlineSelected);

        Show(
            binder.OutlinePanel,
            engine.OutlineSelected);

        binder.SidebarTitle.text = engine.OutlineSelected
            ? "Document outline"
            : "Pages";

        Select(
            binder.Button("SidebarToggleButton"),
            engine.SidebarVisible);

        Select(
            binder.Button("ThumbnailsTabButton"),
            !engine.OutlineSelected);

        Select(
            binder.Button("OutlineTabButton"),
            engine.OutlineSelected);

        Select(
            binder.Button("HandToolButton"),
            engine.HandToolSelected);

        Select(
            binder.Button("ViewerFullscreenButton"),
            fullscreen);

        Select(
            binder.Button("MoreOptionsButton"),
            engine.MoreMenuVisible);

        bool hasOutline =
            document?.Outline != null &&
            document.Outline.Count > 0;

        Show(binder.DocumentOutline, hasOutline);
        Show(binder.OutlineEmptyLabel, !hasOutline);

        Show(
            binder.SearchBar,
            hasDocument && engine.SearchVisible && !presentation);

        if (!IsEditing(binder.SearchField))
        {
            binder.SearchField.SetValueWithoutNotify(
                engine.SearchQuery);
        }

        int matches = engine.SearchMatches.Count;

        binder.SearchResultLabel.text = matches > 0
            ? $"{engine.CurrentMatchIndex + 1} of {matches} matches"
            : string.IsNullOrWhiteSpace(engine.SearchQuery)
                ? "0 matches"
                : "No matches";

        binder.SearchResultLabel.tooltip =
            "Results navigate to matching pages. " +
            "Word highlighting requires positioned text data.";

        binder.Button("PreviousMatchButton").SetEnabled(matches > 0);
        binder.Button("NextMatchButton").SetEnabled(matches > 0);

        Show(
            binder.MoreOptionsMenu,
            hasDocument && engine.MoreMenuVisible && !presentation);

        Show(binder.EmptyDocumentLabel, !hasDocument);
        Show(binder.DocumentScrollView, hasDocument);
        Show(binder.PresentationControls, presentation);

        binder.Button("PresentationPreviousButton").SetEnabled(
            engine.CanGoPrevious);

        binder.Button("PresentationNextButton").SetEnabled(
            engine.CanGoNext);

        binder.DocumentViewport.EnableInClassList(
            "pdf-hand-tool",
            engine.HandToolSelected && !presentation);

        binder.DocumentViewport.tooltip = presentation
            ? "Use arrow keys to change pages. Escape exits."
            : "Drag with the middle mouse button, " +
              "or use left-drag while the Hand tool is enabled.";

        if (!hasDocument)
        {
            binder.ZoomField.SetValueWithoutNotify("100%");
            binder.ZoomField.SetEnabled(false);
            binder.FitDropdown.SetEnabled(false);
            binder.Button("ZoomOutButton").SetEnabled(false);
            binder.Button("ZoomInButton").SetEnabled(false);
            return;
        }

        CurrentScale = CalculateScale(engine, presentation);

        float percentage = CurrentScale * 100f;

        if (!IsEditing(binder.ZoomField))
        {
            binder.ZoomField.SetValueWithoutNotify(
                PdfViewerViewBuilder.FormatZoom(percentage));
        }

        binder.ZoomField.SetEnabled(!presentation);
        binder.FitDropdown.SetEnabled(!presentation);

        binder.FitDropdown.SetValueWithoutNotify(
            engine.ZoomMode switch
            {
                PdfZoomMode.FitPage => "Fit page",
                PdfZoomMode.FitWidth => "Fit width",
                PdfZoomMode.Automatic => "Automatic",
                _ => "Actual size"
            });

        binder.Button("ZoomOutButton").SetEnabled(
            !presentation &&
            percentage > PdfViewerEngine.MinimumZoom + 0.01f);

        binder.Button("ZoomInButton").SetEnabled(
            !presentation &&
            percentage < PdfViewerEngine.MaximumZoom - 0.01f);

        for (int index = 0; index < thumbnails.Count; index++)
            Select(thumbnails[index], index == engine.CurrentIndex);

        bool needsLayout =
            !Mathf.Approximately(lastScale, CurrentScale) ||
            lastRotation != engine.RotationDegrees ||
            lastPresentation != presentation ||
            (presentation &&
             lastPresentationPage != engine.CurrentIndex);

        if (needsLayout)
            ApplyPageLayout(engine, presentation);
    }

    public Anchor CaptureAnchor(Vector2? worldPoint = null)
    {
        if (pages.Count == 0)
            return new Anchor(-1, Vector2.zero, Vector2.zero);

        VisualElement viewport =
            binder.DocumentScrollView.contentViewport;

        Vector2 world = worldPoint ?? viewport.worldBound.center;

        int selectedPage = -1;
        float nearestDistance = float.PositiveInfinity;

        for (int index = 0; index < pages.Count; index++)
        {
            if (pages[index].resolvedStyle.display == DisplayStyle.None)
                continue;

            Rect bounds = pages[index].worldBound;

            float distance = world.y < bounds.yMin
                ? bounds.yMin - world.y
                : world.y > bounds.yMax
                    ? world.y - bounds.yMax
                    : 0f;

            if (distance < nearestDistance)
            {
                selectedPage = index;
                nearestDistance = distance;
            }
        }

        if (selectedPage < 0)
            return new Anchor(-1, Vector2.zero, Vector2.zero);

        VisualElement page = pages[selectedPage];
        Vector2 local = page.WorldToLocal(world);

        Vector2 normalized = new(
            Mathf.Clamp01(local.x / Mathf.Max(1f, page.layout.width)),
            Mathf.Clamp01(local.y / Mathf.Max(1f, page.layout.height)));

        return new Anchor(
            selectedPage,
            normalized,
            viewport.WorldToLocal(world));
    }

    public void RestoreAnchor(Anchor anchor)
    {
        if (anchor.Page < 0 || anchor.Page >= pages.Count)
            return;

        BeginPositioning(() =>
        {
            VisualElement page = pages[anchor.Page];

            Vector2 local = new(
                page.layout.width * anchor.NormalizedPoint.x,
                page.layout.height * anchor.NormalizedPoint.y);

            Vector2 world = page.LocalToWorld(local);

            Vector2 contentPoint =
                binder.DocumentScrollView.contentContainer
                    .WorldToLocal(world);

            SetScrollOffset(contentPoint - anchor.ViewportPoint);
        });
    }

    public void RequestPageJump(int index)
    {
        if (index < 0 || index >= pages.Count)
            return;

        BeginPositioning(() =>
        {
            VisualElement page = pages[index];

            Vector2 point =
                binder.DocumentScrollView.contentContainer
                    .WorldToLocal(page.worldBound.position);

            SetScrollOffset(new Vector2(
                ScrollOffset.x,
                point.y - 16f));
        });
    }

    public int DetectCurrentPage()
    {
        if (pages.Count == 0)
            return -1;

        Rect viewport =
            binder.DocumentScrollView.contentViewport.worldBound;

        float y = viewport.yMin +
                  Mathf.Min(80f, viewport.height * 0.25f);

        int lastVisible = -1;

        for (int index = 0; index < pages.Count; index++)
        {
            if (pages[index].resolvedStyle.display == DisplayStyle.None)
                continue;

            lastVisible = index;

            if (pages[index].worldBound.yMax >= y)
                return index;
        }

        return lastVisible;
    }

    public void SetScrollOffset(Vector2 offset)
    {
        ScrollView scroll = binder.DocumentScrollView;

        offset.x = Mathf.Clamp(
            offset.x,
            0f,
            Mathf.Max(0f, scroll.horizontalScroller.highValue));

        offset.y = Mathf.Clamp(
            offset.y,
            0f,
            Mathf.Max(0f, scroll.verticalScroller.highValue));

        scroll.scrollOffset = offset;
    }

    public void CancelPositioning()
    {
        positioningTask?.Pause();
        positioningTask = null;
        applyPosition = null;
        IsPositioning = false;
    }

    private void RebuildDocument(PdfContentEntry source)
    {
        IsRebuilding = true;

        try
        {
            CancelPositioning();

            binder.DocumentPages.Clear();
            binder.ThumbnailItems.Clear();
            binder.DocumentOutline.ClearSelection();

            pages.Clear();
            images.Clear();
            thumbnails.Clear();

            document = source;
            lastScale = -1f;
            lastRotation = -1;
            lastPresentationPage = -1;

            binder.DocumentScrollView.scrollOffset = Vector2.zero;

            if (document != null)
            {
                for (int index = 0;
                     index < document.Pages.Count;
                     index++)
                {
                    PdfPageContent content = document.Pages[index];

                    VisualElement page = new()
                    {
                        name = $"PdfPage-{index + 1}",
                        pickingMode = PickingMode.Ignore
                    };

                    page.AddToClassList("pdf-page");

                    Image image = builder.BuildPageImage(content);
                    page.Add(image);

                    pages.Add(page);
                    images.Add(image);
                    binder.DocumentPages.Add(page);

                    Button thumbnail = BuildThumbnail(content, index);
                    thumbnails.Add(thumbnail);
                    binder.ThumbnailItems.Add(thumbnail);
                }
            }

            binder.DocumentOutline.SetRootItems(
                builder.BuildOutline(document?.Outline));

            binder.DocumentOutline.Rebuild();
        }
        finally
        {
            IsRebuilding = false;
        }
    }

    private Button BuildThumbnail(PdfPageContent content, int index)
    {
        Button button = new()
        {
            tooltip = $"Go to page {index + 1}"
        };

        button.AddToClassList("pdf-thumbnail");

        VisualElement card = new()
        {
            pickingMode = PickingMode.Ignore
        };

        card.AddToClassList("pdf-thumbnail-card");

        Vector2 size = content.DisplaySize;

        card.style.height =
            144f * size.y / Mathf.Max(1f, size.x);

        Image image = new()
        {
            sprite = content.Thumbnail,
            scaleMode = ScaleMode.ScaleToFit,
            pickingMode = PickingMode.Ignore
        };

        image.AddToClassList("pdf-thumbnail-image");

        Label number = new((index + 1).ToString())
        {
            pickingMode = PickingMode.Ignore
        };

        number.AddToClassList("pdf-thumbnail-number");

        card.Add(image);
        card.Add(number);
        button.Add(card);

        button.clicked += () => onPageSelected?.Invoke(index);

        return button;
    }

    private float CalculateScale(
        PdfViewerEngine engine,
        bool presentation)
    {
        if (!presentation && engine.ZoomMode == PdfZoomMode.Custom)
            return engine.ZoomPercent / 100f;

        Vector2 available =
            binder.DocumentScrollView.contentViewport.contentRect.size;

        if (!PositiveFinite(available.x) ||
            !PositiveFinite(available.y))
        {
            return CurrentScale;
        }

        // Account for document padding and scrollbar changes.
        available -= new Vector2(48f, 48f);
        available.x = Mathf.Max(1f, available.x);
        available.y = Mathf.Max(1f, available.y);

        Vector2 size =
            engine.Document.Pages[engine.CurrentIndex].DisplaySize;

        if (engine.RotationDegrees == 90 ||
            engine.RotationDegrees == 270)
        {
            size = new Vector2(size.y, size.x);
        }

        float fitWidth = available.x / size.x;
        float fitPage = Mathf.Min(fitWidth, available.y / size.y);

        float scale = presentation
            ? fitPage
            : engine.ZoomMode switch
            {
                PdfZoomMode.FitWidth => fitWidth,
                PdfZoomMode.Automatic => Mathf.Min(1.25f, fitWidth),
                _ => fitPage
            };

        return Mathf.Clamp(
            scale,
            PdfViewerEngine.MinimumZoom / 100f,
            PdfViewerEngine.MaximumZoom / 100f);
    }

    private void ApplyPageLayout(
        PdfViewerEngine engine,
        bool presentation)
    {
        lastScale = CurrentScale;
        lastRotation = engine.RotationDegrees;
        lastPresentation = presentation;
        lastPresentationPage = engine.CurrentIndex;

        bool quarterTurn =
            engine.RotationDegrees == 90 ||
            engine.RotationDegrees == 270;

        float maximumWidth = 0f;

        for (int index = 0; index < pages.Count; index++)
        {
            bool visible = !presentation ||
                           index == engine.CurrentIndex;

            pages[index].style.display = visible
                ? DisplayStyle.Flex
                : DisplayStyle.None;

            Vector2 size =
                document.Pages[index].DisplaySize * CurrentScale;

            float width = quarterTurn ? size.y : size.x;
            float height = quarterTurn ? size.x : size.y;

            pages[index].style.width = width + 2f;
            pages[index].style.height = height + 2f;

            Image image = images[index];

            image.style.width = size.x;
            image.style.height = size.y;
            image.style.left = (width - size.x) * 0.5f;
            image.style.top = (height - size.y) * 0.5f;

            image.style.rotate = new Rotate(
                new Angle(
                    engine.RotationDegrees,
                    AngleUnit.Degree));

            if (visible)
                maximumWidth = Mathf.Max(maximumWidth, width + 2f);
        }

        binder.DocumentPages.style.minWidth = maximumWidth + 32f;
    }

    private void BeginPositioning(Action position)
    {
        CancelPositioning();

        if (disposed)
            return;

        applyPosition = position;
        stableFrames = 0;
        positioningAttempts = 0;

        previousContentSize = new Vector2(-1f, -1f);
        previousViewportSize = new Vector2(-1f, -1f);

        positioningTask = binder.Root.schedule
            .Execute(TryPosition)
            .Every(16);
    }

    private void TryPosition()
    {
        if (disposed || applyPosition == null)
        {
            CancelPositioning();
            return;
        }

        if (binder.Root.panel == null)
        {
            CancelPositioning();
            return;
        }

        positioningAttempts++;

        Vector2 contentSize =
            binder.DocumentScrollView.contentContainer.layout.size;

        Vector2 viewportSize =
            binder.DocumentScrollView.contentViewport.layout.size;

        bool valid =
            PositiveFinite(contentSize.x) &&
            PositiveFinite(contentSize.y) &&
            PositiveFinite(viewportSize.x) &&
            PositiveFinite(viewportSize.y);

        bool stable = valid &&
            (contentSize - previousContentSize).sqrMagnitude < 0.01f &&
            (viewportSize - previousViewportSize).sqrMagnitude < 0.01f;

        stableFrames = stable ? stableFrames + 1 : 0;

        previousContentSize = contentSize;
        previousViewportSize = viewportSize;

        if (stableFrames >= 2)
        {
            Vector2 before = ScrollOffset;

            IsPositioning = true;

            try
            {
                applyPosition();
            }
            finally
            {
                IsPositioning = false;
            }

            // Require another pass after an offset change, allowing
            // ScrollView to update its ranges.
            if ((ScrollOffset - before).sqrMagnitude < 0.01f)
            {
                CancelPositioning();
                return;
            }
        }

        if (positioningAttempts >= 24)
        {
            if (valid)
            {
                IsPositioning = true;

                try
                {
                    applyPosition();
                }
                finally
                {
                    IsPositioning = false;
                }
            }

            CancelPositioning();
        }
    }

    private bool IsEditing(VisualElement field)
    {
        VisualElement focused =
            binder.Root.panel?.focusController?.focusedElement
                as VisualElement;

        return focused == field ||
               (focused != null && field.Contains(focused));
    }

    private static bool PositiveFinite(float value)
    {
        return value > 0f &&
               !float.IsNaN(value) &&
               !float.IsInfinity(value);
    }

    private static void Show(VisualElement element, bool visible)
    {
        element.EnableInClassList("pdf-hidden", !visible);
    }

    private static void Select(VisualElement element, bool selected)
    {
        element.EnableInClassList("pdf-selected", selected);
    }

    public void Dispose()
    {
        if (disposed)
            return;

        disposed = true;
        CancelPositioning();

        binder.DocumentPages.Clear();
        binder.ThumbnailItems.Clear();

        binder.DocumentOutline.ClearSelection();
        binder.DocumentOutline.SetRootItems(
            new List<TreeViewItemData<PdfOutlineContent>>());

        binder.DocumentOutline.Rebuild();
        binder.DocumentOutline.makeItem = null;
        binder.DocumentOutline.bindItem = null;

        pages.Clear();
        images.Clear();
        thumbnails.Clear();
        document = null;
    }
}