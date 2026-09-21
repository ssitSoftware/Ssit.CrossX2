using Ssit.CrossX2.Framework.IO;
using Ssit.CrossX2.Framework.Xml;

namespace Ssit.CrossX2.Framework.Games.Logic.Narration;

internal static class NarrationParser
{
    public static IEnumerable<NarrationObject> ParseObjects(IFilesProvider filesProvider, string narrationDir)
    {
        var files = filesProvider.GetFiles(narrationDir, "xml");
    
        foreach (var file in files)
        {
            using var stream = filesProvider.Open(file);
            var obj = Parse(stream);
            if (obj != null)
            {
                yield return obj;
            }
        }
    }

    public static NarrationObject Parse(Stream stream)
    {
        var node = XNode.ReadXml(stream);

        if (node.Tag != "Dialogs")
            return null;
        
        var name = node.Attribute("Name");
        var defaultLang = node.Attribute("DefaultLanguage") ?? "en";
        
        var dialogs = new List<NarrationDialog>();
        foreach (var cn in node.Nodes)
        {
            var dialog = ParseDialog(cn, defaultLang);
            if (dialog != null)
            {
                dialogs.Add(dialog);
            }
        }
        return new NarrationObject(name, dialogs);
    }

    private static NarrationDialog ParseDialog(XNode node, string defaultLanguage)
    {
        if (node.Tag != "Dialog")
            return null;

        if (node.Nodes.Count != 1)
            throw new InvalidDataException();

        var highlight = node.Attribute("Highlight")?.ToLowerInvariant() == "true";
        
        var id = node.Attribute("Id");
        var on = new HashSet<string>(node.Attribute("On")?.Split('|') ?? []);
        var off = new HashSet<string>(node.Attribute("Off")?.Split('|') ?? []);

        var defaultLang = node.Attribute("DefaultLanguage") ?? defaultLanguage;
        
        var entry = ParseEntry(node.Nodes.FirstOrDefault());
        return new NarrationDialog(id, on, off, highlight, defaultLang, entry);
    }

    private static bool IsSpecialTag(string tag)
    {
        switch (tag)
        {
            case "Option":
            case "Text":
                return true;
        }
        return false;
    }

    private static Dictionary<string, string> GetTexts(XNode node)
    {
        var dict = new Dictionary<string, string>();
        foreach (var cn in node.Nodes)
        {
            if (IsSpecialTag(cn.Tag))
                continue;
            
            var text = cn.Value.Trim('\n', ' ', '\t', '\r').Replace('|', '\n');
            if (string.IsNullOrWhiteSpace(text))
                continue;
            
            dict.Add(cn.Tag, text);
        }

        return dict;
    }
    
    private static NarrationEntry ParseEntry(XNode node)
    {
        if ("Text" != node?.Tag)
            return null;

        var text = GetTexts(node);
        
        var list = new List<NarrationOption>();

        foreach (var cn in node.Nodes)
        {
            var option = ParseOption(cn);
            if (option != null)
            {
                list.Add(option);
            }
        }

        if (list.Count == 0)
            return null;
        
        return new NarrationEntry(text, list.ToArray());
    }

    private static NarrationOption ParseOption(XNode node)
    {
        if (node.Tag != "Option")
            return null;
        
        var action = node.Attribute("Action");
        var set = node.Attribute("Set");
        var text = GetTexts(node);

        NarrationEntry entry = null;
        
        if(node.Nodes.Count(o => o.Tag == "Text") > 1)
            throw new InvalidDataException();

        var entryNode = node.Nodes.FirstOrDefault(o => o.Tag == "Text");
        if (entryNode != null)
        {
            entry = ParseEntry(entryNode);
        }
        
        return new NarrationOption(text, entry, action, set);
    }
}