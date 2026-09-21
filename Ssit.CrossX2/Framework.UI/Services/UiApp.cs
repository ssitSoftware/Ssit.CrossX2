using Ssit.CrossX2.Framework.Graphics;
using Ssit.CrossX2.Framework.IoC;
using Ssit.CrossX2.Framework.UI.Exceptions;

namespace Ssit.CrossX2.Framework.UI.Services;

internal class UiApp(IIoCContainer services, IUiActionDispatcher iUiActionDispatcher) : IUiAppInternal
{
    INavigation IUiApp.Navigation => Navigation;

    public void LoadStyles(params Type[] types)
    {
        foreach (var type in types)
        {
            StylesContainer.ParseStyles(type);
        }
    }

    public Navigation Navigation { get; private set; }
    public RectangleF Bounds { get; private set; }

    public float Scale { get; private set; }

    internal readonly StylesContainer StylesContainer = new();

    public IIoCContainer Services { get; private set; } = services;
    public InputProcessor InputProcessor { get; private set; }
    
    private readonly UiActionDispatcher _uiActionDispatcher = (UiActionDispatcher)iUiActionDispatcher;

    public void Initialize(Navigation navigation)
    {
        InputProcessor = Services.IoCConstruct<InputProcessor>(navigation);
        Navigation = navigation;
    }
    
    public void Update(float dt)
    {
        _uiActionDispatcher.Dispatch();

        InputProcessor.Process();
        Navigation.Update(dt);
    }

    public void Draw(IRenderer renderer,RgbaColor? clearColor = null)
    {
        for (var idx = 0; idx < 8; ++idx)
        {
            try
            {
                InternalDraw(renderer, clearColor);
                break;
            }
            catch (InvalidRenderingException)
            {
                Navigation.CurrentPage?.RecalculateLayout();
                Navigation.CurrentPage?.Update(0);
                
                _uiActionDispatcher.Dispatch();
                Navigation.Update(0);
            }
        }
    }

    private void InternalDraw(IRenderer renderer, RgbaColor? clearColor = null)
    {
        if (renderer.CurrentPass == RenderPass.Glow)
        {
            renderer.Clear(RgbaColor.Black);
        }
        else if(clearColor?.A > 0)
        {
            renderer.Clear(clearColor.Value);
        }
        
        if (!Navigation.PreviousPageOnTop && Navigation.PreviousPage is not null)
        {
            Navigation.PreviousPage.Draw(renderer);
        }

        if (Navigation.CurrentPage is not null)
        {
            if (Navigation.ParallelTransitions || Navigation.PreviousPage is null)
            {
                Navigation.CurrentPage.Draw(renderer);
            }
        }

        if (Navigation.PreviousPageOnTop && Navigation.PreviousPage is not null)
        {
            Navigation.PreviousPage.Draw(renderer);
        }
    }

    public void SetBounds(RectangleF bounds, float scale)
    {
        Bounds = bounds;
        Scale = scale;
        
        Navigation.PreviousPage?.SetBounds(bounds, scale);
        Navigation.CurrentPage?.SetBounds(bounds, scale);
    }

    public void Dispose()
    {
        var services = Services;
        Services = null;
        services?.Dispose();
    }
}