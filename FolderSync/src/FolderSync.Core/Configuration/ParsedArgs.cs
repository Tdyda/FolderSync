namespace FolderSync.Core.Configuration;

public class ParsedArgs
{
    public List<string> Switches { get; set; } = new();
    public Dictionary<string, string> Flags { get; set; } = new();
}