using System.Numerics;
using Ssit.CrossX2.Framework.Graphics;
using Ssit.CrossX2.Framework.Graphics.Font;
using Ssit.CrossX2.Framework.Input;
using Ssit.CrossX2.Framework.UI.Common.Pages;
using Ssit.CrossX2.Framework.UI.Services;
using Ssit.CrossX2.Framework.UI.Views;

namespace Ssit.CrossX2.Framework.UI.Handlers;

public class LabelButtonExHandler: LabelButtonHandler<LabelButtonEx>
{
    private float _waveAmplitude;
    private float _bevel;
    private float _time;
    
    public LabelButtonExHandler(CreateHandlerParameters parameters, IFontsManager fontsManager,
        IUiActionDispatcher uiActionDispatcher,
        IUiSounds uiSounds, IHapticDevice hapticDevice, PageInputContext pageInputContext) 
        : base(parameters, fontsManager, uiActionDispatcher, uiSounds, hapticDevice, pageInputContext)
    {
    }

    public override void Update(float dt)
    {
        base.Update(dt);

        _time += dt;
        _time %= 1;
        
        var amplitude = (AttachedView.FocusWaveAmplitude ?? 0).Calculate(CurrentScale, 0);
        var targetAmplitude = Focused ? amplitude : 0.0f;
        
        var bevel = (AttachedView.FocusBevel ?? 0).Calculate(CurrentScale, 0);
        var targetBevel = Focused ? bevel : 0.0f;
        
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

    protected override void OnDrawInternal(IRenderer renderer)
    {
        var globalOffset = _waveAmplitude * MathF.Sin(_time * AttachedView.FocusWaveFrequency.GetValueOrDefault() * 2 * MathF.PI);

        var offset0 = new Vector2(0.0f, -0.0f) * _bevel + globalOffset * new Vector2(1f, 0);
        var offset1 = new Vector2(1.0f, -1.0f) * _bevel + globalOffset * new Vector2(1f, 0);
        
        DrawText(renderer, RgbaColor.Transparent, TextOutlineColor(renderer) ?? RgbaColor.Transparent, offset0);

        if (Focused)
        {
            DrawText(renderer, RgbaColor.Transparent, TextOutlineColor(renderer) ?? RgbaColor.Transparent, offset1);
        }
        
        if (!Focused || Enabled)
        {
            var lowColor = Focused ? AttachedView?.FocusedLowColor.GetColor(renderer) : null;
            DrawText(renderer, lowColor ?? TextColor(renderer, false) ?? RgbaColor.Transparent, RgbaColor.Transparent, offset0);
        }

        if ( MathF.Abs(globalOffset) > 0 || Focused)
        {
            var color = AttachedView.TextColors?.GetColor(renderer, false, true, IsPushed, Enabled, IsChecked);
            DrawText(renderer, color ?? RgbaColor.Transparent, RgbaColor.Transparent, offset1);
        }
    }
}