namespace FolderSync.Core.Results;

public class CopyStats
{
    public int FilesCopied { get; set; }
    public int FilesUpdated { get; set; }
    public int DirsCreated { get; set; }
}