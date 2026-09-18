using Ssit.CrossX2.IO;
using Ssit.CrossX2.UI.Values;

namespace Ssit.CrossX2.UI.Common.Services;

internal class Translator : ITranslator
{
    private readonly Dictionary<string, SharedStringValue> _strings = new();

    private readonly Dictionary<string, string>[] _languages;
    private int _language;
    
    public int CurrentLanguage
    {
        get => _language;
        set
        {
            _language = value % _languages.Length;
            UpdateLanguage(_languages[_language]);
        }
    }
    
    public Translator(IFilesProvider filesProvider, string[] paths)
    {
        var list = new List<Dictionary<string, string>>();
        
        foreach (var path in paths)
        {
            using var stream = filesProvider.Open(path);
            var text = new StreamReader(stream).ReadToEnd();

            var lines = text.Split('\n', '\r');
            
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                ParseLine(line, list);
            }
        }

        _languages = list.ToArray();
        UpdateLanguage(_languages[_language]);
    }
    
    public Translator(IFilesProvider filesProvider, string path)
    {
        using var stream = filesProvider.Open(path);
        var text = new StreamReader(stream).ReadToEnd();
        
        var lines = text.Split('\n', '\r');
        var list = new List<Dictionary<string, string>>();

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            ParseLine(line, list);
        }

        _languages = list.ToArray();
        UpdateLanguage(_languages[_language]);
    }

    private void ParseLine(string line, List<Dictionary<string, string>> list)
    {
        var parts = line.Split('\t');
        var key = parts[0];
        
        for(var idx = 1; idx < parts.Length; idx++)
        {
            var value = parts[idx];
            var listId = idx - 1;
            
            while (list.Count <= listId)
            {
                list.Add(new Dictionary<string, string>());
            }
            list[listId].Add(key, value.Replace('|', '\n'));
        }
    }

    public void ToggleLanguage(bool previous = false)
    {
        if (previous)
        {
            _language = (_language + _languages.Length - 1) % _languages.Length;
        }
        else
        {
            _language = (_language + 1) % _languages.Length;
        }

        UpdateLanguage(_languages[_language]);
        LanguageChanged?.Invoke();
    }

    private void UpdateLanguage(Dictionary<string, string> dict)
    {
        foreach (var val in dict)
        {
            if (!_strings.TryGetValue(val.Key, out var str))
            {
                str = new SharedStringValue();
                _strings.Add(val.Key, str);
            }
            str.SetText(val.Value);
        }
    }

    public event Action LanguageChanged;

    public SharedString this[string key]
    {
        get
        {
            if (_strings.TryGetValue(key, out var str))
            {
                return str;
            }

            return key;
        }   
    }
}