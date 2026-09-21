namespace Ssit.CrossX2.Framework.Games.Logic.Narration;

internal class NarrationDialog
{
    public string Id { get; }
    public IReadOnlySet<string> On { get; }
    public IReadOnlySet<string> Off { get; }
    public bool Highlight { get; }
    public NarrationEntry Entry { get; }
    public string DefaultLanguage { get; }

    public NarrationDialog(string id, IReadOnlySet<string> on, IReadOnlySet<string> off, bool highlight, string defaultLanguage, NarrationEntry entry)
    {
        Id = id;
        On = on;
        Off = off;
        Highlight = highlight;
        DefaultLanguage = defaultLanguage;
        Entry = entry;
    }
}