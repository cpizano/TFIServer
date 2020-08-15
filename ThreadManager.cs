using System;
using System.Collections.Generic;

namespace TFIServer
{
    class ThreadManager
    {
        private static readonly List<Action<GameLogic>> executeOnMainThread = new List<Action<GameLogic>>();
        private static readonly List<Action<GameLogic>> executeCopiedOnMainThread = new List<Action<GameLogic>>();
        private static bool actionToExecuteOnMainThread = false;

        /// <summary>Sets an action to be executed on the main thread.</summary>
        /// <param name="_action">The action to be executed on the main thread.</param>
        public static void ExecuteOnMainThread(Action<GameLogic> _action)
        {
            if (_action == null)
            {
                throw new System.InvalidOperationException("action cannot be null");
            }

            lock (executeOnMainThread)
            {
                executeOnMainThread.Add(_action);
                actionToExecuteOnMainThread = true;
            }
        }

        /// <summary>Executes all code meant to run on the main thread. NOTE: Call this ONLY from the main thread.</summary>
        public static void UpdateFromNetwork(GameLogic game)
        {
            // Is this pattern correct? we are reading is boolean outside the lock.
            if (actionToExecuteOnMainThread)
            {
                executeCopiedOnMainThread.Clear();
                lock (executeOnMainThread)
                {
                    executeCopiedOnMainThread.AddRange(executeOnMainThread);
                    executeOnMainThread.Clear();
                    actionToExecuteOnMainThread = false;
                }

                for (int i = 0; i < executeCopiedOnMainThread.Count; i++)
                {
                    executeCopiedOnMainThread[i](game);
                }
            }
        }
    }
}
