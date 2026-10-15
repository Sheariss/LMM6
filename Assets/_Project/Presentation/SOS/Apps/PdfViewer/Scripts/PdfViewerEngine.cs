using System;
using System.Collections.Generic;
using Atlas.SOS.FileSystem;
using UnityEngine;

public enum PdfZoomMode
{
    Automatic,
    FitPage,
    FitWidth,
    Custom
}

public readonly struct PdfSearchMatch
{
    public int PageIndex { get; }
    public int CharacterIndex { get; }
    public int Length { get; }

    public PdfSearchMatch(
        int pageIndex,
        int characterIndex,
        int length)
    {
        PageIndex = pageIndex;
        CharacterIndex = characterIndex;
        Length = length;
    }
}

public sealed class PdfViewerEngine
{
    public const float MinimumZoom = 10f;
    public const float MaximumZoom = 500f;
    public const float ZoomStep = 10f;

    private readonly List<PdfSearchMatch> searchMatches = new();

    public PdfContentEntry Document { get; private set; }
    public string DocumentTitle { get; private set; }

    public int PageCount => Document?.Pages.Count ?? 0;
    public int CurrentIndex { get; private set; } = -1;
    public int RotationDegrees { get; private set; }

    public bool HasDocument =>
        Document != null &&
        CurrentIndex >= 0 &&
        CurrentIndex < PageCount;

    public bool CanGoPrevious =>
        HasDocument && CurrentIndex > 0;

    public bool CanGoNext =>
        HasDocument && CurrentIndex < PageCount - 1;

    public PdfZoomMode ZoomMode { get; private set; } =
        PdfZoomMode.FitPage;

    public float ZoomPercent { get; private set; } = 100f;

    public bool SidebarVisible { get; set; }
    public bool OutlineSelected { get; set; }
    public bool SearchVisible { get; set; }
    public bool MoreMenuVisible { get; set; }
    public bool HandToolSelected { get; set; }

    public string SearchQuery { get; private set; } = string.Empty;

    public IReadOnlyList<PdfSearchMatch> SearchMatches =>
        searchMatches;

    public int CurrentMatchIndex { get; private set; } = -1;

    public void LoadDocument(
        PdfContentEntry document,
        string displayName)
    {
        if (document == null)
            throw new ArgumentNullException(nameof(document));

        if (!document.TryValidate(out string error))
            throw new ArgumentException(error, nameof(document));

        Document = document;

        DocumentTitle = !string.IsNullOrWhiteSpace(displayName)
            ? displayName
            : !string.IsNullOrWhiteSpace(document.Title)
                ? document.Title
                : "PDF document";

        CurrentIndex = 0;
        RotationDegrees = 0;
        ZoomMode = PdfZoomMode.FitPage;
        ZoomPercent = 100f;

        SidebarVisible = false;
        OutlineSelected = false;
        SearchVisible = false;
        MoreMenuVisible = false;
        HandToolSelected = false;

        SearchQuery = string.Empty;
        searchMatches.Clear();
        CurrentMatchIndex = -1;
    }

    public bool SelectPage(int index)
    {
        if (index < 0 || index >= PageCount)
            return false;

        CurrentIndex = index;
        return true;
    }

    public bool MovePrevious()
    {
        return CanGoPrevious && SelectPage(CurrentIndex - 1);
    }

    public bool MoveNext()
    {
        return CanGoNext && SelectPage(CurrentIndex + 1);
    }

    public void SetZoomMode(PdfZoomMode mode)
    {
        if (!HasDocument)
            return;

        ZoomMode = mode;
    }

    public void SetZoom(float percentage)
    {
        if (!HasDocument ||
            float.IsNaN(percentage) ||
            float.IsInfinity(percentage))
        {
            return;
        }

        ZoomPercent = Mathf.Clamp(
            Mathf.Round(percentage),
            MinimumZoom,
            MaximumZoom);

        ZoomMode = PdfZoomMode.Custom;
    }

    public void ZoomBy(float delta, float displayedPercentage)
    {
        SetZoom(displayedPercentage + delta);
    }

    public void Rotate(int degrees)
    {
        if (!HasDocument)
            return;

        RotationDegrees =
            ((RotationDegrees + degrees) % 360 + 360) % 360;
    }

    public void Search(string query)
    {
        SearchQuery = query ?? string.Empty;

        searchMatches.Clear();
        CurrentMatchIndex = -1;

        if (!HasDocument ||
            string.IsNullOrWhiteSpace(SearchQuery))
        {
            return;
        }

        for (int pageIndex = 0; pageIndex < PageCount; pageIndex++)
        {
            string text = Document.Pages[pageIndex].SearchableText;
            int start = 0;

            while (start <= text.Length - SearchQuery.Length)
            {
                int found = text.IndexOf(
                    SearchQuery,
                    start,
                    StringComparison.OrdinalIgnoreCase);

                if (found < 0)
                    break;

                searchMatches.Add(new PdfSearchMatch(
                    pageIndex,
                    found,
                    SearchQuery.Length));

                start = found + SearchQuery.Length;
            }
        }

        if (searchMatches.Count > 0)
        {
            CurrentMatchIndex = 0;
            SelectPage(searchMatches[0].PageIndex);
        }
    }

    public bool MoveSearchMatch(int direction)
    {
        if (searchMatches.Count == 0)
            return false;

        CurrentMatchIndex =
            (CurrentMatchIndex + direction) % searchMatches.Count;

        if (CurrentMatchIndex < 0)
            CurrentMatchIndex += searchMatches.Count;

        return SelectPage(
            searchMatches[CurrentMatchIndex].PageIndex);
    }
}