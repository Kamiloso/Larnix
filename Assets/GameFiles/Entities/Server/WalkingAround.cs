using Larnix.Server;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Larnix.Server.Entities;
using Larnix.Physics;
using Larnix.Core;

namespace Larnix.Entities.Server
{
    public class WalkingAround : MonoBehaviour
    {
        private EntityController controller;
        private DynamicCollider dynamicCollider;

        private int walking = 0;
        private int walkingCounter = 0;
        private float headRotation = 0f;

        private const int THINKING_TIME_MIN = 30;
        private const int THINKING_TIME_MAX = 150;

        private void Awake()
        {
            controller = GetComponent<EntityController>();

            foreach(Transform trn in transform)
            {
                if(trn.name == "DynamicCollider")
                {
                    dynamicCollider = trn.GetComponent<DynamicCollider>();
                    break;
                }
            }
        }

        private void Start()
        {
            dynamicCollider.Enable();
        }

        public void DoFixedUpdate()
        {
            PhysicsReport report = dynamicCollider.physicsReport;

            if (walkingCounter == 0)
            {
                walking = walking == 0 ? (Common.Rand().Next() % 2 == 0 ? -1 : 1) : 0;
                if (walking != 0) headRotation = walking > 0 ? 0f : 180f;
                walkingCounter = Common.Rand().Next(THINKING_TIME_MIN, THINKING_TIME_MAX + 1);
            }
            else walkingCounter--;

            dynamicCollider.PhysicsUpdate(new InputData
            {
                Left = walking < 0,
                Right = walking > 0,
                Jump = report.OnLeftWall || report.OnRightWall,
            });

            controller.ApplyTransform();
            controller.UpdateRotation(headRotation);
        }
    }
}
