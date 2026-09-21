using System.Numerics;
using SkiaSharp;
using Ssit.CrossX2.Framework.Core;
using Ssit.CrossX2.Framework.IoC;

namespace Ssit.CrossX2.Framework.Graphics;

internal class RenderHost(IRenderHostParameters parameters, IRenderer renderer, IIoCContainer container): IRenderHost
{
    private Size _targetSize = Size.Zero;
    
    private IRenderTarget _renderTarget;
    private IRenderTarget _endRenderTarget;
    
    public void Begin()
    {
        if (_targetSize != renderer.TargetSize)
        {
            Resize();
        }
        
        renderer.StateManager.Reset();
        renderer.StateManager.SetRenderTarget(_renderTarget);
    }

    private void Resize()
    {
        _targetSize = renderer.TargetSize;

        var scaleWidth = _targetSize.Width / (float)parameters.DesignSize.Width;
        var scaleHeight = _targetSize.Height / (float)parameters.DesignSize.Height;
        var pixelPerfect = (parameters.Flags & RenderHostFlags.PixelPerfect) != 0;
        
        if (pixelPerfect)
        {
            scaleWidth = (int)scaleWidth;
            scaleHeight = (int)scaleHeight;
        }

        int scale = 1;
        var aspect = _targetSize.Width / (float)_targetSize.Height;
        Vector2 size = Vector2.One;

        switch (parameters.Flags & RenderHostFlags.ExactSize)
        {
            case RenderHostFlags.ExactSize:
                scale = (int)Math.Min(MathF.Ceiling(scaleWidth), MathF.Ceiling(scaleHeight));
                size = parameters.DesignSize.ToVector() * scale;
                break;
            
            case RenderHostFlags.MatchWidth:
                scale = (int)MathF.Ceiling(scaleWidth);
                size = new Vector2(parameters.DesignSize.Width * scale, parameters.DesignSize.Width * scale / aspect);
                break;
            
            case RenderHostFlags.MatchHeight:
                scale = (int)MathF.Ceiling(scaleHeight);
                size = new Vector2(parameters.DesignSize.Height * scale * aspect, parameters.DesignSize.Height * scale);
                break;
            case 0:
                scale = parameters.MinScale;
                size = parameters.DesignSize.ToVector() * scale;
                break;
        }
        
        TargetSize = new Size((int)size.X, (int)size.Y);
        Scale = scale;

        PrepareRenderTargets();
    }

    private void PrepareRenderTargets()
    {
        var useEndRenderTarget = _targetSize.Width > TargetSize.Width || _targetSize.Height > TargetSize.Height;
        if ((parameters.Flags & RenderHostFlags.PixelPerfect) != 0)
        {
            useEndRenderTarget = false;
        }

        var endRenderTargetScale = (int)MathF.Ceiling(MathF.Max(_targetSize.Width / (float)TargetSize.Width, _targetSize.Height / (float)TargetSize.Height));
        
        var endRenderTargetSize = TargetSize * endRenderTargetScale;

        if (endRenderTargetSize != _endRenderTarget?.Size || !useEndRenderTarget)
        {
            _endRenderTarget?.Dispose();
            _endRenderTarget = null;
            
            if (endRenderTargetScale > 1 && useEndRenderTarget)
            {
                _endRenderTarget = container.IoCConstruct<IRenderTarget>(new CreateRenderTargetParameters
                {
                    Size = TargetSize * endRenderTargetScale
                });
            }
        }

        if (TargetSize != _renderTarget?.Size)
        {
            _renderTarget?.Dispose();
            _renderTarget = container.IoCConstruct<IRenderTarget>(new CreateRenderTargetParameters
            {
                Size = TargetSize
            });
        }
    }

    public void End()
    {
        renderer.StateManager.Reset();
        renderer.LightingManager.EnableLighting(false);

        var sourceTexture = _renderTarget;
        if (_endRenderTarget != null)
        {
            renderer.StateManager.SetTextureFilter(TextureFilter.Point);
            renderer.StateManager.SetRenderTarget(_endRenderTarget);
            renderer.SpriteRenderer.Draw(sourceTexture, new RectangleF(0, 0, _endRenderTarget.Size.Width, _endRenderTarget.Size.Height), null, Vector2.Zero);
            sourceTexture = _endRenderTarget;
        }
        
        renderer.StateManager.SetRenderTarget(null);
        renderer.Clear(RgbaColor.Black);
        
        var scaleXy = renderer.TargetSize.ToVector() / sourceTexture.Size.ToVector();
        var scale = MathF.Min(scaleXy.X, scaleXy.Y);

        if ( (parameters.Flags & RenderHostFlags.PixelPerfect) != 0)
        {
            scale = MathF.Floor(scale);
            renderer.StateManager.SetTextureFilter(TextureFilter.Point);
        }
        else
        {
            renderer.StateManager.SetTextureFilter(TextureFilter.Linear);
        }
        
        var pos = renderer.TargetSize.ToVector() / 2f - sourceTexture.Size.ToVector() * scale / 2f;
        renderer.SpriteRenderer.Draw(sourceTexture, pos, null, Vector2.Zero, scale: scale);
    }
    
    public void Dispose()
    {
        _renderTarget?.Dispose();
        _endRenderTarget?.Dispose();

        _renderTarget = null;
        _endRenderTarget = null;
    }

    public Matrix3x2 Transform { get; }
    public Matrix3x2 TransformInv { get; }
    public Size TargetSize { get; private set; }
    public int Scale { get; private set; }
}