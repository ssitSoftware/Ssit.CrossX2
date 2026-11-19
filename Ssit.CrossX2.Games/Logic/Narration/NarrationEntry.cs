namespace Ssit.CrossX2.Framework.Games.Logic.Narration;

internal class NarrationEntry
{
    /// <summary>
    /// Id of the dialog part (the <c>Id</c> attribute of <c>&lt;Text&gt;</c>) - a jump target for
    /// <see cref="NarrationOption.Goto"/> and <see cref="NarrationActionRequest.Goto"/>. Null if not set.
    /// </summary>
    public string Id { get; }
    public Dictionary<string, string> Text { get; }
    public NarrationOption[] Options { get; }

    public NarrationEntry(string id, Dictionary<string, string> text, NarrationOption[] options)
    {
        Id = id;
        Text = text;
        Options = options;
    }
}