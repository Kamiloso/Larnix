using System;
using System.Collections.Generic;
using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;
using static UnityEngine.PlayerLoop.Update;

namespace Larnix.Patches
{
    // This class was written by ChatGPT - Thank you, dear chatbot!

    /// <summary>
    /// Static injector for running actions early in the PlayerLoop, before all MonoBehaviour.Update calls.
    /// </summary>
    public static class EarlyUpdateInjector
    {
        // Registered actions
        private static readonly List<Action> _earlyUpdateActions = new();
        private static readonly Dictionary<Action, int> _actionOrders = new();
        private static bool _initialized = false;

        /// <summary>
        /// Injects an Action to be called early before all Update() methods.
        /// </summary>
        public static void InjectEarlyUpdate(Action action, int order)
        {
            if (action == null)
                return;
            
            Initialize();

            if (!_earlyUpdateActions.Contains(action))
            {
                _earlyUpdateActions.Add(action);
                _actionOrders[action] = order;
            }

            // Sort actions by order
            _earlyUpdateActions.Sort((a, b) => _actionOrders[a].CompareTo(_actionOrders[b]));
        }

        /// <summary>
        /// Uninjects an Action from being called early before all Update() methods.
        /// </summary>
        public static void UninjectEarlyUpdate(Action action)
        {
            if (action == null)
                return;

            _earlyUpdateActions.Remove(action);
            _actionOrders.Remove(action);
        }

        /// <summary>
        /// Ensures the custom player loop system is inserted.
        /// </summary>
        private static void Initialize()
        {
            if (_initialized)
                return;

            var playerLoop = PlayerLoop.GetCurrentPlayerLoop();

            for (int i = 0; i < playerLoop.subSystemList.Length; i++)
            {
                if (playerLoop.subSystemList[i].type == typeof(Update))
                {
                    var updateSystem = playerLoop.subSystemList[i];
                    var subs = new List<PlayerLoopSystem>(updateSystem.subSystemList ?? new PlayerLoopSystem[0]);

                    int insertIndex = subs.FindIndex(s => s.type == typeof(ScriptRunBehaviourUpdate));
                    if (insertIndex < 0)
                        insertIndex = 0;

                    subs.Insert(insertIndex, CreateEarlyUpdateSystem());

                    updateSystem.subSystemList = subs.ToArray();
                    playerLoop.subSystemList[i] = updateSystem;
                    break;
                }
            }

            // Apply modified loop
            PlayerLoop.SetPlayerLoop(playerLoop);
            _initialized = true;
        }

        /// <summary>
        /// Creates the PlayerLoopSystem for early update execution.
        /// </summary>
        private static PlayerLoopSystem CreateEarlyUpdateSystem()
        {
            return new PlayerLoopSystem
            {
                type = typeof(EarlyUpdateInjector),
                updateDelegate = ExecuteEarlyUpdates
            };
        }

        /// <summary>
        /// Executes all registered early update actions.
        /// </summary>
        private static void ExecuteEarlyUpdates()
        {
            for (int i = 0; i < _earlyUpdateActions.Count; i++)
            {
                _earlyUpdateActions[i]?.Invoke();
            }
        }
    }
}
