using UnityEngine;

namespace Larnix.Scoping
{
    public static class MyInput
    {
        private static bool ScopeMatches(ScopeID scope)
        {
            return !Scopes.DeafFrame && Scopes.Matches(scope);
        }

#region Robust Game Input

        public static bool PressedUp(ScopeID scope = ScopeID.Default)
        {
            return ScopeMatches(scope) && (
                Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W)
            );
        }

        public static bool PressedLeft(ScopeID scope = ScopeID.Default)
        {
            return ScopeMatches(scope) && (
                Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A)
            );
        }

        public static bool PressedDown(ScopeID scope = ScopeID.Default)
        {
            return ScopeMatches(scope) && (
                Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S)
            );
        }

        public static bool PressedRight(ScopeID scope = ScopeID.Default)
        {
            return ScopeMatches(scope) && (
                Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D)
            );
        }

        public static bool PressedJump(ScopeID scope = ScopeID.Default)
        {
            return ScopeMatches(scope) && (
                PressedUp(scope) || Input.GetKey(KeyCode.Space)
            );
        }

        public static bool PressedCrouch(ScopeID scope = ScopeID.Default)
        {
            return ScopeMatches(scope) && (
                Input.GetKey(KeyCode.LeftShift)
            );
        }

        public static float GetScrollNormal(ScopeID scope = ScopeID.Default)
        {
            if (!ScopeMatches(scope)) return 0f;
            if (Input.GetKey(KeyCode.LeftControl)) return 0f;
            return Input.mouseScrollDelta.y;
        }

        public static float GetScrollCtrl(ScopeID scope = ScopeID.Default)
        {
            if (!ScopeMatches(scope)) return 0f;
            if (!Input.GetKey(KeyCode.LeftControl)) return 0f;
            return Input.mouseScrollDelta.y;
        }

#endregion
#region Unity Wrappers

        public static bool GetKey(KeyCode key, ScopeID scope = ScopeID.Default)
        {
            return ScopeMatches(scope) && Input.GetKey(key);
        }

        public static bool GetKeyDown(KeyCode key, ScopeID scope = ScopeID.Default)
        {
            return ScopeMatches(scope) && Input.GetKeyDown(key);
        }

        public static bool GetKeyUp(KeyCode key, ScopeID scope = ScopeID.Default)
        {
            return ScopeMatches(scope) && Input.GetKeyUp(key);
        }

        public static bool GetMouseButton(int button, ScopeID scope = ScopeID.Default)
        {
            return ScopeMatches(scope) && Input.GetMouseButton(button);
        }

        public static bool GetMouseButtonDown(int button, ScopeID scope = ScopeID.Default)
        {
            return ScopeMatches(scope) && Input.GetMouseButtonDown(button);
        }

        public static bool GetMouseButtonUp(int button, ScopeID scope = ScopeID.Default)
        {
            return ScopeMatches(scope) && Input.GetMouseButtonUp(button);
        }

#endregion

    }
}
