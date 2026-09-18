using System.Collections.Concurrent;
using System.Numerics;

namespace Ssit.CrossX2.Input.Internal;

public class PointingDevicesBase: IPointingDevices, IInputHandler
{
    public virtual PointingDevicesMode Mode { get; set; }

    private readonly List<Pointer> _pointers = new();
    public IReadOnlyList<Pointer> Pointers => _pointers;
    public Pointer GetPointer(int id) => _pointers.Find(o => o.Id == id);
    public Vector2? HoverPosition { get; private set; }
    
    public bool LockMouseInWindow { get; set; }

    public bool ShowHoverPointer { get; set; } = true;

    private readonly Stack<Vector2?> _hoverPositions = new();

    private readonly Dictionary<ulong, int> _touchIds = new();
    private int _nextTouchId = MouseButtons.Middle + 1;

    private readonly List<TouchEvent> _aggregatedEvents = new();
    
    protected PointingDevicesBase()
    {
    }
    
    protected void SetPointer(int id, ButtonState state, Vector2? position)
    {
        if (Mode == PointingDevicesMode.Disabled)
            return;

        if ((Mode & PointingDevicesMode.Touch) == 0)
        {
            if (id is < MouseButtons.Left or > MouseButtons.Middle)
            {
                return;
            }
        }

        if ((Mode & PointingDevicesMode.Mouse) == 0 && id is >= MouseButtons.Left and <= MouseButtons.Middle)
        {
            return;
        }
        
        var pointer = GetPointer(id);
        if (pointer == null)
        {
            pointer = new Pointer(id);
            pointer.Update(ButtonState.JustPressed, position.GetValueOrDefault(), position.GetValueOrDefault());
            _pointers.Add(pointer);
        }
        else
        {
            if (position.HasValue)
            {
                pointer.Update(state, position.Value);
            }
            else
            {
                pointer.Update(state);
            }
        }
    }

    public void OnPreUpdate()
    {
        for (var idx = 0; idx < _pointers.Count;)
        {
            if (_pointers[idx].State == ButtonState.Empty)
            {
                _pointers.RemoveAt(idx);
            }
            ++idx;
        }
        
        for (var idx = 0; idx < _pointers.Count; ++idx)
        {
            if (_pointers[idx].State == ButtonState.JustReleased)
            {
                _pointers[idx].Update(ButtonState.Empty);
            }
        }
        
        for (var idx = 0; idx < _pointers.Count; ++idx)
        {
            if (_pointers[idx].State == ButtonState.JustPressed)
            {
                _pointers[idx].Update(ButtonState.Down);
            }
        }

        _aggregatedEvents.Clear();
        
        while (_touchEvents.TryDequeue(out var te))
        {
            var index = _aggregatedEvents.FindIndex(e => e.Id == te.Id && e.State.IsDown == te.State.IsDown);
            if (index >= 0)
            {
                var evnt = _aggregatedEvents[index];   
                _aggregatedEvents[index] = te with { State = evnt.State & te.State };
            }
            else
            {
                _aggregatedEvents.Add(te);
            }
        }
        
        foreach (var evnt in _aggregatedEvents)
        {
            SetPointer(GetTouchId(evnt.Id), evnt.State, evnt.Position);
        }

        _aggregatedEvents.Clear();
    }
    
    protected int GetTouchId(ulong fingerId)
    {
        if (!_touchIds.TryGetValue(fingerId, out var id))
        {
            id = _nextTouchId++;
            _touchIds.Add(fingerId, id);
        }

        if (_nextTouchId > int.MaxValue - 10)
        {
            _nextTouchId = MouseButtons.Middle + 1;
        }
        
        return id;
    }

    public void Reset()
    {
        foreach (var pointer in _pointers)
        {
            pointer.Update(ButtonState.Empty);
        }
    }
    
    public void UpdateHoverPosition(Vector2? position)
    {
        if ((Mode & PointingDevicesMode.Mouse) == 0)
        {
            ShowPointer(!position.HasValue);
            return;
        }

        HoverPosition = position;
        
        if (ShowHoverPointer)
        {
            ShowPointer(true);
        }
        else
        {
            ShowPointer(!position.HasValue);
        }
    }

    protected virtual void ShowPointer(bool show)
    {
    }

    public virtual void SetHoverPosition(Vector2 position)
    {
    }

    public void PushTransform(Matrix3x2 transform)
    {
        _hoverPositions.Push(HoverPosition);

        if (HoverPosition.HasValue)
        {
            HoverPosition = Vector2.Transform(HoverPosition.Value, transform);
        }
        
        foreach (var ptr in Pointers)
        {
            ptr.PushTransform(transform);
        }
    }

    public void PopTransform()
    {
        foreach (var ptr in Pointers)
        {
            ptr.PopTransform();
        }
        HoverPosition = _hoverPositions.Pop();
    }

    private record TouchEvent(ulong Id, ButtonState State, Vector2? Position);

    private readonly ConcurrentQueue<TouchEvent> _touchEvents = new();
    
    public void OnTouch(ulong id, ButtonState state, Vector2? position)
    {
        _touchEvents.Enqueue(new TouchEvent(id, state, position));
    }
}