namespace FolderSync.Core.Utilities;

public static class ExceptionExtensions
{
    public static bool IsBenign(this Exception ex)
    {
        return ex is IOException
               || ex is UnauthorizedAccessException
               || ex is DirectoryNotFoundException
               || ex is FileNotFoundException
               || ex is PathTooLongException;
    }
}