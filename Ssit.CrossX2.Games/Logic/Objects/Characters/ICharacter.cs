using Ssit.CrossX2.Framework.Games.Physics;

namespace Ssit.CrossX2.Framework.Games.Logic.Objects.Characters;

public interface ICharacter
{
    bool FaceLeft { get; set; }
    IBody Body { get; }
    TParameters GetParameters<TParameters>(bool create);
}