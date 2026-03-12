using UnityEngine;
using Larnix.Core;

namespace Larnix.Client.Relativity
{
    public class RelativityManager : MonoBehaviour
    {
        public IRelativityOrigin Origin => GlobRef.Get<MainPlayer>();

        private void Awake()
        {
            GlobRef.Set(this);
        }
    }
}
