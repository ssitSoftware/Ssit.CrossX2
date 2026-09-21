namespace Ssit.CrossX2.Framework.Games.Rendering;

public interface IShooterRenderer : IGameObjectRenderer
{
    bool GunBehind { get; set; }
    void UpdateAimingAngle(float angle, bool aiming, bool reloading, float recoil);
}