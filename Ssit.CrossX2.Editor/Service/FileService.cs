using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;

namespace Ssit.CrossX2.Editor.Service;

public class FileService: IFileService
{
    private readonly IApplicationLifetime _applicationLifetime;

    public FileService(IApplicationLifetime applicationLifetime)
    {
        _applicationLifetime = applicationLifetime;
    }

    public async Task<string> ChooseFile(bool open, string title, string filters, string startFolder = null,
        string suggestedFileName = null)
        {
            var names = filters.Split('|');

            var dlgFilters = new List<FilePickerFileType>();
            string defaultExtension = null;

            for (var idx = 0; idx < names.Length; ++idx)
            {
                var parts = names[idx].Split('(');
                var ext = "*";

                var name = parts[0];

                if (parts.Length > 1)
                {
                    parts = parts[1].Split('.');
                    if (parts.Length > 1)
                    {
                        if (parts[0] != "")
                        {

                        }
                        ext = parts[1].Trim(')').Trim();
                    }
                }

                if (defaultExtension is null && ext != "*")
                {
                    defaultExtension = ext;
                }

                dlgFilters.Add(new FilePickerFileType(name)
                {
                    Patterns = new []{ $"*.{ext}"},
                });
            }

            string file = null;

            if (_applicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                // The active window can be a dialog (e.g. Extract prefab) - open the picker over it.
                var owner = desktop.Windows.FirstOrDefault(w => w.IsActive) ?? desktop.MainWindow;
                var storageProvider = owner.StorageProvider;

                IStorageFolder suggestedStartLocation = null;
                if (!string.IsNullOrWhiteSpace(startFolder) && Directory.Exists(startFolder))
                {
                    suggestedStartLocation = await storageProvider.TryGetFolderFromPathAsync(startFolder);
                }

                if (open)
                {
                    var files = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                    {
                        Title = title,
                        AllowMultiple = false,
                        FileTypeFilter = dlgFilters,
                        SuggestedStartLocation = suggestedStartLocation
                    });

                    var selected = files?.FirstOrDefault();
                    file = selected?.TryGetLocalPath() ?? selected?.Path.AbsolutePath;
                }
                else
                {
                    var retFile = await storageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
                    {
                        Title = title,
                        FileTypeChoices = dlgFilters,
                        ShowOverwritePrompt = true,
                        SuggestedStartLocation = suggestedStartLocation,
                        SuggestedFileName = suggestedFileName,
                        DefaultExtension = defaultExtension
                    });

                    file = retFile?.TryGetLocalPath() ?? retFile?.Path.AbsolutePath;
                }
            }

            if (file is null) throw new TaskCanceledException();
            return file;
        }
}