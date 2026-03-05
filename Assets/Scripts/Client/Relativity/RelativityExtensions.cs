using System.Collections.Generic;
using Larnix.Core.Vectors;
using Larnix.Core;
using UnityEngine;
using System;

namespace Larnix.Client.Relativity
{
    public static class RelativityExtensions
    {
        private static MainPlayer MainPlayer => GlobRef.Get<MainPlayer>();

        public static Transform SetLarnixPos(this Transform transform, Vec2 targetPos)
        {
            if (transform == null)
                throw new ArgumentNullException(nameof(transform));

            transform.position = MainPlayer.ToUnityPos(targetPos);
            return transform;
        }

        public static GameObject SetLarnixPos(this GameObject gameObject, Vec2 targetPos)
        {
            if (gameObject == null)
                throw new ArgumentNullException(nameof(gameObject));

            return gameObject.transform.SetLarnixPos(targetPos).gameObject;
        }

        public static Transform Relativise(this Transform transform, Vec2 targetPos)
        {
            if (transform == null)
                throw new ArgumentNullException(nameof(transform));

            Relativiser relativiser = transform.GetComponent<Relativiser>();
            if(relativiser == null)
                relativiser = transform.gameObject.AddComponent<Relativiser>();

            relativiser.Position = targetPos;
            return transform;
        }

        public static GameObject Relativise(this GameObject gameObject, Vec2 targetPos)
        {
            if (gameObject == null)
                throw new ArgumentNullException(nameof(gameObject));

            return gameObject.transform.Relativise(targetPos).gameObject;
        }
    }
}
