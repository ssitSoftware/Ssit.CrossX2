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
    public event Action<NarrationActionRequest> NarrationAction;
    
    private readonly Dictionary<string, NarrationObject> _objects = new();
    private readonly Dictionary<string, NarrationDialog> _specialDialogs = new();

    // Jump targets (Goto): texts with an Id and dialogs with an Id.
    private readonly Dictionary<string, NarrationEntry> _parts = new();

    private readonly Dictionary<string, string> _values = new();
    
    public NarrationSystem(IGameDialogs gameDialogs, IGameState gameState, IActionScheduler actionScheduler, IFilesProvider filesProvider, ITranslator translator, string narrationDir)
    {
        _dialogs = gameDialogs;
        _gameState = gameState;
        _actionScheduler = actionScheduler;
        _translator = translator;

        var list = NarrationParser.ParseObjects(filesProvider, narrationDir);
        foreach (var obj in list)
        {
            foreach (var dlg in obj.Dialogs)
            {
                RegisterParts(dlg.Entry);

                // A dialog with an Id is started by its Id or jumped to (Goto) - it isn't chosen for its subject.
                if (!string.IsNullOrWhiteSpace(dlg.Id))
                {
                    _specialDialogs.Add(dlg.Id, dlg);
                    AddPart(dlg.Id, dlg.Entry);
                }
            }

            if (string.IsNullOrWhiteSpace(obj.Name))
                continue;

            var dialogs = obj.Dialogs.Where(d => string.IsNullOrWhiteSpace(d.Id)).ToList();

            if (_objects.TryGetValue(obj.Name, out var narrationObject))
            {
                narrationObject.Concat(dialogs);
            }
            else
            {
                _objects.Add(obj.Name, new NarrationObject(obj.Name, dialogs));
            }
        }
    }

    /// <summary>
    /// Registers the texts with an Id in the <paramref name="entry"/> tree as jump targets.
    /// </summary>
    private void RegisterParts(NarrationEntry entry)
    {
        if (entry is null) return;

        if (!string.IsNullOrWhiteSpace(entry.Id))
        {
            AddPart(entry.Id, entry);
        }

        foreach (var option in entry.Options)
        {
            RegisterParts(option.Entry);
        }
    }

    private void AddPart(string id, NarrationEntry entry)
    {
        if (!_parts.TryAdd(id, entry))
        {
            Console.WriteLine($"Dialog part '{id}' is defined more than once - the first one is used.");
        }
    }

    /// <summary>
    /// The dialog part to jump to. Not found - null: the narration ends.
    /// </summary>
    private NarrationEntry GetPart(string id)
    {
        if (_parts.TryGetValue(id, out var entry))
            return entry;

        Console.WriteLine($"Dialog part '{id}' not found - the narration ends.");
        return null;
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
            entry = await ShowEntry(entry, dialog.DefaultLanguage);
        }
    }

    /// <summary>
    /// Shows the <paramref name="entry"/> until one of its options is taken.
    /// </summary>
    /// <returns>The entry to continue with: the handler's jump, the option's jump or its nested text. Null ends the
    /// narration.</returns>
    private async Task<NarrationEntry> ShowEntry(NarrationEntry entry, string defaultLanguage)
    {
        while (true)
        {
            var langId = _translator["#LangId"].ToString();

            var text = GetText(entry.Text, langId, defaultLanguage);
            
            var result = await _dialogs.ShowAsync(text,
                entry.Options.Select(o => GetText(o.Text, langId, defaultLanguage)).ToArray());
            
            // Negative result - the same text is shown again.
            if (result < 0)
                continue;

            var option = entry.Options[result];

            var request = string.IsNullOrWhiteSpace(option.Action)
                ? null
                : await ExecuteAction(option.Action);

            if (request is { Result: false })
            {
                // Refused - continue where the handler says, otherwise show this text with its options again.
                if (string.IsNullOrWhiteSpace(request.Goto))
                    continue;

                return GetPart(request.Goto);
            }

            if (!string.IsNullOrWhiteSpace(option.SetTags))
            {
                _gameState.SetFlags(option.SetTags);
            }

            // The handler's jump wins over the option's own jump and its nested text.
            var gotoId = !string.IsNullOrWhiteSpace(request?.Goto) ? request.Goto : option.Goto;

            return string.IsNullOrWhiteSpace(gotoId) ? option.Entry : GetPart(gotoId);
        }
    }

    /// <summary>
    /// Raises <see cref="NarrationAction"/> on the game thread and waits for it.
    /// </summary>
    /// <returns>The request with the handlers' <see cref="NarrationActionRequest.Result"/> (false when a handler
    /// failed) and <see cref="NarrationActionRequest.Goto"/>.</returns>
    private Task<NarrationActionRequest> ExecuteAction(string action)
    {
        var request = new NarrationActionRequest(action);
        var tcs = new TaskCompletionSource<NarrationActionRequest>();

        _actionScheduler.Schedule(() =>
        {
            try
            {
                NarrationAction?.Invoke(request);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Narration action '{action}' failed: {e.Message}");
                request.Result = false;
                request.Goto = null;
            }

            tcs.TrySetResult(request);
        });

        return tcs.Task;
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