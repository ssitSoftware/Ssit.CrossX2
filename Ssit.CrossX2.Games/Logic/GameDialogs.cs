using System.Windows.Input;
using Ssit.CrossX2.Framework.Commands;
using Ssit.CrossX2.Framework.Services;
using Ssit.CrossX2.Framework.UI.Common.Services;
using Ssit.CrossX2.Framework.UI.Values;

namespace Ssit.CrossX2.Framework.Games.Logic;

public class GameDialogs : GameDialogsBase, IGameDialogsUi
{
    private readonly IActionScheduler _actionScheduler;
    private readonly ITranslator _translator;
    public event Action<int> FocusElement;    
    public SharedBool Visible => _visible;
    public SharedString CurrentText => _currentText;
    public IReadOnlyList<SharedBool> ReplyOptionVisible => _replyOptionVisible;
    public IReadOnlyList<SharedString> ReplyOptions => _replyOptions;

    public override bool IsConversationActive => Visible.Value;
    
    public ICommand ReplyCommand => _replyCommand;
    
    private readonly SharedBoolMutable _visible = new(false);
    private readonly SharedStringValue _currentText = new("");
    
    private readonly SharedBoolMutable[] _replyOptionVisible =
    [
        new(false),
        new(false),
        new(false)
    ];

    private readonly SharedStringValue[] _replyOptions =
    [
        new(""),
        new(""),
        new("")
    ];

    private readonly SyncCommand _replyCommand;
    private bool _waitForOptions;

    public GameDialogs(IActionScheduler actionScheduler, ITranslator translator): base(actionScheduler)
    {
        _actionScheduler = actionScheduler;
        _translator = translator;
        _replyCommand = new SyncCommand(OnCommandReply, CanReply);
        
        _translator.LanguageChanged += TranslatorOnLanguageChanged;
    }

    private void TranslatorOnLanguageChanged()
    {
        OnCommandReply(null);
    }

    private bool CanReply(object arg)
    {
        if (_waitForOptions)
            return false;
        
        if (arg is int index)
        {
            return _replyOptionVisible[index].Value;
        }

        return false;
    }

    private void OnCommandReply(object obj)
    {
        ShouldHide = true;

        ActionScheduler.Schedule(() =>
        {
            if (!ShouldHide)
                return;

            foreach (var ro in _replyOptions)
            {
                ro.SetText("");
            }

            foreach (var rov in _replyOptionVisible)
            {
                rov.SetValue(false);
            }

            _currentText.SetText("");
            _visible.SetValue(false);
            
            _replyCommand.RaiseCanExecuteChanged();
            FocusElement?.Invoke(-1);
        });

        if (obj is int index)
        {
            OnReply(index);
        }
        else
        {
            OnReply(-1);
        }
    }

    protected override void SetValuesForDialog(string text, string[] replyOptions)
    {
        foreach (var ro in _replyOptions)
        {
            ro.SetText("");
        }

        foreach (var rov in _replyOptionVisible)
        {
            rov.SetValue(false);
        }

        for (var idx = 0; idx < replyOptions.Length; idx++)
        {
            _replyOptions[idx].SetText(replyOptions[idx]);
            _replyOptionVisible[idx].SetValue(true);
        }
        
        _waitForOptions = true;
        _replyCommand.RaiseCanExecuteChanged();
        
        FocusElement?.Invoke(-1);
        _visible.SetValue(true);
        
        _currentText.SetText("");
        _currentText.SetText(text);
        
        Task.Delay(500).ContinueWith(_ =>
        {
            _actionScheduler.Schedule(() =>
            {
                _waitForOptions = false;
                _replyCommand.RaiseCanExecuteChanged();
                FocusElement?.Invoke(0);
            });
        });
        
        _replyCommand.RaiseCanExecuteChanged();
    }

    public void Dispose()
    {
        _translator.LanguageChanged -= TranslatorOnLanguageChanged;
    }
}