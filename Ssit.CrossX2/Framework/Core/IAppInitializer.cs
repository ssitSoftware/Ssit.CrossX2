using Ssit.CrossX2.Framework.Graphics;
using Ssit.CrossX2.Framework.IoC;

namespace Ssit.CrossX2.Framework.Core;

public interface IAppInitializer
{
    void RegisterServices(IIoCContainerBuilder builder);
    IAppComponent CreateAppComponent(IIoCContainer container);
    void InitializeRenderHost(IRenderHostParameters parameters);
    bool ShouldInitializePortraitApp => false;
}