using System.Collections.Generic;
using Larnix.Core.Vectors;
using Larnix.Client.Relativity;
using UnityEngine;
using Larnix.Core;

namespace Larnix.Scoping
{
    public static class MyInput
    {
        private static Camera Camera => GlobRef.Get<Camera>();

        private static bool ScopeMatches(ScopeID scope)
        {
            return !Scopes.DeafFrame && Scopes.Matches(scope);
        }

#region Robust Game Input

        // ------ MOVEMENT KEYS ------

        public static bool PressUp => PressedUp();
        public static bool PressedUp(ScopeID scope = ScopeID.Default)
        {
            return ScopeMatches(scope) && (
                Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W)
            );
        }

        public static bool PressLeft => PressedLeft();
        public static bool PressedLeft(ScopeID scope = ScopeID.Default)
        {
            return ScopeMatches(scope) && (
                Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A)
            );
        }

        public static bool PressDown => PressedDown();
        public static bool PressedDown(ScopeID scope = ScopeID.Default)
        {
            return ScopeMatches(scope) && (
                Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S)
            );
        }

        public static bool PressRight => PressedRight();
        public static bool PressedRight(ScopeID scope = ScopeID.Default)
        {
            return ScopeMatches(scope) && (
                Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D)
            );
        }

        // ----- ACTION KEYS ------

        public static bool PressJump => PressedJump();
        public static bool PressedJump(ScopeID scope = ScopeID.Default)
        {
            return ScopeMatches(scope) && (
                PressedUp(scope) || Input.GetKey(KeyCode.Space)
            );
        }

        public static bool PressCrouch => PressedCrouch();
        public static bool PressedCrouch(ScopeID scope = ScopeID.Default)
        {
            return ScopeMatches(scope) && (
                /*PressedDown(scope) ||*/ Input.GetKey(KeyCode.LeftShift)
            );
        }

        // ------ MOUSE BUTTONS ------

        public static bool PressClickLeft => PressedClickLeft();
        public static bool PressedClickLeft(ScopeID scope = ScopeID.Default)
        {
            return ScopeMatches(scope) && (
                Input.GetMouseButton(0)
            );
        }

        public static bool PressClickRight => PressedClickRight();
        public static bool PressedClickRight(ScopeID scope = ScopeID.Default)
        {
            return ScopeMatches(scope) && (
                Input.GetMouseButton(1)
            );
        }

        public static bool PressClickMiddle => PressedClickMiddle();
        public static bool PressedClickMiddle(ScopeID scope = ScopeID.Default)
        {
            return ScopeMatches(scope) && (
                Input.GetMouseButton(2)
            );
        }

        // ------ SCROLL WHEEL ------

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

        // ------ OTHER ------

        public static Vec2? MouseTargetPos(ScopeID scope = ScopeID.Default)
        {
            if (!ScopeMatches(scope)) return null;

            Vector2 mousePos = Input.mousePosition;
            Vector2 mousePosWorld = Camera.ScreenToWorldPoint(mousePos);

            Vec2 cursorPos = ((Vector2)mousePosWorld).ToLarnixPos();
            return cursorPos;
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
