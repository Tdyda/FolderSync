namespace FolderSync.Core.Scheduling;

public class CopyStats()
{
    public int FilesCopied { get; set; } = default;
    public int FilesUpdated { get; set; } = default;
    public int DirsCreated { get; set; } = default;
}