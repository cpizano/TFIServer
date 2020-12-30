using System;
using System.Collections.Generic;
using System.Text;

namespace TFIServer
{
    public static class Array2dExt
    {
        public static IEnumerable<IEnumerable<T>> Rows<T>(this T[,] array)
        {
            for (int r = array.GetLowerBound(0); r <= array.GetUpperBound(0); ++r)
                yield return row(array, r);
        }

        static IEnumerable<T> row<T>(T[,] array, int r)
        {
            for (int c = array.GetLowerBound(1); c <= array.GetUpperBound(1); ++c)
                yield return array[r, c];
        }
    }
    class RLECodec
    {
        // tile  0 0 0 0 9 9 1 5 0 0 0
        // rle   4 x x x 2 y 1 1 3 x x 
        // count 1 2 3 4 1 2 1 1 1 2 3
        public static void EncodeInPlace(MapCell[,,] map)
        {
           for (int layer = 0; layer != map.GetLength(0); layer++)
           {
                for (int row = 0; row != map.GetLength(1); row++)
                {
                    short last = -1;
                    short count = 0;
                    int ix = 0;
                    for (int column = 0; column != map.GetLength(2); column++)
                    {
                        var ct = map[layer, row, column].tile;
                        if (ct == last)
                        {
                            count++;
                        }
                        else
                        {
                            map[layer, row, ix].rle = count;
                            last = ct;
                            ix = column;
                            count = 1;
                        }
                    }

                    map[layer, row, ix].rle = count;
                }
            }
        }

    }
}
