using System.Numerics;
using Avalonia.Input;

namespace Ssit.CrossX2.Editor.Input
{
    public readonly struct MouseInputInfo
    {
        public Vector2 Position { get; }
        public MouseButton ActionButton { get; }
        public MouseButton MouseButtons { get; }
        public KeyModifiers Modifiers { get; }
        public Vector2 Delta { get; }

        public MouseInputInfo(Vector2 position, MouseButton actionButton, MouseButton mouseButtons, KeyModifiers modifiers, Vector2 delta)
        {
            Position = position;
            ActionButton = actionButton;
            MouseButtons = mouseButtons;
            Modifiers = modifiers;
            Delta = delta;
        }
    }
}