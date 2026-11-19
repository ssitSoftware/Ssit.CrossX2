namespace Ssit.CrossX2.Framework.Games.Logic.Narration;

internal class NarrationOption
{
    public Dictionary<string, string> Text { get; }
    public NarrationEntry Entry { get; }
    public string Action { get; }
    public string SetTags { get; }

    /// <summary>
    /// Id of the dialog part to continue with once the option is taken (the <c>Goto</c> attribute) - instead of
    /// a nested <c>&lt;Text&gt;</c>. Null if not set.
    /// </summary>
    public string Goto { get; }
    
    public NarrationOption(Dictionary<string, string> text, NarrationEntry entry, string action, string setTags, string @goto)
    {
        Text = text;
        Entry = entry;
        Action = action;
        SetTags = setTags;
        Goto = @goto;
    }
}