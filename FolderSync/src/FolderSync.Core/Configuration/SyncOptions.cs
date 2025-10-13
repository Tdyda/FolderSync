namespace FolderSync.Core.Configuration;

public sealed class SyncOptions
{
    public required string SourcePath { get; init; }
    public required string ReplicaPath { get; init; }
    public required TimeSpan Interval { get; init; }
    public required string LogFilePath { get; init; }
    public bool IsDebug { get; init; }

    public override string ToString()
    {
        return $"Source={SourcePath}, Replica={ReplicaPath}, Interval={Interval}, Log={LogFilePath}";
    }
}