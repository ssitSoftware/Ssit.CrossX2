using System.Globalization;
using System.Numerics;

namespace Ssit.CrossX2.Framework.Games;

public class TilesetMeta(Dictionary<(int, int), (Vector2[], string)> collisions)
{
    public IReadOnlyDictionary<(int, int), (Vector2[], string)> Collisions => collisions;

    public Vector2[] GetCollisionPolygon(int x, int y)
    {
        collisions.TryGetValue((x, y), out var polygon);
        return polygon.Item1;
    }

    public string GetMaterial(int x, int y)
    {
        collisions.TryGetValue((x, y), out var polygon);
        return polygon.Item2;
    }

    public static TilesetMeta FromStream(Stream stream)
    {
        if (stream == null)
        {
            return new TilesetMeta(new());
        }
        
        var reader = new StreamReader(stream);
        var data = reader.ReadToEnd();

        var lines = data.Split('\n', '\r');

        Dictionary<(int, int), (Vector2[], string)> dict = new();
        foreach (var line in lines)
        {
            ParseLine(line, dict);
        }

        return new TilesetMeta(dict);
    }

    private static void ParseLine(string line, Dictionary<(int, int), (Vector2[], string)> dict)
    {
        if (string.IsNullOrWhiteSpace(line))
            return;

        var parts = line.Split(':');
        
        var dims = parts[0].Trim('(', ')').Replace(" ", "");
        var dimensions = dims.Split(',');

        var x = 0;
        var y = 0;
        var w = 1;
        var h = 1;
        
        if (dimensions.Length >= 2)
        {
            x = int.Parse(dimensions[0]);
            y = int.Parse(dimensions[1]);
        }

        if (dimensions.Length >= 4)
        {
            w = int.Parse(dimensions[2]);
            h = int.Parse(dimensions[3]);
        }

        var material = "Default";
        if (parts.Length > 1)
        {
            material = parts[1].Trim();
            material = material.Replace('_', ' ');
        }

        Vector2[] figure = ParseFigure(parts.Length > 2 ? parts[2] : "Solid");
        FixFigure(ref figure);
        
        for (var xx = x; xx < x + w; ++xx)
        {
            for (var yy = y; yy < y + h; ++yy)
            {
                dict.Add((xx,yy), (figure, material));
            }
        }
    }

    private static void FixFigure(ref Vector2[] figure)
    {
        var list = figure.ToList();
        
        for (var idx = 0; idx < list.Count-1; )
        {
            if (list[idx] == list[idx + 1])
            {
                list.RemoveAt(idx);
                continue;
            }

            ++idx;
        }

        figure = list.ToArray();
    }

    private static (float, float) ParsePair(string data)
    {
        data = data.Trim(')', '(');
        var parts = data.Split(',');

        return (float.Parse(parts[0], CultureInfo.InvariantCulture), float.Parse(parts[1], CultureInfo.InvariantCulture));
    }
    
    private static Vector2[] ParseFigure(string figure)
    {
        figure = figure.Trim().ToLowerInvariant();
        
        var data = figure.Split(' ');
        
        switch(data[0])
        {
            default:
                return [
                    new Vector2(0,0),
                    new Vector2(0,1),
                    new Vector2(1,1),
                    new Vector2(1,0),
                ];

            case "bottom":
            {
                var (left, right) = ParsePair(data[1]);
                return
                [
                    new Vector2(0, 1),
                    new Vector2(0, left),
                    new Vector2(1, right),
                    new Vector2(1, 1)
                ];
            }

            case "stair":
            {
                var (left, right) = ParsePair(data[1]);
                var up = (left*3+right)/4;
                var mid = (left+right)/2;
                var down = (left+right*3)/4;
                
                return
                [
                    new Vector2(0, 1),
                    new Vector2(0, left),
                    new Vector2(1/8f, left),
                    new Vector2(2/8f, up),
                    new Vector2(3/8f, up),
                    new Vector2(4/8f, mid),
                    new Vector2(5/8f, mid),
                    new Vector2(6/8f, down),
                    new Vector2(7/8f, down),
                    new Vector2(1f, right),
                    new Vector2(1, 1)
                ];
            }

            case "top":
            {
                var (left, right) = ParsePair(data[1]);
                return
                [
                    new Vector2(0, 0),
                    new Vector2(0, left),
                    new Vector2(1, right),
                    new Vector2(1, 0)
                ];
            }
            
            case "topleft":
            {
                var (left, top) = ParsePair(data[1]);
                return
                [
                    new Vector2(0, 1),
                    new Vector2(0, left),
                    new Vector2(top, 0),
                    new Vector2(1, 0),
                    new Vector2(1, 1)
                ];
            }
            
            case "topright":
            {
                var (top, right) = ParsePair(data[1]);
                return
                [
                    new Vector2(0, 1),
                    new Vector2(0, 0),
                    new Vector2(right, 0),
                    new Vector2(1, top),
                    new Vector2(1, 1)
                ];
            }
            
            case "vertical":
            {
                var (left, right) = ParsePair(data[1]);
                return
                [
                    new Vector2(left, 1),
                    new Vector2(left, 0),
                    new Vector2(right, 0),
                    new Vector2(right, 1)
                ];
            }

            // TODO: Add more possibilities
        }
    }
}