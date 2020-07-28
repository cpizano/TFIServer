using System;

namespace TFIServer
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Title = "TFI Game server";
            Server.Start(20, 26951);
            Console.ReadKey();
        }
    }
}
