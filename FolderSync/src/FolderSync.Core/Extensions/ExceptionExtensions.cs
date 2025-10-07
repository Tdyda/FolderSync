namespace FolderSync.Core.Extensions;

public static class ExceptionExtensions 
{
        public static bool IsBenign(this Exception ex) =>
        ex is IOException
        || ex is UnauthorizedAccessException
        || ex is DirectoryNotFoundException
        || ex is FileNotFoundException
        || ex is PathTooLongException;
}