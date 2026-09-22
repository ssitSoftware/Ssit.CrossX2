using System.Reflection;
using Avalonia;
using Ssit.CrossX2.Framework.Games.Template;

namespace Ssit.CrossX2.Editor;

public static class EditorRunner
{
    internal static Assembly RunAssembly { get; private set; }
    
    public static void Run(string[] args, IGameTemplate gameTemplate, Assembly runAssembly = null)
    {
        RunAssembly = runAssembly;
        
        var appBuilder = BuildAvaloniaApp(gameTemplate);
        appBuilder.StartWithClassicDesktopLifetime(args);
    }

    private static AppBuilder BuildAvaloniaApp(IGameTemplate gameTemplate)
    {
        return AppBuilder.Configure(() => new App(gameTemplate))
            .UseSkia()
            .WithInterFont()
            .LogToTrace()
            .UsePlatformDetect();
    }
}