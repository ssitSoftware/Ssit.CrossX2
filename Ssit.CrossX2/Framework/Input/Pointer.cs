using System.Numerics;

namespace Ssit.CrossX2.Framework.Input;

public sealed class Pointer(int id)
{
    public int Id { get; } = id;
    
    public ButtonState State { get; private set; } = ButtonState.Empty;
    public Vector2 Position { get; private set; } = Vector2.Zero;
    public Vector2 Origin { get; private set; } = Vector2.Zero;

    private Stack<(Vector2, Vector2)> _transformHistory;
    
    public Vector2 OriginalPosition { get; private set; } = Vector2.Zero;
    
    internal void Update(ButtonState state, Vector2 position, Vector2 origin)
    {
        State = state;
        Position = position;
        Origin = origin;
        OriginalPosition = position;
        
        _transformHistory?.Clear();
    }
    
    internal void Update(ButtonState state, Vector2 position)
    {
        State = state;
        Position = position;
        OriginalPosition = position;
        
        _transformHistory?.Clear();
    }
    
    internal void Update(ButtonState state)
    {
        State = state;
        _transformHistory?.Clear();
    }

    internal void Reset()
    {
        State = ButtonState.Empty;
        Position = Vector2.Zero;
        Origin = Vector2.Zero;
        OriginalPosition = Vector2.Zero;
        
        _transformHistory?.Clear();
    }

    internal void PushTransform(Matrix3x2 matrix)
    {
        if (_transformHistory is null)
        {
            _transformHistory = new Stack<(Vector2, Vector2)>();
        }
        
        _transformHistory.Push((Position, Origin));
        Position = Vector2.Transform(Position, matrix);
        Origin = Vector2.Transform(Origin, matrix);
    }

    internal void PopTransform()
    {
        if (_transformHistory is null || _transformHistory.Count == 0)
        {
            return;
        }
        
        (Position, Origin) = _transformHistory.Pop();
    }
}