using System.Threading.Tasks;

namespace Ssit.CrossX2.Editor.Service
{
    public enum MessageBoxType
    {
        Ok,
        OkCancel,
        YesNoCancel,
        YesNo
    }

    public interface IWindowService
    {
        Task<bool?> ShowMessageBox(string title, string message, MessageBoxType type);
        Task ShowDialog<TViewModel>(object parameters = null);
        void CloseMainWindow();
    }

    public interface IFileService
    {
        Task<string> ChooseFile(bool open, string title, string filters);
    }
}