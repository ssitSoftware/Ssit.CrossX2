using System.Collections.Generic;
using System.Linq;
using Ssit.CrossX2.Editor.Tools;
using Ssit.CrossX2.Framework.Utils;

namespace Ssit.CrossX2.Editor.Service;

public class EditorTools: BindableModel, IEditorTools
{
    private readonly IEditorInstances _instances;

    public EditorTool Current
    {
        get;
        set
        {
            var old = field;
            if (SetField(ref field, value))
            {
                old?.OnFinished();
                _instances.Editor?.Redraw();
            }
        }
    }

    private readonly List<EditorTool> _tools = new();

    public EditorTools(IEditorInstances instances, IServices services)
    {
        _instances = instances;
        
        _tools.Add(services.Create<EmptyTool>());
        _tools.Add(services.Create<SelectionTool>());
        _tools.Add(services.Create<InsertTilesTool>());
        _tools.Add(services.Create<EraserTool>());
        _tools.Add(services.Create<InsertObjectTool>());
        _tools.Add(services.Create<InsertImageTool>());
        _tools.Add(services.Create<SetMaterialTool>());
    }

    public EditorTool GetTool(string tool) => _tools.FirstOrDefault( o=>o.Name == tool);

    public T GetTool<T>() where T: EditorTool
    {
        return (T)_tools.First( o => o.GetType() == typeof(T));
    }
}