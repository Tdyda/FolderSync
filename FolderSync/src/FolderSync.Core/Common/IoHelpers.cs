namespace FolderSync.Core.Common
{
    public static class IoHelpers
    {
        /// <summary>
        /// Determines whether the given exception is a benign I/O exception
        /// that can be safely ignored or logged without interrupting the process.
        /// </summary>
        public static bool IsBenign(Exception ex) =>
            ex is IOException
            || ex is UnauthorizedAccessException
            || ex is DirectoryNotFoundException
            || ex is FileNotFoundException
            || ex is PathTooLongException;
    }
}
