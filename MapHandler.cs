using System;
using System.Collections.Generic;
using System.Linq;

namespace TFIServer
{
    class MapHandler
    {
        public const int mapVersion = 1;
        public const int layers = 1;
        public const int rows = 100;
        public const int column_count = 100;

        private enum LoadState { Header, Rows, Done }
        private int [,] map = new int[rows, column_count];

        public void LoadMap(string path_map)
        {
            LoadState state = LoadState.Header;

            using (var reader = new System.IO.StreamReader(path_map))
            {
                int row = 0;

                while (!reader.EndOfStream)
                {
                    switch (state)
                    {
                        case LoadState.Header:
                            var header = reader.ReadLine().Split(' ');
                            if (header[0] != "TFIMAP")
                            {
                                throw new Exception("Invalid map header");
                            }
                            state = LoadState.Rows;
                            break;

                        case LoadState.Rows:
                            var row_str = reader.ReadLine().Split(',');
                            int column = 0;
                            foreach (var str in row_str)
                            {
                                map[row, column] = Int16.Parse(str);
                                column++;
                            }
                            row++;

                            if (row == rows)
                            {
                                state = LoadState.Done;
                            }
                            break;

                        case LoadState.Done:
                            var footer = reader.ReadLine();
                            if (footer != "END")
                            {
                                throw new Exception("Invalid map footer");
                            }
                            return;
                    }
                }  // while
            }  // using
        }

        // So C# cannot return a reference to a row of a square array. Rather
        // than abandoning square arrays we can use a simple generator. Maybe
        // it is not as inneficient as it looks. 
        public IEnumerable<short> GetRowIter(int row)
        {
            for (var ix = 0; ix < column_count; ix++)
            {
                yield return (short)map[row, ix];
            }
        }

        public bool SendMap(int _toClient)
        {
            for (int iy = 0; iy < rows; iy++)
            {
                int real_row = column_count - iy - 1;
                ServerSend.MapLayerRow(
                    _toClient, 0, real_row, column_count, GetRowIter(iy));
            }

            return true;
        }

    }
}
