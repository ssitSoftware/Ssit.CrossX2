namespace Ssit.CrossX2.Framework.Core;

/// <summary>
/// Receives per-frame and fixed-step update callbacks.
/// </summary>
public interface IUpdatable
{
    void Update(float dt)
    {
    }

    void PostUpdate()
    {
    }

    void FixedUpdate(float dt)
    {
    }

    void PostFixedUpdate()
    {
    }
}
