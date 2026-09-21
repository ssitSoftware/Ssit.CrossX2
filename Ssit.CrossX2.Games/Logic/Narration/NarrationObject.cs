namespace Ssit.CrossX2.Framework.Games.Logic.Narration;


internal class NarrationObject
{
    public string Name { get; }
    public IReadOnlyList<NarrationDialog> Dialogs => _dialogs;
    private readonly List<NarrationDialog> _dialogs;

    public NarrationObject(string name, IReadOnlyList<NarrationDialog> dialogs)
    {
        Name = name;
        _dialogs = new(dialogs);
    }

    public void Concat(IReadOnlyList<NarrationDialog> dialogs)
    {
        _dialogs.AddRange(dialogs);
    }
}