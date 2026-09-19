namespace Ssit.CrossX2.Core;

public interface IAppComponent: IDisposable
{
    void Initialize();
    void SetActive(bool active);
    void Update(float dt);
    void Draw();
    void Resize();
}