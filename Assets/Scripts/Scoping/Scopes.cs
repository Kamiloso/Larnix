using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using Larnix.Patches;

namespace Larnix.Scoping
{
    [Flags]
    public enum ScopeID
    {
        None = 0,
        All = ~0,

        Default = 1 << 0,
            Inventory = 1 << 1,
            Chat = 1 << 2,
            Pause = 1 << 3,
    }

    public class Scopes : MonoBehaviour, IGlobalUnitySingleton
    {
        private void Awake()
        {
            EarlyUpdateInjector.InjectEarlyUpdate(PreEarlyUpdate, order: -100);
        }

        private void PreEarlyUpdate()
        {
            DeafFrame = false;
        }

        private void OnDestroy()
        {
            EarlyUpdateInjector.UninjectEarlyUpdate(PreEarlyUpdate);
        }

#region Static Methods

        public static bool DeafFrame { get; private set; } = false;
        
        private static ScopeID _currentScope = ScopeID.Default;
        public static ScopeID CurrentScope
        {
            get => _currentScope;
            private set
            {
                _currentScope = value;
                DeafFrame = true;
            }
        }

        private static readonly Dictionary<ScopeID, ScopeID> _returnTable = new()
        {
            [ScopeID.Default] = ScopeID.Default,
            [ScopeID.Inventory] = ScopeID.Default,
            [ScopeID.Chat] = ScopeID.Default,
            [ScopeID.Pause] = ScopeID.Default,
        };

        public static bool Matches(ScopeID scope)
        {
            return (CurrentScope & scope) != 0;
        }

        public static bool EnterScopeWhen(ScopeID newScope, Func<bool> predicate)
        {
            if (MayEnter(newScope) && predicate())
            {
                Enter(newScope);
                return true;
            }
            return false;
        }

        public static bool LeaveScopeWhen(ScopeID oldScope, Func<bool> predicate)
        {
            if (MayLeave(oldScope) && predicate())
            {
                Leave(oldScope);
                return true;
            }
            return false;
        }

        public static void Reset()
        {
            CurrentScope = ScopeID.Default;
        }

#endregion
#region Private Methods

        private static bool MayEnter(ScopeID newScope)
        {
            return _returnTable.TryGetValue(newScope, out var backScope)
                && backScope == CurrentScope;
        }

        private static bool MayLeave(ScopeID fromScope)
        {
            return CurrentScope == fromScope;
        }

        private static void Enter(ScopeID newScope)
        {
            if (!MayEnter(newScope))
                throw new ArgumentException("Cannot enter scope ID: " + newScope);
            
            CurrentScope = newScope;
        }
        
        private static void Leave(ScopeID oldScope)
        {
            if (!MayLeave(oldScope))
                throw new ArgumentException("Cannot leave scope ID: " + oldScope);

            CurrentScope = _returnTable[CurrentScope];
        }

#endregion

    }
}
