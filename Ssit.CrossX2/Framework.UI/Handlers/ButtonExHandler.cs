using System.Numerics;
using Ssit.CrossX2.Framework.Graphics;
using Ssit.CrossX2.Framework.Input;
using Ssit.CrossX2.Framework.UI.Common.Pages;
using Ssit.CrossX2.Framework.UI.Services;
using Ssit.CrossX2.Framework.UI.Views;

namespace Ssit.CrossX2.Framework.UI.Handlers;

public class ButtonExHandler: ButtonHandler<ButtonEx>
{
    private float _waveAmplitude;
    private float _bevel;
    private float _time;
    private bool _lowColorPass;

    public ButtonExHandler(CreateHandlerParameters parameters, IHandlerMapper handlerMapper,
        IUiSounds uiSounds, IHapticDevice hapticDevice,
        IRenderer renderer,
        IPointingDevices pointingDevices,
        PageInputContext pageInputContext)
        : base(parameters, handlerMapper, uiSounds, hapticDevice, renderer, pointingDevices, pageInputContext)
    {
    }

    public override void Update(float dt)
    {
        base.Update(dt);

        _time += dt;
        _time %= 1;

        var amplitude = (AttachedView.FocusWaveAmplitude ?? 0).Calculate(CurrentScale, Bounds.Width);
        var targetAmplitude = Focused ? amplitude : 0.0f;

        var bevel = (AttachedView.FocusBevel ?? 0).Calculate(CurrentScale, Bounds.Height);
        var targetBevel = Focused ? bevel : 0.0f;

        if (IsPushed) targetBevel = bevel / 2;

        if (_waveAmplitude < targetAmplitude)
        {
            _waveAmplitude += dt * amplitude * 8;
            _waveAmplitude = MathF.Min(_waveAmplitude, targetAmplitude);
        }
        else if (_waveAmplitude > targetAmplitude)
        {
            _waveAmplitude -= dt * amplitude * 16;
            _waveAmplitude = MathF.Max(_waveAmplitude, targetAmplitude);
        }

        if (AttachedView.AnimateBevel.GetValueOrDefault())
        {
            if (_bevel < targetBevel)
            {
                _bevel += dt * bevel * 8;
                _bevel = MathF.Min(_bevel, targetBevel);
            }
            else if (_bevel > targetBevel)
            {
                _bevel -= dt * bevel * 16;
                _bevel = MathF.Max(_bevel, targetBevel);
            }
        }
        else
        {
            _bevel = targetBevel;
        }
    }

    public override RgbaColor? GetColor(string id)
    {
        if (_lowColorPass && id is nameof(ButtonEx.ForegroundColors) or nameof(ButtonEx.OutlineColors))
        {
            var lowColor = AttachedView?.FocusedLowColor.GetColor(Renderer);
            if (lowColor.HasValue)
            {
                return lowColor;
            }
        }
        
        if (_lowColorPass && id is nameof(ButtonEx.BackgroundColors))
        {
            return RgbaColor.Transparent;
        }

        return base.GetColor(id);
    }

    protected override void DrawChildren(IRenderer renderer)
    {
        if (_bevel == 0 && _waveAmplitude == 0)
        {
            base.DrawChildren(renderer);
            return;
        }

        renderer.StateManager.SaveState();
        var globalOffset = _waveAmplitude * MathF.Sin(_time * AttachedView.FocusWaveFrequency.GetValueOrDefault() * 2 * MathF.PI);
        
        renderer.StateManager.Translate(new Vector2(globalOffset, 0));
        
        _lowColorPass = true;
        base.DrawChildren(renderer);
        _lowColorPass = false;
        
        var offset = new Vector2(1.0f, -1.0f) * _bevel;
        
        renderer.StateManager.Translate(offset);
        base.DrawChildren(renderer);
        renderer.StateManager.RestoreState();
    }
}
