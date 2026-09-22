using System.Collections.Generic;
using System.Linq;
using Ssit.CrossX2.Editor.Helpers;

namespace Ssit.CrossX2.Editor.Service;

public interface IUndoRedoService
{
    void PushState();
    void Undo();
    void Redo();
    void Clear();
}

public class UndoRedoService : IUndoRedoService
{
    private List<UndoRedoEntry> _undoEntries = new();
    private Stack<UndoRedoEntry> _redoEntries = new();

    private readonly IEditorInstances _instances;

    public UndoRedoService(IEditorInstances instances)
    {
        _instances = instances;
    }
    
    public void PushState()
    {
        _redoEntries.Clear();
        PushInternal();
    }

    public void Undo()
    {
        if (_undoEntries.Count > 0)
        {
            var entry = new UndoRedoEntry(_instances);
            _redoEntries.Push(entry);

            entry = _undoEntries.Last();
            _undoEntries.Remove(entry);
            
            entry.Restore();
        }
    }

    public void Redo()
    {
        if (_redoEntries.Count > 0)
        {
            PushInternal();
            var entry = _redoEntries.Pop();
            entry.Restore();
        }
    }

    public void Clear()
    {
        _redoEntries.Clear();
        _undoEntries.Clear();
    }

    private void PushInternal()
    {
        if (_undoEntries.Count > 99)
        {
            _undoEntries.RemoveRange(0, _undoEntries.Count - 99);
        }
        
        var entry = new UndoRedoEntry(_instances);
        _undoEntries.Add(entry);
    }
}