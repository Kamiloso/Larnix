using System.Collections.Generic;
using Larnix.Core.Vectors;
using Unity.VisualScripting;
using UnityEngine;

namespace Larnix.Client.Relativity
{
    public static class RelativityExtensions
    {
        private static MainPlayer MainPlayer => Ref.MainPlayer;

        public static Transform SetLarnixPos(this Transform transform, Vec2 targetPos)
        {
            transform.position = MainPlayer.ToUnityPos(targetPos);
            return transform;
        }

        public static GameObject SetLarnixPos(this GameObject gameObject, Vec2 targetPos) =>
            gameObject.transform.SetLarnixPos(targetPos).gameObject;

        public static Transform Relativise(this Transform transform, Vec2 targetPos)
        {
            Relativiser relativiser = transform.GetComponent<Relativiser>();
            if(relativiser == null)
                relativiser = transform.gameObject.AddComponent<Relativiser>();

            relativiser.Position = targetPos;
            return transform;
        }

        public static GameObject Relativise(this GameObject gameObject, Vec2 targetPos) =>
            gameObject.transform.Relativise(targetPos).gameObject;
    }
}
