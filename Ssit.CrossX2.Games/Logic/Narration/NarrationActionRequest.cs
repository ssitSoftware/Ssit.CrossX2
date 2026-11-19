namespace Ssit.CrossX2.Framework.Games.Logic.Narration;

/// <summary>
/// Action of a chosen dialog option (the <c>Action</c> attribute of <c>&lt;Option&gt;</c>), raised by
/// <see cref="INarrationSystem.NarrationAction"/> on the game thread.
/// A handler sets <see cref="Result"/> to false to refuse the action (e.g. not enough silver) - the dialog doesn't
/// progress then: the option's flags (<c>Set</c>) aren't set and the same text is shown again with its options.
/// A handler sets <see cref="Goto"/> to continue with another dialog part - for a refused action too (e.g. "your sword
/// is already sharp").
/// </summary>
public class NarrationActionRequest
{
    /// <summary>Name of the action - the value of the option's <c>Action</c> attribute.</summary>
    public string Action { get; }

    /// <summary>
    /// True (default) - the action was done, the dialog continues with the option. Set to false by the handler to
    /// refuse it.
    /// </summary>
    public bool Result { get; set; } = true;

    /// <summary>
    /// Id of the dialog part (<c>&lt;Text Id&gt;</c> or <c>&lt;Dialog Id&gt;</c>) to continue with, set by the handler.
    /// Wins over the option's own <c>Goto</c> and nested text. Null (default) - the dialog continues as written, or
    /// stays at the same text when the action was refused.
    /// </summary>
    public string Goto { get; set; }

    public NarrationActionRequest(string action)
    {
        Action = action;
    }
}
