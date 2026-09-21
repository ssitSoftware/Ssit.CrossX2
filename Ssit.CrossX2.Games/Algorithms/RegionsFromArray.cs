namespace Ssit.CrossX2.Framework.Games.Algorithms
{
    public class RegionsFromArray<TValue>
    {
        public interface IMatcher
        {
            bool Match(TValue origin, int x, int y, TValue value);
            bool IsEmpty(TValue value);
            TValue Empty { get; }
        }

        public delegate void CreateRegion(TValue value, int x, int y, int width, int height, object context);

        public int MaxWidth = int.MaxValue;
        public int MaxHeight = int.MaxValue;

        public void GenerateRegions(TValue[,] array, Rectangle? rect, IMatcher matcher, CreateRegion creator, object context)
        {
            int maxX = rect?.Right ?? array.GetLength(0);
            int maxY = rect?.Bottom ?? array.GetLength(1);

            int startX = rect?.X ?? 0;
            int startY = rect?.Y ?? 0;

            for (int idxY = startY; idxY < maxY; ++idxY)
            {
                for (int idxX = startX; idxX < maxX; ++idxX)
                {
                    TValue value = array[idxX, idxY];

                    if (matcher.IsEmpty(value)) continue;

                    int width = 0;
                    int height = 0;

                    bool canDoWidth = true;
                    bool canDoHeight = true;

                    while (canDoWidth || canDoHeight)
                    {
                        if (canDoHeight)
                        {
                            height++;
                            for (var xx = 0; xx < width; ++xx)
                            {
                                for (var yy = 0; yy < height; ++yy)
                                {
                                    if (!matcher.Match(value, xx, yy, array[xx + idxX, yy + idxY]))
                                    {
                                        height--;
                                        canDoHeight = false;
                                        break;
                                    }
                                }
                            }

                            if (height + idxY == maxY || height == MaxHeight)
                            {
                                canDoHeight = false;
                            }
                        }


                        if (canDoWidth)
                        {
                            width++;
                            for (var xx = 0; xx < width; ++xx)
                            {
                                for (var yy = 0; yy < height; ++yy)
                                {
                                    if (!matcher.Match(value, xx, yy, array[xx + idxX, yy + idxY]))
                                    {
                                        width--;
                                        canDoWidth = false;
                                        break;
                                    }
                                }
                            }
                            if (width + idxX == maxX || width == MaxWidth)
                            {
                                canDoWidth = false;
                            }
                        }
                    }

                    for (int xx = 0; xx < width; ++xx)
                    {
                        for (int yy = 0; yy < height; ++yy)
                        {
                            if (yy == 0 && xx == 0) continue;

                            array[idxX + xx, idxY + yy] = matcher.Empty;
                        }
                    }

                    if (height == 0 || width == 0) throw new InvalidOperationException();

                    creator(value, idxX, idxY, width, height, context);
                }
            }
        }
    }
}
