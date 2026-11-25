using Ssit.CrossX2.Framework.IO;
using Ssit.CrossX2.Framework.UI.Values;

namespace Ssit.CrossX2.Framework.UI.Common.Services;

internal class Translator : ITranslator
{
    private const string LangIdKey = "#LangId";
    private const string DefaultExtension = ".tsv";
    
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
            LoadWithLanguageFiles(filesProvider, path, list);
        }

        ApplyNoBreakSpaces(list);
        FillMissingTranslations(list);

        _languages = list.ToArray();
        UpdateLanguage(_languages[_language]);
    }
    
    public Translator(IFilesProvider filesProvider, string path) : this(filesProvider, [path])
    {
    }

    private static void LoadWithLanguageFiles(IFilesProvider filesProvider, string path, List<Dictionary<string, string>> list)
    {
        var loaded = false;

        // Original file (optional) - fills all columns.
        if (filesProvider.FileExists(path))
        {
            foreach (var line in ReadLines(filesProvider, path))
            {
                ParseLine(line, list);
            }
            loaded = true;
        }

        // Per language files: {name}.{langId}.tsv - override single column.
        for (var langIdx = 0; langIdx < list.Count; langIdx++)
        {
            if (!list[langIdx].TryGetValue(LangIdKey, out var langId) || string.IsNullOrWhiteSpace(langId)) continue;

            var langPath = GetLanguageFilePath(path, langId.Trim());
            if (!filesProvider.FileExists(langPath)) continue;

            foreach (var line in ReadLines(filesProvider, langPath))
            {
                ParseLanguageLine(line, list[langIdx]);
            }
            loaded = true;
        }

        if (!loaded)
        {
            throw new FileNotFoundException($"Translation file {path} not found (neither the file nor any of its .{{langId}}{DefaultExtension} variants).");
        }
    }

    private static string[] ReadLines(IFilesProvider filesProvider, string path)
    {
        using var stream = filesProvider.Open(path);
        using var reader = new StreamReader(stream);
        var text = reader.ReadToEnd();

        return text
            .Split('\n', '\r')
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToArray();
    }

    private static string GetLanguageFilePath(string path, string langId)
    {
        var slash = Math.Max(path.LastIndexOf('/'), path.LastIndexOf('\\'));
        var dot = path.LastIndexOf('.');

        if (dot <= slash)
        {
            return $"{path}.{langId}{DefaultExtension}";
        }

        return $"{path[..dot]}.{langId}{path[dot..]}";
    }

    private static void ParseLine(string line, List<Dictionary<string, string>> list)
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

    private static void ParseLanguageLine(string line, Dictionary<string, string> language)
    {
        var parts = line.Split('\t');
        if (parts.Length < 2) return;

        language[parts[0]] = parts[1].Replace('|', '\n');
    }

    /// <summary>
    /// Applies the <see cref="NoBreakSpaces"/> rules of each language to all its strings (keys starting with '#',
    /// like #LangId, are left as they are). Runs before <see cref="FillMissingTranslations"/>, so a fallback keeps
    /// the rules of the language it comes from.
    /// </summary>
    private static void ApplyNoBreakSpaces(List<Dictionary<string, string>> list)
    {
        foreach (var language in list)
        {
            language.TryGetValue(LangIdKey, out var langId);
            langId = langId?.Trim();

            foreach (var key in language.Keys.ToArray())
            {
                if (key.StartsWith('#')) continue;
                language[key] = NoBreakSpaces.Apply(language[key], langId);
            }
        }
    }

    private static void FillMissingTranslations(List<Dictionary<string, string>> list)
    {
        var keys = list.SelectMany(o => o.Keys).Distinct().ToArray();

        foreach (var key in keys)
        {
            // Fallback: first language (in column order) that has the key.
            string fallback = null;
            foreach (var language in list)
            {
                if (language.TryGetValue(key, out fallback)) break;
            }

            foreach (var language in list)
            {
                language.TryAdd(key, fallback ?? key);
            }
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
