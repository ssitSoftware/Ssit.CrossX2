using Ssit.CrossX2.Framework.IO;
using Ssit.CrossX2.Framework.Services;
using Ssit.CrossX2.Framework.UI.Common.Services;

namespace Ssit.CrossX2.Framework.Games.Logic.Narration;

public class NarrationSystem: INarrationSystem
{
    private readonly IGameDialogs _dialogs;
    private readonly IGameState _gameState;
    private readonly IActionScheduler _actionScheduler;
    private readonly ITranslator _translator;
    public event Action<string> NarrationAction;
    
    private readonly Dictionary<string, NarrationObject> _objects = new();
    private readonly Dictionary<string, NarrationDialog> _specialDialogs = new();

    private readonly Dictionary<string, string> _values = new();
    
    public NarrationSystem(IGameDialogs dialogs, IGameState gameState, IActionScheduler actionScheduler, IFilesProvider filesProvider, ITranslator translator, string narrationDir)
    {
        _dialogs = dialogs;
        _gameState = gameState;
        _actionScheduler = actionScheduler;
        _translator = translator;

        var list = NarrationParser.ParseObjects(filesProvider, narrationDir);
        foreach (var obj in list)
        {
            if (string.IsNullOrWhiteSpace(obj.Name))
            {
                foreach(var dlg in obj.Dialogs)
                {
                    if (!string.IsNullOrWhiteSpace(dlg.Id))
                    {
                        _specialDialogs.Add(dlg.Id, dlg);
                    }
                }
            }
            else
            {
                if (_objects.TryGetValue(obj.Name, out var narrationObject))
                {
                    narrationObject.Concat(obj.Dialogs);
                }
                else
                {
                    _objects.Add(obj.Name, obj);
                }
            }
        }
    }

    private string GetText(Dictionary<string, string> dict, string langId, string defaultId)
    {
        if (!dict.TryGetValue(langId, out var text))
        {
            text = dict.GetValueOrDefault(defaultId, "????");
        }

        foreach (var vals in _values)
        {
            if (text.Contains(vals.Key))
            {
                text = text.Replace(vals.Key, _translator[vals.Value].ToString());
            }
        }
        
        return text;
    }
    
    public bool HasNarration(string subject)
    {
        var dialog = GetDialog(subject) ?? GetSpecialConversation(subject);
        return dialog != null;
    }

    public INarrationSystem SetValue(string key, string value)
    {
        _values['{'+key+'}'] = value;
        return this;
    }

    public async Task StartNarration(string subject)
    {
        var dialog = GetDialog(subject);
        if (dialog == null)
        {
            dialog = GetSpecialConversation(subject);
            if (dialog is null)
            {
                return;
            }
        }

        var entry = dialog.Entry;

        while (entry != null)
        {
            bool reload = true;
            int result = 0;
            
            while (reload)
            {
                var langId = _translator["#LangId"].ToString();

                var text = GetText(entry.Text, langId, dialog.DefaultLanguage);
                
                result = await _dialogs.ShowAsync(text,
                    entry.Options.Select(o => GetText(o.Text, langId, dialog.DefaultLanguage)).ToArray());
                
                reload = result < 0;
            }

            var action = entry.Options[result].Action;
            var tags = entry.Options[result].SetTags;

            if (!string.IsNullOrWhiteSpace(tags))
            {
                _gameState.SetFlags(tags);
            }

            if (!string.IsNullOrWhiteSpace(action))
            {
                _actionScheduler.Schedule(() => NarrationAction?.Invoke(action));
            }
            
            entry = entry.Options[result].Entry;
        }
    }

    private NarrationDialog GetSpecialConversation(string id)
    {
        _specialDialogs.TryGetValue(id, out var dialog);
        return IsDialogValid(dialog) ? dialog : null;
    }

    private bool IsDialogValid(NarrationDialog dialog)
    {
        if (dialog is null)
            return false;
        
        foreach (var tag in dialog.On)
        {
            if (!_gameState.HasFlag(tag))
            {
                return false;
            }
        }

        foreach (var tag in dialog.Off)
        {
            if (_gameState.HasFlag(tag))
            {
                return false;
            }
        }
        return true;
    }

    private NarrationDialog GetDialog(string subject)
    {
        if (!_objects.TryGetValue(subject, out var narrationObject))
        {
            return null;
        }
        
        foreach (var dialog in narrationObject.Dialogs.Reverse())
        {
            if (IsDialogValid(dialog))
            {
                return dialog.Entry is not null ? dialog : null;
            }
        }

        return null;
    }

    public bool HasRequest(string subject)
    {
        var dialog = GetDialog(subject);

        if (true == dialog?.Highlight)
        {
            //var key = GetDialogKey(subject, dialog.On);
            return true;//!_gameState.HasFlag(key);
        }

        return false;
    }
}