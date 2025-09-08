using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Larnix.Client.Entities.Body
{
    public class LimbAnimator : MonoBehaviour
    {
        [SerializeField] List<Transform> LimbFront;
        [SerializeField] List<Transform> LimbBack;
        [SerializeField] List<Transform> ArmMain;

        public void DoUpdate()
        {

        }
    }
}
