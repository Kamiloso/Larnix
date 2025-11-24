using System;
using System.Collections.Generic;
using UnityEngine;
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
        private static readonly List<Action> earlyUpdateActions = new List<Action>();
        private static bool initialized = false;

        /// <summary>
        /// Injects an Action to be called early before all Update() methods.
        /// </summary>
        public static void InjectEarlyUpdate(Action action)
        {
            if (action == null)
                return;
            Initialize();
            if (!earlyUpdateActions.Contains(action))
                earlyUpdateActions.Add(action);
        }

        /// <summary>
        /// Clears all registered early update actions.
        /// </summary>
        public static void ClearEarlyUpdate()
        {
            earlyUpdateActions.Clear();
        }

        /// <summary>
        /// Ensures the custom player loop system is inserted.
        /// </summary>
        private static void Initialize()
        {
            if (initialized)
                return;

            // Get current loop
            var playerLoop = PlayerLoop.GetCurrentPlayerLoop();

            // Insert our system into the Update phase
            for (int i = 0; i < playerLoop.subSystemList.Length; i++)
            {
                if (playerLoop.subSystemList[i].type == typeof(Update))
                {
                    var updateSystem = playerLoop.subSystemList[i];
                    // Prepare new subSystemList with space for our system
                    var subs = new List<PlayerLoopSystem>(updateSystem.subSystemList ?? new PlayerLoopSystem[0]);

                    // Find index of ScriptRunBehaviourUpdate
                    int insertIndex = subs.FindIndex(s => s.type == typeof(ScriptRunBehaviourUpdate));
                    if (insertIndex < 0)
                        insertIndex = 0;

                    // Insert our callback system before ScriptRunBehaviourUpdate
                    subs.Insert(insertIndex, CreateEarlyUpdateSystem());

                    updateSystem.subSystemList = subs.ToArray();
                    playerLoop.subSystemList[i] = updateSystem;
                    break;
                }
            }

            // Apply modified loop
            PlayerLoop.SetPlayerLoop(playerLoop);
            initialized = true;
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
            for (int i = 0; i < earlyUpdateActions.Count; i++)
            {
                earlyUpdateActions[i]?.Invoke();
            }
        }
    }
}
