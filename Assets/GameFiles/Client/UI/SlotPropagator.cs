using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Larnix.Client.UI
{
    public class SlotPropagator : MonoBehaviour
    {
        [SerializeField] int SlotAmount;
        [SerializeField] Vector2 DeltaPropagate;

        private bool isRoot = true;
        private int slotID = 0;

        private void Start()
        {
            if(isRoot)
            {
                Vector2 deltaPos = -(SlotAmount - 1) * DeltaPropagate / 2;

                for (int i = 1; i < SlotAmount; i++)
                {
                    Transform trn = Instantiate(gameObject, transform.position, transform.rotation).transform;
                    trn.SetParent(transform.parent, false);
                    trn.position = transform.position;

                    SlotPropagator spp = trn.GetComponent<SlotPropagator>();
                    spp.isRoot = false;
                    spp.Initialize(i, deltaPos);
                }

                Initialize(0, deltaPos);
            }
        }

        private void Initialize(int slotID, Vector2 deltaPos)
        {
            this.slotID = slotID;
            transform.localPosition += (Vector3)(slotID * DeltaPropagate + deltaPos);
            transform.name = $"Slot {slotID}";
            GetComponent<Slot>().Init(slotID);
            Destroy(this);
        }
    }
}
