namespace FolderSync.Core.Scheduling;

public sealed class SyncSummary
{
    public int FilesCopied { get; set; }
    public int FilesUpdated { get; set; }
    public int FilesDeleted { get; set; }
    public int DirsCreated { get; set; }
    public int DirsDeleted { get; set; }
    public TimeSpan Elapsed { get; set; }
    public DateTime StartedUtc { get; set; }
    public DateTime FinishedUtc { get; set; }

    public override string ToString() =>
        $"Files copied: {FilesCopied}, files updated: {FilesUpdated}, " +
        $"files deleted: {FilesDeleted}, dirs created: {DirsCreated}, " +
        $"dirs deleted: {DirsDeleted}, elapsed: {Elapsed}";
}