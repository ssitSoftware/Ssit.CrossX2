using System.ComponentModel;
using Ssit.CrossX2.Editor.Tools;

namespace Ssit.CrossX2.Editor.Service
{
    public interface IEditorTools: INotifyPropertyChanged
    {
        EditorTool Current { get; set; }
        EditorTool GetTool(string tool);
        T GetTool<T>() where T : EditorTool;
    }
}