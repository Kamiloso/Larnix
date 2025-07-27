using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Larnix.Server.Terrain
{
    public class BlockDataManager : MonoBehaviour
    {
        private void Awake()
        {
            References.BlockDataManager = this;
        }
    }
}
