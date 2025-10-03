namespace FolderSync.Core.Scanning;

public sealed class DirectorySnapshot
{
    public required string RootPath { get; init; }
    public required IReadOnlyDictionary<string, FileMetadata> Files { get; init; }
    public required ISet<string> Directories { get; init; }
    public DateTime CreatedUtc { get; } = DateTime.UtcNow;
    
    public override string ToString() => $"Snapshot(Root='{RootPath}', Files={Files.Count}, Dirs={Directories.Count})";
}