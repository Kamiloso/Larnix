using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Larnix.Client.Entities.Body
{
    public class HeadRotor : MonoBehaviour
    {
        [SerializeField] List<Transform> Head;

        public void HeadRotate(float angle)
        {
            while (angle < -90f) angle += 360f;
            while (angle >= 270f) angle -= 360f;

            transform.localScale = new Vector3(angle <= 90f ? 1f : -1f, 1f, 1f);

            foreach (var head in Head)
            {
                head.rotation = Quaternion.Euler(0f, 0f, angle <= 90f ? angle : 180f + angle);
            }
        }
    }
}
