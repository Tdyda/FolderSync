namespace FolderSync.Core.Configuration;

public sealed record ParsedArgs(IReadOnlyDictionary<string, string> Tokens, IReadOnlySet<string> Switches);