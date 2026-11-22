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
        /// <param name="startFolder">Folder the picker opens in, if it exists.</param>
        /// <param name="suggestedFileName">Initial file name (save picker only).</param>
        Task<string> ChooseFile(bool open, string title, string filters, string startFolder = null,
            string suggestedFileName = null);

        /// <summary>Folder picker; throws <see cref="TaskCanceledException"/> when cancelled.</summary>
        Task<string> ChooseFolder(string title, string startFolder = null);
    }
}