using Ssit.CrossX2.Framework.Core;
using Ssit.CrossX2.Framework.Graphics;
using Ssit.CrossX2.Framework.IoC;

namespace Ssit.CrossX2.Framework.Games;

public interface IGameInstance: IDisposable
{
    IMessenger Messenger { get; }
    event Action<float> FixedUpdate;
    int RenderPasses { get; }
    void Render(IRenderer renderer, RectangleF target, int renderPass, float scale);
    void RenderDebug(IRenderer renderer, RectangleF target, float scale);
    void Update(float deltaTime);
    TService GetComponent<TService>() where TService : class;
    void Activate(bool active);
    IIoCContainer Services { get; }
}