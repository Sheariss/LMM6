namespace Atlas.Presentation.SOS.FileExplorer
{
    public enum FileExplorerItemType
    {
        File,
        Folder
    }

    public enum FileExplorerViewMode
    {
        LargeGrid,
        Grid,
        List,
        Details
    }

    public enum FileExplorerLocation
    {
        None,
        Home,
        Recent,
        Starred,
        Desktop,
        Documents,
        Pictures,
        Downloads
    }

    public enum FileExplorerSortMode
    {
        Name,
        DateModified,
        Type,
        Size
    }

    public enum FileExplorerSortDirection
    {
        Ascending,
        Descending
    }
}