namespace Ssit.CrossX2.Framework.Core;

public interface IAppComponent: IDisposable
{
    void Initialize();
    void SetActive(bool active);
    void Update(float dt);
    void Draw();
    void Resize();
}