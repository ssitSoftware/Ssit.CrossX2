using Ssit.CrossX2.Editor.Service;

namespace Ssit.CrossX2.Editor.Tools;

public class EmptyTool : EditorTool
{
    public new const string Name = "Empty";
    public EmptyTool(IEditorInstances instances) : base(Name, instances)
    {
    }
}