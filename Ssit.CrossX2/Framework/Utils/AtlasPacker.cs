namespace Ssit.CrossX2.Framework.Utils;

/// <summary>
/// Packs variable-sized tile images tightly using a skyline bottom-left heuristic: the bin
/// width starts at sqrt(total area) so the result trends toward a square instead of a single
/// wide strip, images are placed tallest-first, and the final width/height are trimmed down
/// to whatever area ended up actually used.
/// </summary>
public static class AtlasPacker
{
    public class Result
    {
        public (int X, int Y)[] Positions { get; init; }
        public Size Size { get; init; }
    }

    private readonly record struct Segment(int X, int Y, int Width);

    public static Result Pack(RgbaColor[][,] images)
    {
        long totalArea = 0;
        var maxWidth = 0;

        foreach (var image in images)
        {
            totalArea += (long)image.GetLength(0) * image.GetLength(1);
            maxWidth = Math.Max(maxWidth, image.GetLength(0));
        }

        var binWidth = Math.Max(maxWidth, (int)Math.Ceiling(Math.Sqrt(totalArea)));

        var order = Enumerable.Range(0, images.Length)
            .OrderByDescending(idx => images[idx].GetLength(1))
            .ThenByDescending(idx => images[idx].GetLength(0))
            .ToArray();

        var skyline = new List<Segment> { new(0, 0, binWidth) };
        var positions = new (int X, int Y)[images.Length];

        var usedWidth = 0;
        var usedHeight = 0;

        foreach (var idx in order)
        {
            var w = images[idx].GetLength(0);
            var h = images[idx].GetLength(1);

            var position = Insert(skyline, binWidth, w, h);
            positions[idx] = position;

            usedWidth = Math.Max(usedWidth, position.X + w);
            usedHeight = Math.Max(usedHeight, position.Y + h);
        }

        return new Result
        {
            Positions = positions,
            Size = new Size(usedWidth, usedHeight)
        };
    }

    private static (int X, int Y) Insert(List<Segment> skyline, int binWidth, int width, int height)
    {
        var bestIndex = -1;
        var bestY = int.MaxValue;

        for (var i = 0; i < skyline.Count; i++)
        {
            if (!TryFit(skyline, binWidth, i, width, out var y)) continue;
            if (y >= bestY) continue;

            bestY = y;
            bestIndex = i;
        }

        if (bestIndex < 0)
            throw new InvalidOperationException("Tile image does not fit into the atlas.");

        var bestX = skyline[bestIndex].X;
        Place(skyline, bestX, bestY, width, height);
        return (bestX, bestY);
    }

    private static bool TryFit(List<Segment> skyline, int binWidth, int index, int width, out int y)
    {
        var x = skyline[index].X;
        y = 0;

        if (x + width > binWidth)
            return false;

        var remaining = width;
        var i = index;

        while (remaining > 0)
        {
            if (i >= skyline.Count)
                return false;

            y = Math.Max(y, skyline[i].Y);
            remaining -= skyline[i].Width;
            i++;
        }

        return true;
    }

    private static void Place(List<Segment> skyline, int x, int y, int width, int height)
    {
        var right = x + width;

        var index = 0;
        while (skyline[index].X < x)
            index++;

        while (index < skyline.Count && skyline[index].X < right)
        {
            var segment = skyline[index];
            var segmentRight = segment.X + segment.Width;

            if (segmentRight <= right)
            {
                skyline.RemoveAt(index);
            }
            else
            {
                skyline[index] = segment with { X = right, Width = segmentRight - right };
                break;
            }
        }

        skyline.Insert(index, new Segment(x, y + height, width));
        MergeAdjacent(skyline);
    }

    private static void MergeAdjacent(List<Segment> skyline)
    {
        for (var i = 0; i < skyline.Count - 1; i++)
        {
            if (skyline[i].Y != skyline[i + 1].Y) continue;

            skyline[i] = skyline[i] with { Width = skyline[i].Width + skyline[i + 1].Width };
            skyline.RemoveAt(i + 1);
            i--;
        }
    }
}
