using System;
using System.ComponentModel;
using System.Numerics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using Avalonia.Threading;
using SkiaSharp;
using Ssit.CrossX2.Editor.Input;
using MouseButton = Avalonia.Input.MouseButton;
// ReSharper disable BitwiseOperatorOnEnumWithoutFlags

namespace Ssit.CrossX2.Editor.Controls
{
    public class SkPanel : UserControl
    {
        public class CustomDraw : ICustomDrawOperation
        {
            private readonly Control _control;

            public ISkRenderer Renderer
            {
                get;
                set
                {
                    if (field != value)
                    {
                        field?.UnloadResources();
                        field = value;
                    }
                }
            }

            public CustomDraw(Control control)
            {
                _control = control;
            }
        
            public void Dispose()
            {
                Renderer.UnloadResources();
            }

            public bool HitTest(Point p)
            {
                return false;
            }

            public void Render(ImmediateDrawingContext context)
            {
                if (!context.TryGetFeature<ISkiaSharpApiLeaseFeature>(out var feature)) return;

                using var lease = feature.Lease();
            
                var skCanvas = lease.SkCanvas;
                var grContext = lease.GrContext;

                try
                {
                    skCanvas.Clear(new SKColor(Background.R, Background.G, Background.B));
                    Renderer?.Render(skCanvas, grContext, (int) _control.Bounds.Width, (int) _control.Bounds.Height);
                }
                catch (Exception)
                {
                    // IGNORE
                }
            }
        

            public Rect Bounds => new Rect(0, 0, _control.Bounds.Width, _control.Bounds.Height);
            public Color Background { get; set; }
        
            public bool Equals(ICustomDrawOperation other)
            {
                return ReferenceEquals(this, other);
            }
        }
    
        public static readonly StyledProperty<bool> ContinuousRedrawProperty =
            AvaloniaProperty.Register<SkPanel, bool>(nameof(ContinuousRedraw));
    
        public new static readonly StyledProperty<Color> BackgroundProperty =
            AvaloniaProperty.Register<SkPanel, Color>(nameof(Background));
    
    
        public static readonly StyledProperty<ISkRenderer> RendererProperty =
            AvaloniaProperty.Register<SkPanel, ISkRenderer>(nameof(Renderer));
    
        public static readonly StyledProperty<IPointerHandler> MouseHandlerProperty =
            AvaloniaProperty.Register<SkPanel, IPointerHandler>(nameof(MouseHandler));

        public new Color Background
        {
            get => GetValue(BackgroundProperty);
            set => SetValue(BackgroundProperty, value);
        }
        public bool ContinuousRedraw
        {
            get => GetValue(ContinuousRedrawProperty);
            set => SetValue(ContinuousRedrawProperty, value);
        }
    
        public ISkRenderer Renderer
        {
            get => GetValue(RendererProperty);
            set => SetValue(RendererProperty, value);
        }
    
        public IPointerHandler MouseHandler
        {
            get => GetValue(MouseHandlerProperty);
            set => SetValue(MouseHandlerProperty, value);
        }
    
        private readonly CustomDraw _customDraw;
    
        public SkPanel()
        {
            _customDraw = new CustomDraw(this);
            base.Background = new SolidColorBrush(Colors.Transparent);
            ClipToBounds = true;
            Focusable = true;
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);
        
            if (change.Property == ContinuousRedrawProperty)
            {
                Dispatcher.UIThread.InvokeAsync(InvalidateVisual, DispatcherPriority.Render);
            }
        
            if (change.Property == RendererProperty)
            {
                if ( change.OldValue is ISkRenderer oldRenderer)
                {
                    oldRenderer.RedrawNeeded -= RendererOnRedrawNeeded;
                    if (oldRenderer is INotifyPropertyChanged npc)
                    {
                        npc.PropertyChanged -= RendererOnPropertyChanged;
                    }
                }
            
                _customDraw.Renderer = Renderer;
                Renderer.RedrawNeeded += RendererOnRedrawNeeded;
                if (Renderer is INotifyPropertyChanged npc2)
                {
                    npc2.PropertyChanged += RendererOnPropertyChanged;
                }

                RendererOnResize(Renderer.Size);
            }

            if (change.Property == BackgroundProperty)
            {
                RendererOnRedrawNeeded();
            }
        }

        private void RendererOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ISkRenderer.Size))
            {
                RendererOnResize(Renderer.Size);
            }
        }

        private void RendererOnResize(Vector2 size)
        {
            if (size.X < 1 || size.Y < 1) return;
        
            Width = size.X;
            Height = size.Y;
        }

        private void RendererOnRedrawNeeded()
        {
            Dispatcher.UIThread.InvokeAsync(InvalidateVisual, DispatcherPriority.Render);
        }

        public override void Render(DrawingContext context)
        {
            _customDraw.Background = Background;
            context.Custom(_customDraw);

            if (ContinuousRedraw)
            {
                Dispatcher.UIThread.InvokeAsync(InvalidateVisual, DispatcherPriority.Render);
            }
        }

        protected override void OnPointerMoved(PointerEventArgs e)
        {
            var primary = e.Pointer.IsPrimary;
            if (!primary)
            {
                base.OnPointerMoved(e);
                return;
            }
        
            MouseHandler?.OnMouseMove(GetMouseInputInfo(e, Vector2.Zero));
        }

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            var primary = e.Pointer.IsPrimary;
            if (!primary)
            {
                base.OnPointerPressed(e);
                return;
            }
        
            MouseHandler?.OnButtonDown(GetMouseInputInfo(e, Vector2.Zero));
            e.Pointer.Capture(this);
        }

        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            var primary = e.Pointer.IsPrimary;
            if (!primary)
            {
                base.OnPointerReleased(e);
                return;
            }
        
            MouseHandler?.OnButtonUp(GetMouseInputInfo(e, Vector2.Zero));
        }

        protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
        {
            var primary = e.Pointer.IsPrimary;
        
            if (!primary)
            {
                base.OnPointerWheelChanged(e);
                return;
            }

            e.Handled =
                MouseHandler?.OnMouseWheel(GetMouseInputInfo(e, new Vector2((float) e.Delta.X, (float) e.Delta.Y))) ??
                false;
        }

        protected override void OnPointerExited(PointerEventArgs e)
        {
            base.OnPointerExited(e);
        
            var primary = e.Pointer.IsPrimary;
            if (!primary)
            {
                base.OnPointerExited(e);
                return;
            }

            MouseHandler?.OnMouseLeave(GetMouseInputInfo(e, Vector2.Zero));
        }

        private MouseInputInfo GetMouseInputInfo(PointerEventArgs e, Vector2 delta)
        {
            var btn = GetButton(e);
            var btns = GetButtons(e);
            var pos = GetPosition(e);
            var modifiers = e.KeyModifiers;

            return new MouseInputInfo(pos, btn, btns, modifiers, delta);
        }

        private MouseButton GetButtons(PointerEventArgs e)
        {
            var properties = e.GetCurrentPoint(this).Properties;
        
            var btn = MouseButton.None;

            if (properties.IsMiddleButtonPressed)
            {
                btn |= MouseButton.Middle;
            }
        
            if (properties.IsRightButtonPressed)
            {
                btn |= MouseButton.Right;
            }
        
            if (properties.IsLeftButtonPressed)
            {
                btn |= MouseButton.Left;
            }

            return btn;
        }

        private Vector2 GetPosition(PointerEventArgs e)
        {
            var pos = e.GetPosition(this);
            return new Vector2((float) pos.X, (float) pos.Y);
        }

        private MouseButton GetButton(PointerEventArgs e)
        {
            var btn = e.GetCurrentPoint(this).Properties.PointerUpdateKind.GetMouseButton();
            return btn;
        }
    }
}