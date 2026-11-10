using Ssit.CrossX2.Framework.Games.Logic.Objects;
using Ssit.CrossX2.Framework.Games.Logic.Objects.Characters;

namespace Ssit.CrossX2.Framework.Games.Platformer.Behaviors.BotBrains;

/// <summary>
/// Enemy that belongs to a horde: when one member spots a target, nearby members are alerted and join the chase.
/// </summary>
public interface IHordeMember : IBrainCharacter
{
    /// <summary>
    /// Name of the horde this member belongs to (e.g. "Wolves"). Members of the same horde alert each other.
    /// </summary>
    string Horde { get; }

    /// <summary>
    /// Called by another horde member that acquired a target. <paramref name="horde"/> is the horde being alerted.
    /// Implementations should ignore the call when it's not their horde, or when they are already
    /// fighting (or dead), so the alert spreads through the horde without looping.
    /// </summary>
    void Alert(IHittable target, string horde);
}
