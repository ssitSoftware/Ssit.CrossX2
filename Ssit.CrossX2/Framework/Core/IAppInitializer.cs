using Ssit.CrossX2.Framework.Graphics;
using Ssit.CrossX2.Framework.IoC;

namespace Ssit.CrossX2.Framework.Core;

public interface IAppInitializer
{
    string WindowTitle { get; }
    void RegisterServices(IIoCContainerBuilder builder);
    IAppComponent Initialize(IIoCContainer container, IRenderHostParameters parameters);
    bool ShouldInitializePortraitApp => false;
}