using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Ssit.CrossX2.Editor.Input;
using Ssit.CrossX2.Editor.Models;
using Ssit.CrossX2.Editor.Views;

namespace Ssit.CrossX2.Editor.Service;

public class WindowService: IWindowService
{
    private readonly IServices _services;
    private readonly IApplicationLifetime _applicationLifetime;

    public WindowService(IServices services, IApplicationLifetime applicationLifetime)
    {
        _services = services;
        _applicationLifetime = applicationLifetime;
    }

    public async Task<bool?> ShowMessageBox(string title, string message, MessageBoxType type)
    {
        if (_applicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop) return null;
        var window = desktop.MainWindow;

        if (window is null) return null;

        var dlg = new MessageBox();
        
        dlg.DataContext = new MessageBoxModel
        {
            Title = title,
            Message = message,
            ShowYes = type == MessageBoxType.YesNo || type == MessageBoxType.YesNoCancel,
            ShowNo = type == MessageBoxType.YesNo || type == MessageBoxType.YesNoCancel,
            ShowCancel = type == MessageBoxType.OkCancel || type == MessageBoxType.YesNoCancel,
            ShowOk = type == MessageBoxType.Ok || type == MessageBoxType.OkCancel
        };
        
        try
        {
            return await dlg.ShowDialog<bool?>(window);
        }
        catch
        {
            return null;
        }
    }

    public Task ShowDialog<TViewModel>(object parameters = null)
    {
        if (_applicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop) return Task.CompletedTask;

        var parent = desktop.MainWindow;
        var viewModel = _services.Create<TViewModel>(parameters);

        var windowName = typeof(TViewModel).Name.Replace("ViewModel", "");
        windowName = "Ssit.CrossX2.Editor.Views." + windowName;

        var windowType = GetType().Assembly.GetType(windowName);
        var window = Activator.CreateInstance(windowType) as Window;
        window.DataContext = viewModel;

        if (viewModel is IDialog dialog)
        {
            dialog.RequestClose += window.Close;
        }
        
        return window.ShowDialog(parent);
    }

    public void CloseMainWindow()
    {
        if (_applicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop) return;
        desktop.MainWindow?.Close();
    }
}