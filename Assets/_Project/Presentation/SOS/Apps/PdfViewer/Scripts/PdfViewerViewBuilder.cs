using System;
using System.Collections.Generic;
using System.Globalization;
using Atlas.SOS.FileSystem;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class PdfViewerViewState
{
    public PdfContentEntry Document { get; internal set; }
    public string Title { get; internal set; }

    public int PageIndex { get; internal set; }
    public int RotationDegrees { get; internal set; }

    public float Scale { get; internal set; }
    public string ZoomText { get; internal set; }

    public bool SidebarVisible { get; internal set; }
    public bool OutlineSelected { get; internal set; }
    public bool SearchVisible { get; internal set; }
    public bool MoreMenuVisible { get; internal set; }
    public bool HandToolSelected { get; internal set; }

    public string SearchQuery { get; internal set; }
    public string SearchResultText { get; internal set; }
    public int SearchMatchCount { get; internal set; }

    public bool HasDocument =>
        Document != null &&
        PageIndex >= 0 &&
        PageIndex < Document.Pages.Count;
}

public sealed class PdfViewerViewBuilder
{
    public const string AutomaticChoice = "Automatic zoom";
    public const string FitPageChoice = "Fit page";
    public const string FitWidthChoice = "Fit width";

    private static readonly int[] ZoomPresets =
    {
        10, 25, 50, 75, 100, 125, 150,
        200, 250, 300, 400, 500
    };

    public PdfViewerViewState Build(
        PdfViewerEngine engine,
        Vector2 availablePageArea)
    {
        if (engine == null)
            throw new ArgumentNullException(nameof(engine));

        float scale = CalculateScale(engine, availablePageArea);

        string zoomText = engine.ZoomMode switch
        {
            PdfZoomMode.Automatic => AutomaticChoice,
            PdfZoomMode.FitPage => FitPageChoice,
            PdfZoomMode.FitWidth => FitWidthChoice,
            _ => FormatZoom(engine.ZoomPercent)
        };

        string results = engine.SearchMatches.Count > 0
            ? $"{engine.CurrentMatchIndex + 1} of " +
              $"{engine.SearchMatches.Count} matches"
            : string.IsNullOrWhiteSpace(engine.SearchQuery)
                ? "0 matches"
                : "No matches";

        return new PdfViewerViewState
        {
            Document = engine.Document,
            Title = engine.HasDocument
                ? engine.DocumentTitle
                : "PDF Viewer",

            PageIndex = engine.CurrentIndex,
            RotationDegrees = engine.RotationDegrees,
            Scale = scale,
            ZoomText = zoomText,

            SidebarVisible = engine.SidebarVisible,
            OutlineSelected = engine.OutlineSelected,
            SearchVisible = engine.SearchVisible,
            MoreMenuVisible = engine.MoreMenuVisible,
            HandToolSelected = engine.HandToolSelected,

            SearchQuery = engine.SearchQuery,
            SearchResultText = results,
            SearchMatchCount = engine.SearchMatches.Count
        };
    }

    public List<string> BuildZoomChoices(string currentChoice)
    {
        List<string> choices = new()
        {
            AutomaticChoice,
            FitPageChoice,
            FitWidthChoice
        };

        foreach (int value in ZoomPresets)
            choices.Add(FormatZoom(value));

        if (!string.IsNullOrEmpty(currentChoice) &&
            !choices.Contains(currentChoice))
        {
            choices.Add(currentChoice);
        }

        return choices;
    }

    public static bool TryParseZoom(
        string choice,
        out float percentage)
    {
        percentage = 0f;

        if (string.IsNullOrWhiteSpace(choice))
            return false;

        return float.TryParse(
            choice.Trim().TrimEnd('%'),
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out percentage);
    }

    public Image BuildPageImage(PdfPageContent page)
    {
        Image image = new()
        {
            sprite = page.Image,
            scaleMode = ScaleMode.ScaleToFit,
            pickingMode = PickingMode.Ignore
        };

        image.AddToClassList("pdf-page-image");

        image.style.position = Position.Absolute;
        image.style.flexGrow = 0;
        image.style.transformOrigin = new TransformOrigin(
            Length.Percent(50f),
            Length.Percent(50f),
            0f);

        return image;
    }

    public Button BuildThumbnail(
        PdfPageContent page,
        int index,
        Action<int> onSelected)
    {
        Button button = new()
        {
            text = string.Empty,
            tooltip = $"Page {index + 1}",
            userData = index
        };

        button.AddToClassList("pdf-button");
        button.AddToClassList("pdf-thumbnail");

        Image image = new()
        {
            sprite = page.Thumbnail,
            scaleMode = ScaleMode.ScaleToFit,
            pickingMode = PickingMode.Ignore
        };

        image.AddToClassList("pdf-thumbnail-image");
        button.Add(image);

        Label number = new((index + 1).ToString())
        {
            pickingMode = PickingMode.Ignore
        };

        number.AddToClassList("pdf-thumbnail-number");
        button.Add(number);

        button.clicked += () => onSelected?.Invoke(index);

        return button;
    }

    public List<TreeViewItemData<PdfOutlineContent>> BuildOutline(
        IReadOnlyList<PdfOutlineContent> entries)
    {
        if (entries == null || entries.Count == 0)
            return new List<TreeViewItemData<PdfOutlineContent>>();

        int index = 0;
        return BuildOutlineLevel(entries, ref index, 0);
    }

    private static List<TreeViewItemData<PdfOutlineContent>>
        BuildOutlineLevel(
            IReadOnlyList<PdfOutlineContent> entries,
            ref int index,
            int level)
    {
        List<TreeViewItemData<PdfOutlineContent>> result = new();

        while (index < entries.Count && entries[index].Level == level)
        {
            int id = index + 1;
            PdfOutlineContent entry = entries[index++];

            List<TreeViewItemData<PdfOutlineContent>> children = null;

            if (index < entries.Count &&
                entries[index].Level > level)
            {
                children = BuildOutlineLevel(
                    entries,
                    ref index,
                    level + 1);
            }

            result.Add(new TreeViewItemData<PdfOutlineContent>(
                id,
                entry,
                children));
        }

        return result;
    }

    private static float CalculateScale(
        PdfViewerEngine engine,
        Vector2 available)
    {
        if (!engine.HasDocument)
            return 1f;

        if (engine.ZoomMode == PdfZoomMode.Custom)
            return engine.ZoomPercent / 100f;

        if (!IsPositiveFinite(available.x) ||
            !IsPositiveFinite(available.y))
        {
            return 1f;
        }

        Vector2 size =
            engine.Document.Pages[engine.CurrentIndex].DisplaySize;

        if (engine.RotationDegrees == 90 ||
            engine.RotationDegrees == 270)
        {
            size = new Vector2(size.y, size.x);
        }

        float fitWidth = available.x / size.x;
        float fitPage = Mathf.Min(fitWidth, available.y / size.y);

        float scale = engine.ZoomMode switch
        {
            PdfZoomMode.FitWidth => fitWidth,
            PdfZoomMode.Automatic => Mathf.Min(1f, fitWidth),
            _ => fitPage
        };

        return Mathf.Clamp(
            scale,
            PdfViewerEngine.MinimumZoom / 100f,
            PdfViewerEngine.MaximumZoom / 100f);
    }

    private static bool IsPositiveFinite(float value)
    {
        return value > 0f &&
               !float.IsNaN(value) &&
               !float.IsInfinity(value);
    }

    private static string FormatZoom(float percentage)
    {
        return percentage.ToString(
            "0",
            CultureInfo.InvariantCulture) + "%";
    }
}