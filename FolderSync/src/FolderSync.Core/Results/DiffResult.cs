namespace FolderSync.Core.Scheduling;

public sealed class DiffResult
{
    public required ISet<string> DirsToCreate { get; init; }
    public required ISet<string> DirsToDelete { get; init; }
    public required ISet<string> FilesToCopy { get; set; }
    public required ISet<string> FilesToDelete { get; set; }
    public required ISet<string> FilesToUpdate { get; set; }
    
    public int TotalPlannedOperations() => 
        DirsToCreate.Count + DirsToDelete.Count + FilesToCopy.Count + FilesToDelete.Count + FilesToUpdate.Count;
}