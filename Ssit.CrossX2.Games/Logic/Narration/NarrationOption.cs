namespace Ssit.CrossX2.Framework.Games.Logic.Narration;

internal class NarrationOption
{
    public Dictionary<string, string> Text { get; }
    public NarrationEntry Entry { get; }
    public string Action { get; }
    public string SetTags { get; }
    
    public NarrationOption(Dictionary<string, string> text, NarrationEntry entry, string action, string setTags)
    {
        Text = text;
        Entry = entry;
        Action = action;
        SetTags = setTags;
    }
}