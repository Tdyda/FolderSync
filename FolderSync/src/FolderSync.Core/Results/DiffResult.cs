namespace FolderSync.Core.Results;

public sealed class DiffResult
{
    public required ISet<string> DirsToCreate { get; init; }
    public required ISet<string> DirsToDelete { get; init; }
    public required ISet<string> FilesToCopy { get; init; }
    public required ISet<string> FilesToDelete { get; init; }
    public required ISet<string> FilesToUpdate { get; init; }

    public int TotalPlannedOperations()
    {
        return DirsToCreate.Count + DirsToDelete.Count + FilesToCopy.Count + FilesToDelete.Count + FilesToUpdate.Count;
    }
}