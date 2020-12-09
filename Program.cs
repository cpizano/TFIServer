﻿using System;
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
        private static bool isRunning = false;
        private static readonly int udpPort = 26951;

        private static Dictionary<string, (Action<GameLogic>, string)> actions;

        static void Main(string[] args)
        {
            Console.BackgroundColor = ConsoleColor.Blue;
            Console.ForegroundColor = ConsoleColor.White;
            Console.Title = "TFI Game server";
            Console.WriteLine($"Server v201129a started at {DateTime.Now} ");
            Console.WriteLine($"+ Address {GetLocalIPAddress()} : {udpPort} ");
            Console.WriteLine($"+ Directory: {System.IO.Directory.GetCurrentDirectory()}");
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine("Commands available. Type ? for help ");
            Console.ResetColor();

            isRunning = true;

            Thread simThread = new Thread(new ThreadStart(SimThread));
            simThread.Start();

            Server.Start(20, udpPort);

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

        private static void SimThread()
        {
            GameLogic game = new GameLogic();
            DateTime _nextLoop = DateTime.Now;
            var _ticks_start = _nextLoop.Ticks; 

            while (isRunning)
            {
                var now = DateTime.Now;
                while (_nextLoop < now)
                {
                    game.UpdateFixed(now.Ticks - _ticks_start);

                    _nextLoop = _nextLoop.AddMilliseconds(Constants.MS_PER_TICK);

                    if (_nextLoop > DateTime.Now)
                    {
                        Thread.Sleep(_nextLoop - DateTime.Now);
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

        private static void DumpPlayers(GameLogic _game)
        {
            _game.DumpPlayers();
        }

        private static void HeartBeat(GameLogic _game)
        {
            _game.ToggleHeartbeatPrint();
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
