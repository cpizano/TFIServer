using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace TFIServer
{
    // The TFI server has the following main components
    //  #### Networking centric, multithreaded
    //  - Client
    //      Contains the tcp and upd enpoints
    //  - Server
    //      Mantains Dictionary<int, Client>
    //
    //  #### Gameplay centric, singlethreaded
    //  - GameLogic
    //        Mantains Dictionary<int, Player>
    //  - Player
    //
    // The networking side talks to the game side via
    // some sort of messageloop in ThreadManager
    // and the game side directly talks to  the server
    // which uses a reader-writer lock to keep things
    // consistent.

    class Program
    {
        private static bool is_running = false;
        private static readonly int udp_port = 26951;

        private static Dictionary<string, (Action<GameLogic>, string)> actions;

        static void Main(string[] _)
        {
            Console.BackgroundColor = ConsoleColor.Blue;
            Console.ForegroundColor = ConsoleColor.White;
            Console.Title = "TFI Game server";
            Console.WriteLine($"Server v20.12.26a started at {DateTime.Now} ");
            Console.WriteLine($"+ Address {GetLocalIPAddress()} : {udp_port} ");
            Console.WriteLine($"+ Directory: {System.IO.Directory.GetCurrentDirectory()}");
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine("Commands available. Type ? for help ");
            Console.ResetColor();

            is_running = true;

            (int status, string map) = ProcessConfig();
            if (status != 0)
            {
                Console.WriteLine($"config error {status}. Terminating.\n");
                return;
            }

            Thread simThread = new Thread(SimThread);
            simThread.Start(map);

            Server.Start(20, udp_port);

            InitActions();

            for(; ; )
            {
                var line = Console.ReadLine();
                if (actions.TryGetValue(line, out (Action<GameLogic> action, string name) it))
                {
                    ThreadManager.ExecuteOnMainThread(it.action);
                }
            }
        }

        // Process the config file that is found in the "bin" directory.
        // the return is the path to the map and 0 for success any other
        // status it means error.
        private static (int status, string map) ProcessConfig()
        {
            // The config file is expected to be:
            // CONFIG v1
            // MAP <path to a map (json format).
            // END

            List<string[]> lines = new List<string[]>();
            using (var reader = new System.IO.StreamReader("..\\..\\config.txt"))
            {
                while (!reader.EndOfStream)
                {
                    lines.Add(reader.ReadLine().Split(' '));
                }
            }
            if (lines.Count < 3)
            {
                return (1, "");
            }
            if (lines[0].Length != 2 ||  lines[0][0] != "CONFIG" || lines[0][1] != "v1")
            {
                return (2, "");
            }
            if (lines[1].Length != 2 || lines[1][0] != "MAP")
            {
                return (3, "");
            }

            return (0, lines[1][1]);
        }

        // 

        private static void SimThread(object map)
        {
            GameLogic game = new GameLogic(map as string);
            DateTime next_loop = DateTime.Now;
            var _ticks_start = next_loop.Ticks; 

            while (is_running)
            {
                var now = DateTime.Now;
                while (next_loop < now)
                {
                    game.UpdateFixed(now.Ticks - _ticks_start);

                    next_loop = next_loop.AddMilliseconds(Constants.MS_PER_TICK);

                    if (next_loop > DateTime.Now)
                    {
                        Thread.Sleep(next_loop - DateTime.Now);
                    } 
                    else
                    {
                        Console.WriteLine("falling behind");
                    }
                }
            }
        }

        private static string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (IPAddress.IsLoopback(ip)) continue;
                if (ip.AddressFamily != AddressFamily.InterNetwork) continue;

                return ip.ToString();
            }
            throw new Exception("No IPV4 network adapters!");
        }

        // The actions here run on the Gamelogic thread.
        private static void PrintHelp(GameLogic _)
        {
            foreach(var action in actions)
            {
                Console.WriteLine($"{action.Key} : {action.Value.Item2}");
            }
        }

        private static void DumpPlayers(GameLogic game)
        {
            game.DumpPlayers();
        }

        private static void HeartBeat(GameLogic game)
        {
            game.ToggleHeartbeatPrint();
        }

        private static void InitActions()
        {
            actions = new Dictionary<string, (Action<GameLogic>, string)>()
            {
                { "?", (PrintHelp, nameof(PrintHelp)) },
                { "d", (DumpPlayers, nameof(DumpPlayers)) },
                { "h", (HeartBeat, nameof(HeartBeat)) }
            };
        }
    }
}
