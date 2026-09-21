namespace Ssit.CrossX2.Framework.Games.Logic.Narration;

internal class NarrationEntry
{
    public Dictionary<string, string> Text { get; }
    public NarrationOption[] Options { get; }

    public NarrationEntry(Dictionary<string, string> text, NarrationOption[] options)
    {
        Text = text;
        Options = options;
    }
}