namespace Ssit.CrossX2.Framework.Core;

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