using System;
using System.Collections.Generic;
using System.Text;

namespace TFIServer
{
    class MapHandler
    {
        public const int mapVersion = 1;
        public const int layers = 1;
        public const int rows = 100;
        public const int columns = 100;

        private enum LoadState { Header, Rows, Done }
        private int [,] map = new int[columns, rows];

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
                                map[column, row] = Int32.Parse(str);
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

    }
}
