using Ssit.CrossX2.Framework.Games.Physics;

namespace Ssit.CrossX2.Framework.Games.Logic.Objects;

public interface IPushable
{
    IBody Body { get; }
    bool CanPull => false;
}