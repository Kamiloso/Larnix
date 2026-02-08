using Larnix.Client.Entities;
using Larnix.Entities;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Larnix.Core.Vectors;

namespace Larnix.Client
{
    public class CameraControl : MonoBehaviour
    {
        [SerializeField] Camera MainCamera;
        [SerializeField] Transform FollowTransform;

        private Debugger Debugger => Ref.Debugger;

        private int ZoomSteps = DefaultZoomSteps;

        private const int DefaultZoomSteps = 0;
        private const int ZoomMinSteps = -5;
        private const int ZoomMaxSteps = 5;

        private const float ZoomBase = 8.0f;
        private const float ZoomStep = 0.5f;
        private const float ZoomBase_SPECT = 18.0f;
        private const float ZoomStep_SPECT = 1.5f;

        private void LateUpdate()
        {
            // Ctrl + Scroll reaction
            if (Input.GetKey(KeyCode.LeftControl))
            {
                float scroll = Input.GetAxis("Mouse ScrollWheel");

                if (scroll > 0f && ZoomSteps > ZoomMinSteps)
                    ZoomSteps--;

                if (scroll < 0f && ZoomSteps < ZoomMaxSteps)
                    ZoomSteps++;
            }

            // Camera position
            Vec2 offset = EntityFactory.GetSlaveInstance<IHasCollider>(EntityID.Player).COLLIDER_OFFSET();
            MainCamera.transform.position = new Vector3(
                FollowTransform.position.x + (float)offset.x,
                FollowTransform.position.y + (float)offset.y,
                MainCamera.transform.position.z
            );

            // Camera zoom
            float zoomValue = !Debugger.SpectatorMode ?
                (ZoomBase + ZoomSteps * ZoomStep) :
                (ZoomBase_SPECT + ZoomSteps * ZoomStep_SPECT);
            MainCamera.orthographicSize = zoomValue;
        }
    }
}
