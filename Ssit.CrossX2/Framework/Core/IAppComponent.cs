namespace Ssit.CrossX2.Framework.Core;

/// <summary>
/// Root application component driven by the framework lifecycle.
/// </summary>
public interface IAppComponent: IDisposable
{
    void Initialize();
    void SetActive(bool active);
    void Update(float dt);
    void Draw();
    void Resize();
}
