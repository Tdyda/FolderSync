namespace FolderSync.Core.Scanning;

public record RawArgs(string Source, string Replica, TimeSpan Interval, string LogPath, bool IsDebug);