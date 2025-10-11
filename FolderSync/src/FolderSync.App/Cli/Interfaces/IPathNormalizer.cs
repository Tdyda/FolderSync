namespace FolderSync.App.Cli.Interfaces;

public interface IPathNormalizer
{
    string NormalizeExistingDirectory(string path, bool mustExist, string name);
    string NormalizeDirectory(string path, string name);
    string NormalizeFilePath(string path, string name);
}