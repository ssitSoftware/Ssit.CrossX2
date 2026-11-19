using System.Diagnostics;
using Ssit.CrossX2.Framework.Xml;

namespace Ssit.CrossX2.PixelFontGen;

/// <summary>
/// Single file generator: takes one fonts XML file and generates
/// &lt;Name&gt;@&lt;Size&gt;.font / .png pairs plus &lt;XmlName&gt;.json next to it.
///
/// Usage: pixelfontgen &lt;fonts.xml&gt; [-o|--output &lt;directory&gt;]
/// </summary>
public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        string inputPath = null;
        string outputDir = null;

        for (var idx = 0; idx < args.Length; idx++)
        {
            switch (args[idx])
            {
                case "-o":
                case "--output":
                    if (idx + 1 >= args.Length)
                        return Usage("Missing value for " + args[idx]);
                    outputDir = args[++idx];
                    break;

                case "-h":
                case "--help":
                    return Usage(null);

                default:
                    if (inputPath != null)
                        return Usage("Only one input file is supported.");
                    inputPath = args[idx];
                    break;
            }
        }

        if (inputPath == null)
            return Usage("No input file given.");

        var fullPath = Path.GetFullPath(inputPath);

        if (!File.Exists(fullPath))
        {
            await Console.Error.WriteLineAsync($"{fullPath}: error: file not found.");
            return 2;
        }

        outputDir = Path.GetFullPath(outputDir ?? Path.GetDirectoryName(fullPath) ?? ".");
        Directory.CreateDirectory(outputDir);

        var timer = Stopwatch.StartNew();

        try
        {
            XNode xmlNode;
            await using (var stream = File.OpenRead(fullPath))
            {
                xmlNode = XNode.ReadXml(stream);
            }

            if (xmlNode.Tag != "Fonts")
            {
                await Console.Error.WriteLineAsync($"{fullPath}: error: root element must be <Fonts>, found <{xmlNode.Tag}>.");
                return 3;
            }

            Console.WriteLine(fullPath);

            var generator = new FontGenerator(fullPath, outputDir, xmlNode);
            var files = await generator.Generate();

            foreach (var file in files)
                Console.WriteLine("  -> " + file);
        }
        catch (Exception ex)
        {
            await Console.Error.WriteLineAsync($"{fullPath}: error: {ex.Message}");
            await Console.Error.WriteLineAsync(ex.ToString());
            return 1;
        }

        var time = timer.Elapsed;
        Console.WriteLine($"Generation time: {time.Minutes:00}:{time.Seconds:00}:{time.Milliseconds:000}");

        return 0;
    }

    private static int Usage(string error)
    {
        if (error != null)
            Console.Error.WriteLine("error: " + error);

        Console.WriteLine("Usage: pixelfontgen <fonts.xml> [-o|--output <directory>]");
        Console.WriteLine("  Generates <Name>@<Size>.font and .png for every <Font> element,");
        Console.WriteLine("  plus <xml-name>.json listing the generated fonts.");
        Console.WriteLine("  Output goes next to the XML file unless --output is given.");

        return error == null ? 0 : -1;
    }
}
