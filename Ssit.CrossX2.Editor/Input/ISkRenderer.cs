using System;
using System.Numerics;
using SkiaSharp;

namespace Ssit.CrossX2.Editor.Input
{
    public interface ISkRenderer
    {
        event Action RedrawNeeded;
        Vector2 Size { get; }
    
        void Render(SKCanvas skCanvas, GRContext grContext, int width, int height);
        void UnloadResources();
    }

    public interface IDialog
    {
        event Action RequestClose;
    }
}