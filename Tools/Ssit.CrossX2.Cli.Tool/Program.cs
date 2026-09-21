using System.Diagnostics;
using SkiaSharp;
using Ssit.CrossX2.Framework.Xml;

namespace Ssit.CrossX.Tool;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        var dir = Directory.GetCurrentDirectory();

        if (args.Length == 0)
        {
            Console.WriteLine("Usage: cxtool <filename>");
            return -1;
        }

        var filenames = args;

        List<Task> tasks = new List<Task>();

        var timer = new Stopwatch();
        timer.Start();

        foreach (var filename in filenames)
        {
            foreach (var file in Directory.GetFiles(dir, filename, SearchOption.AllDirectories))
            {
                var fullPath = file;
                var ext = Path.GetExtension(file);

                switch (ext.ToLowerInvariant())
                {
                    case ".xml":
                        var converter = GetXmlConverter(fullPath);
                        if (converter is null)
                            continue;

                        Console.WriteLine(fullPath);
                        tasks.Add(converter.Generate());
                        break;
                    
                    case ".png":
                        if (file.EndsWith("_to1bit.png"))
                        {
                            tasks.Add(Task.Run(() =>
                                OneBitImageTool.ConvertTo1BitAndAddOutline(fullPath, SKColors.White, SKColors.Black)));
                        }
                        break;
                }
            }
        }

        await Task.WhenAll(tasks);

        var time = timer.Elapsed;
        Console.WriteLine($"Generation time: {time.Minutes:00}:{time.Seconds:00}:{time.Milliseconds:000}");
        
        return 0;
    }

    private static IXmlFileConverter GetXmlConverter(string fullPath)
    {
        using var stream = File.OpenRead(fullPath);
        var xmlNode = XNode.ReadXml(stream);

        switch (xmlNode.Tag)
        {
            case "Fonts":
                return new XmlToFontConverter(fullPath, xmlNode);
        }

        return null;
    }
}