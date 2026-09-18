using Ssit.CrossX2.Graphics;
using Ssit.CrossX2.IoC;

namespace Ssit.CrossX2.Core;

public interface IAppInitializer
{
    void RegisterServices(IIoCContainerBuilder builder);
    IAppComponent CreateAppComponent(IIoCContainer container);
    void InitializeRenderHost(IRenderHostParameters parameters);
    bool ShouldInitializePortraitApp => false;
}