using System.Numerics;
using Ssit.CrossX2.Framework.Core;
using Ssit.CrossX2.Framework.Graphics.Effects;
using Ssit.CrossX2.Framework.IoC;

namespace Ssit.CrossX2.Framework.Graphics;

internal class RenderHost : IRenderHost
{
    private Size _targetSize = Size.Zero;
    
    private IRenderTarget _renderTarget;
    private IRenderTarget _glowRenderTarget;
    private IRenderTarget _glowRenderIntermediateTarget;
    private IRenderTarget _endRenderTarget;
    
    private ICrtSimEffect _crtSimEffect;
    private IGlowEffect _glowEffect;
    
    private Size _minMaxScale = Size.Zero;
    private bool _pixelPerfect;
    private readonly IRenderHostParameters _parameters;
    private readonly IRenderer _renderer;
    private readonly IIoCContainer _container;

    public RenderHost(IRenderHostParameters parameters, IRenderer renderer, IIoCContainer container)
    {
        _parameters = parameters;
        _renderer = renderer;
        _container = container;

        _crtSimEffect = _container.IoCConstruct<ICrtSimEffect>();
    }

    public bool Begin()
    {
        var resize = Check();

        if (resize)
        {
            Resize();
        }
        
        _renderer.StateManager.Reset();
        _renderer.SetRenderTarget(_renderTarget);

        return resize;
    }

    public bool BeginGlowPass()
    {
        if (_glowRenderTarget == null)
            return false;
        
        _renderer.StateManager.Reset();
        _renderer.SetRenderTarget(_glowRenderTarget);
        _renderer.LightingManager.EnableLighting(false, false);
        _renderer.StateManager.SetBlendMode(BlendMode.AlphaBlend);
        _renderer.Clear(RgbaColor.Black);

        return true;
    }

    public bool Check()
    {
        return _targetSize != _renderer.TargetSize ||
               _minMaxScale.Width != _parameters.MinScale || _minMaxScale.Height != _parameters.MaxScale
               || _pixelPerfect != ((_parameters.Flags & RenderHostFlags.PixelPerfect) != 0);
    }

    public void Resize(Size? targetSize = null)
    {
        _targetSize = targetSize ?? _renderer.TargetSize;
        _minMaxScale = new Size(_parameters.MinScale, _parameters.MaxScale);
        _pixelPerfect = (_parameters.Flags & RenderHostFlags.PixelPerfect) != 0;
        
        var scaleWidth = _targetSize.Width / (float)_parameters.DesignSize.Width;
        var scaleHeight = _targetSize.Height / (float)_parameters.DesignSize.Height;
        var pixelPerfect = (_parameters.Flags & RenderHostFlags.PixelPerfect) != 0;
        
        if (pixelPerfect)
        {
            scaleWidth = (int)scaleWidth;
            scaleHeight = (int)scaleHeight;
        }
        
        scaleWidth = MathF.Min(MathF.Max(scaleWidth, _parameters.MinScale), _parameters.MaxScale);
        scaleHeight = MathF.Min(MathF.Max(scaleHeight, _parameters.MinScale), _parameters.MaxScale);

        if ( (_parameters.Flags & RenderHostFlags.EnableCrtSimulation) != 0)
        {
            var maxScale = Math.Min(1920 / _parameters.DesignSize.Width, 1920 / _parameters.DesignSize.Height);
            scaleWidth = MathF.Min(scaleWidth, maxScale);
            scaleHeight = MathF.Min(scaleHeight, maxScale);
        }
        
        int scale = 1;
        var aspect = _targetSize.Width / (float)_targetSize.Height;
        Vector2 size = Vector2.One;

        switch (_parameters.Flags & RenderHostFlags.ExactSize)
        {
            case RenderHostFlags.ExactSize:
                var es = Math.Min(scaleWidth, scaleHeight);
                es = pixelPerfect ? MathF.Floor(es) : MathF.Ceiling(es);
                scale = (int)es;
                size = _parameters.DesignSize.ToVector() * scale;
                break;
            
            case RenderHostFlags.MatchWidth:
                scale = pixelPerfect ? (int)MathF.Floor(scaleWidth) : (int)MathF.Ceiling(scaleWidth);
                size = new Vector2(_parameters.DesignSize.Width * scale, _parameters.DesignSize.Width * scale / aspect);
                break;
            
            case RenderHostFlags.MatchHeight:
                scale = pixelPerfect ? (int)MathF.Floor(scaleHeight) : (int)MathF.Ceiling(scaleHeight);
                size = new Vector2(_parameters.DesignSize.Height * scale * aspect, _parameters.DesignSize.Height * scale);
                break;
            case 0:
                scale = _parameters.MinScale;
                size = _parameters.DesignSize.ToVector() * scale;
                break;
        }
        
        TargetSize = new Size((int)size.X, (int)size.Y);
        Scale = scale;

        PrepareRenderTargets();
    }
    
    public void Apply() => PrepareRenderTargets();

    private void PrepareRenderTargets()
    {
        var useEndRenderTarget = _targetSize.Width > TargetSize.Width || _targetSize.Height > TargetSize.Height;
        if ((_parameters.Flags & RenderHostFlags.PixelPerfect) != 0)
        {
            useEndRenderTarget = false;
        }
        
        var endRenderTargetScale = (int)MathF.Ceiling(MathF.Max(_targetSize.Width / (float)TargetSize.Width, _targetSize.Height / (float)TargetSize.Height));
        
        if ((_parameters.Flags & RenderHostFlags.EnableCrtSimulation) != 0)
        {
            useEndRenderTarget = true;
            var scale = 2;

            while (scale < endRenderTargetScale)
            {
                scale *= 2;
            }
            endRenderTargetScale = scale;
        }
        
        var endRenderTargetSize = TargetSize * endRenderTargetScale;

        if (endRenderTargetSize != _endRenderTarget?.Size || !useEndRenderTarget)
        {
            _endRenderTarget?.Dispose();
            _endRenderTarget = null;
            
            if (endRenderTargetScale > 1 && useEndRenderTarget)
            {
                _endRenderTarget = _container.IoCConstruct<IRenderTarget>(new CreateRenderTargetParameters
                {
                    Size = TargetSize * endRenderTargetScale
                });
            }
        }

        if (TargetSize != _renderTarget?.Size)
        {
            _glowEffect?.Dispose();
            _glowEffect = _container.IoCConstruct<IGlowEffect>(TargetSize);
            
            _renderTarget?.Dispose();
            _renderTarget = _container.IoCConstruct<IRenderTarget>(new CreateRenderTargetParameters
            {
                Size = TargetSize
            });
            
            _glowRenderTarget?.Dispose();
            _glowRenderTarget = null;
            
            _glowRenderIntermediateTarget?.Dispose();
            _glowRenderIntermediateTarget = null;

            if ((_parameters.Flags & RenderHostFlags.EnableGlowPass) != 0)
            {
                _glowRenderTarget = _container.IoCConstruct<IRenderTarget>(new CreateRenderTargetParameters
                {
                    Size = TargetSize
                });
                
                _glowRenderIntermediateTarget = _container.IoCConstruct<IRenderTarget>(new CreateRenderTargetParameters
                {
                    Size = TargetSize
                });
            }
        }
    }

    public void End()
    {
        _renderer.StateManager.Reset();
        _renderer.LightingManager.EnableLighting(false, false);

        var sourceTexture = _renderTarget;
        
        if (_glowRenderTarget != null)
        {
            _renderer.StateManager.SetTextureFilter(TextureFilter.Point);
             _glowEffect?.Render(_glowRenderIntermediateTarget, _renderTarget, _glowRenderTarget, Scale);
             sourceTexture = _glowEffect != null ? _glowRenderIntermediateTarget : _renderTarget;
        }
        
        if (_endRenderTarget != null)
        {
            if (_crtSimEffect != null && (_parameters.Flags & RenderHostFlags.EnableCrtSimulation) != 0)
            {
                _crtSimEffect.Render(_endRenderTarget, sourceTexture, Scale);
            }
            else
            {
                _renderer.StateManager.SetTextureFilter(TextureFilter.Point);
                _renderer.SetRenderTarget(_endRenderTarget);
                _renderer.SpriteRenderer.Draw(sourceTexture, new RectangleF(0, 0, _endRenderTarget.Size.Width, _endRenderTarget.Size.Height), null, Vector2.Zero);
            }
            sourceTexture = _endRenderTarget;
        }
        
        _renderer.SetRenderTarget(null);
        _renderer.Clear(RgbaColor.Black);
        
        var scaleXy = _renderer.TargetSize.ToVector() / sourceTexture.Size.ToVector();
        var scale = MathF.Min(scaleXy.X, scaleXy.Y);

        if ( (_parameters.Flags & RenderHostFlags.PixelPerfect) != 0)
        {
            scale = MathF.Floor(scale);
            _renderer.StateManager.SetTextureFilter(TextureFilter.Point);
        }
        else
        {
            _renderer.StateManager.SetTextureFilter(TextureFilter.Linear);
        }
        
        var pos = _renderer.TargetSize.ToVector() / 2f - sourceTexture.Size.ToVector() * scale / 2f;
        _renderer.SpriteRenderer.Draw(sourceTexture, pos, null, Vector2.Zero, scale: scale);
        
        _renderer.StateManager.Reset();
        _parameters.PostRenderer?.Render();
    }
    
    public void Dispose()
    {
        _crtSimEffect?.Dispose();
        _crtSimEffect = null;
        
        _glowEffect?.Dispose();
        _glowEffect = null;
        
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