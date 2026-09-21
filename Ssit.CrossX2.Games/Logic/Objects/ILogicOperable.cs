using Ssit.CrossX2.Framework.Games.Physics;

namespace Ssit.CrossX2.Framework.Games.Logic.Objects;

public interface ILogicOperable
{
    void Operate(IBodyOwner @operator);
}