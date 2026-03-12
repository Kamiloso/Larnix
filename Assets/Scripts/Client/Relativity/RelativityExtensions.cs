using Larnix.Core.Vectors;
using Larnix.Core;
using UnityEngine;
using System;

namespace Larnix.Client.Relativity
{
    public static class RelativityExtensions
    {
        private static RelativityManager RelativityManager => GlobRef.Get<RelativityManager>();
        private static IRelativityOrigin Origin => RelativityManager.Origin;

        public static Transform SetLarnixPos(this Transform transform, Vec2 targetPos)
        {
            if (transform == null)
                throw new ArgumentNullException(nameof(transform));

            transform.position = targetPos.ToUnityPos();
            return transform;
        }

        public static Transform Relativise(this Transform transform, Vec2 targetPos)
        {
            if (transform == null)
                throw new ArgumentNullException(nameof(transform));

            Relativiser relativiser = transform.GetComponent<Relativiser>();
            if(relativiser == null)
            {
                relativiser = transform.gameObject.AddComponent<Relativiser>();
            }

            relativiser.Position = targetPos;
            return transform;
        }

        public static Vec2 ToLarnixPos(this Vector2 unityPos)
        {
            return Origin.ToLarnixPos(unityPos);
        }

        public static Vector2 ToUnityPos(this Vec2 larnixPos)
        {
            return Origin.ToUnityPos(larnixPos);
        }
    }
}
