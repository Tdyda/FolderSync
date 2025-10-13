namespace FolderSync.Core.Results;

public class OperationStats
{
    public int FilesCopied { get; set; }
    public int FilesUpdated { get; set; }
    public int DirsCreated { get; set; }
    public int FilesDeleted { get; set; }
    public int DirsDeleted { get; set; }
}