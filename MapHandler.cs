using System;
using System.Collections.Generic;
using System.Linq;

namespace TFIServer
{
    class MapHandler
    {
        public readonly int mapVersion = 2;

        public int Layers { get => map.GetLength(0); }
        public int Row_count { get => map.GetLength(1); }
        public int Column_count { get => map.GetLength(2); }

        // The map is [layer][rows][columns]. The |columns| is the count
        // of elements in the x coordinate, in other words the size of each row.
        private int[,,] map;

        // A map is a directory with a manifest (man.txt) and as many
        // csv files (L0.csv, L1.csv, ...) as layers. The manifest defines
        // the number of layers, the expected size of each layer and much
        // more.
        public void LoadMap(string path_map)
        {
            var manifest = System.IO.Path.Combine(path_map, "man.txt");
            (int layers, int row_count, int column_count, int tile_count) =
                ProcessManifest(manifest);

            map = new int[layers, row_count, column_count];

            for (int layer = 0; layer != layers; layer++)
            {
                var file = System.IO.Path.Combine(path_map, $"L{layer}.csv");
                using (var reader = new System.IO.StreamReader(file))
                {
                    int row = 0;

                    while (!reader.EndOfStream)
                    {

                        var row_str = reader.ReadLine().Split(',');
                        if (row_str.Length != map.GetLength(2))
                        {
                            throw new Exception("Map invalid column");
                        }

                        int column = 0;
                        foreach (var str in row_str)
                        {
                            var tile_id = Int16.Parse(str);
                            if (tile_id >= tile_count)
                            {
                                throw new Exception("Map invalid tile id");
                            }
                            map[layer, row, column] = tile_id;
                            column++;
                        }

                        if (column != column_count)
                        {
                            throw new Exception("Map csv column too short");
                        }

                        row++;

                        if (row > map.GetLength(1))
                        {
                            throw new Exception("Map csv too many rows");
                        }

                    }  // while

                    if (row != row_count)
                    {
                        throw new Exception("Map csv too few rows");
                    }
                }  // using
            }

            System.Console.WriteLine($"loaded map [{path_map}] {layers}x{row_count}x{column_count}");
        }

        // So C# cannot return a reference to a row of a square array. Rather
        // than abandoning square arrays we can use a simple generator. Maybe
        // it is not as inneficient as it looks. 
        private IEnumerable<short> GetRowIter(int layer, int row)
        {
            for (var ix = 0; ix < Column_count; ix++)
            {
                yield return (short)map[layer, row, ix];
            }
        }

        public bool SendMap(int _toClient)
        {
            for (int layer = 0; layer < Layers; layer++)
            {
                for (int iy = 0; iy < Row_count; iy++)
                {
                    var real_row = Row_count - iy - 1;
                    ServerSend.MapLayerRow(
                        _toClient, layer, real_row, Column_count, GetRowIter(layer, iy));
                }
            }

            return true;
        }

        internal (int layers, int rows, int columns, int tilecount) ProcessManifest(string _path)
        {
            List<string[]> lines = new List<string[]>();
            using (var reader = new System.IO.StreamReader(_path))
            {
                while (!reader.EndOfStream)
                {
                    lines.Add(reader.ReadLine().Split(' '));
                }
            }

            // We expect something like this in the manifest:
            // TFIMAP v01
            // TILES 32 x 32 x 1240 gmap.png
            // SIZE 100 x 100 x 3 csv
            //
            // We ignore the rest of the file so it can contain
            // comments after that.

            if (lines.Count < 3)
            {
                throw new Exception("Map Invalid manifest size");
            }

            if (lines[0].Length != 2 && lines[0][1] != "TFIMAP" &&
                lines[0][2] != "v02") // keep in sync with |mapVersion|.
            {
                throw new Exception("Map Invalid manifest version");
            }

            if (lines[1].Length != 7 && lines[1][0] != "TILES" &&
                lines[1][1] != "32" && lines[1][2] != "x" && lines[1][3] != "32")
            {
                throw new Exception("Map Invalid tiles");
            }

            var tilecount = int.Parse(lines[1][5]);

            if (lines[2].Length != 7 && lines[2][0] != "SIZE"
                && lines[2][2] != "x" && lines[2][4] != "x" && lines[2][6] != "csv")
            {
                throw new Exception("Map Invalid size");
            }

            var rows = int.Parse(lines[2][1]);
            var cols = int.Parse(lines[2][3]);
            var layers = int.Parse(lines[2][5]);

            if (rows > 500 || rows < 5 || cols > 500 || cols < 5 || layers < 1 || layers > 5 )
            {
                throw new Exception("Map Unsuported size");
            } 

            return (layers, rows, cols, tilecount);
        }

    }
}
