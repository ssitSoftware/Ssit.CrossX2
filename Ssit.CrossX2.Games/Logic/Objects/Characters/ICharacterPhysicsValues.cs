namespace Ssit.CrossX2.Framework.Games.Logic.Objects.Characters;

public interface ICharacterPhysicsValues
{
    float RunSpeed => 10f;
    float Acceleration => 44f;
    float JumpVelocity => 12f;
    float WalkSpeed => 4.4f;
    float JumpFactor => 4.0f;
    float JumpOfSpeed => 2.0f;
    float PushPullVelocity => 4.4f;
    float FrictionModifierOnLandingFactor => 0.5f;
    float JumpHoldAccelFactor => 2.0f;
    float JumpHoldAccelInc => 0.11f;
    float WallSlideSpeed => 5.5f;
    float WallSlideTimeout => 0.44f;
    float WallClimbSpeed => 8.8f;
    float AttackVelocity => 13.2f;
    float JumpAttackRaiseVelocity => JumpVelocity * 1.25f;
    float AirAttackDownVelocity => 15f;
    float ThrustDownVelocity => 18f;
    float MoveAwayDistance => 2f;
    float MoveAwaySpeed => 20f;
    float AirAcceleration => 36f;
    float AirControlSpeed => RunSpeed;
    float AirControlZeroSpeed => RunSpeed / 4;
}