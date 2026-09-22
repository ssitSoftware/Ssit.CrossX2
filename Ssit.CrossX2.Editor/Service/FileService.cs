using System.Collections.Generic;
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
    
    public async Task<string> ChooseFile(bool open, string title, string filters)
        {
            var names = filters.Split('|');

            var dlgFilters = new List<FilePickerFileType>();
        
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
            
                dlgFilters.Add(new FilePickerFileType(name)
                {
                    Patterns = new []{ $"*.{ext}"},
                });
            }

            string file = null;
        
            if (_applicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                if (open)
                {
                    var files = await desktop.MainWindow.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                    {
                        Title = title,
                        AllowMultiple = false,
                        FileTypeFilter = dlgFilters
                    });
                
                    file = files?.FirstOrDefault()?.Path.AbsolutePath;
                }
                else
                {
                    var retFile = await desktop.MainWindow.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
                    {
                        Title = title,
                        FileTypeChoices = dlgFilters,
                        ShowOverwritePrompt = true
                    });
                
                    file = retFile?.Path.AbsolutePath;
                }
            }

            if (file is null) throw new TaskCanceledException();
            return file;
        }
}