using System.Text.RegularExpressions;

namespace Ssit.CrossX2.Framework.UI.Common.Services;

/// <summary>
/// Typographic rules which glue words that must not be left hanging at the end of a line to the next word with a
/// no-break space (' '). The <see cref="Translator"/> applies them to every translated string when it loads, so
/// the .tsv files can keep plain spaces.
/// <para>
/// Glued are: every word of at most <see cref="MaxShortWordLength"/> letters (prepositions, conjunctions, articles -
/// "a", "w", "do", "le", ...), the longer function words listed per language in <see cref="Words"/> and a spaced dash
/// (" - ", " – ", " — ") which is glued to the word before it, so a line never starts with it.
/// </para>
/// <para>
/// The rules are idempotent - text which already contains no-break spaces is fine. Change <see cref="Enabled"/>,
/// <see cref="MaxShortWordLength"/> or <see cref="Words"/> before the translator is created.
/// </para>
/// </summary>
public static class NoBreakSpaces
{
    public const char NoBreakSpace = ' ';

    /// <summary>
    /// Turns the rules on or off. Default: on.
    /// </summary>
    public static bool Enabled { get; set; } = true;

    /// <summary>
    /// Any word with at most this many letters is glued to the next word. Default: 2.
    /// </summary>
    public static int MaxShortWordLength { get; set; } = 2;

    /// <summary>
    /// Longer words glued to the next word, per language id (the #LangId column of the languages file).
    /// Compared case-insensitive.
    /// </summary>
    public static Dictionary<string, HashSet<string>> Words { get; } = new(StringComparer.OrdinalIgnoreCase)
    {
        ["en"] = Set("the and for but its nor"),
        ["pl"] = Set("nie bez nad pod lub czy ich jej jak gdy"),
        ["cs"] = Set("pro při nad pod bez před jak ale než"),
        ["sk"] = Set("pre pri nad pod bez pred ako ale než"),
        ["de"] = Set("der die das den dem des und ein von mit aus bei für vor oft ihr"),
        ["es"] = Set("los las del que con sin sus una por tras"),
        ["fr"] = Set("les des une aux par que sur ses son dans"),
        ["el"] = Set("στους για που την τον του της των τις τους στο στη στην στον στις στα και από ένα μια"),
        ["uk"] = Set("від при без над під для але або щоб їх цей"),
    };

    // A whole word (not the tail of "l'ordre" or "ім'я", not part of a hyphenated word) followed by a single space
    // and a visible character.
    private static readonly Regex WordBeforeSpace = new(@"(?<![\w'’\-])(\w+) (?=\S)", RegexOptions.Compiled);

    // A spaced dash - the space before it becomes a no-break space.
    private static readonly Regex SpacedDash = new(@" (?=[-–—] )", RegexOptions.Compiled);

    /// <summary>
    /// Returns <paramref name="text"/> with the no-break space rules of the language <paramref name="langId"/>
    /// applied. Words of an unknown (or null) language get only the short word and dash rules.
    /// </summary>
    public static string Apply(string text, string langId)
    {
        if (!Enabled || string.IsNullOrEmpty(text) || text.IndexOf(' ') < 0) return text;

        HashSet<string> words = null;
        if (langId != null) Words.TryGetValue(langId, out words);

        text = SpacedDash.Replace(text, NoBreakSpace.ToString());

        return WordBeforeSpace.Replace(text, match =>
        {
            var word = match.Groups[1].Value;
            var glue = word.Length <= MaxShortWordLength || words?.Contains(word) == true;
            return glue ? word + NoBreakSpace : match.Value;
        });
    }

    private static HashSet<string> Set(string words) =>
        new(words.Split(' ', StringSplitOptions.RemoveEmptyEntries), StringComparer.OrdinalIgnoreCase);
}
