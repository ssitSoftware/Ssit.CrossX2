using Ssit.CrossX2.Framework.Games.Physics;

namespace Ssit.CrossX2.Framework.Games.Logic.Objects.Characters;

public interface IBrainCharacter
{
    bool FaceLeft { get; set; }
    IBody Body { get; }
    TParameters GetParameters<TParameters>();

    ISteeringInputController InputController { get; }
    void SetBrainState(string state);
}