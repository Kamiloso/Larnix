using Larnix.Client.Entities;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Larnix.Client
{
    public class CameraControl : MonoBehaviour
    {
        [SerializeField] Camera MainCamera;
        [SerializeField] Transform FollowTransform;
        [SerializeField] Vector2 Offset;

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
            MainCamera.transform.position = new Vector3(
                FollowTransform.position.x + Offset.x,
                FollowTransform.position.y + Offset.y,
                MainCamera.transform.position.z
            );

            // Camera zoom
            float zoomValue = !Ref.Debug.SpectatorMode ?
                (ZoomBase + ZoomSteps * ZoomStep) :
                (ZoomBase_SPECT + ZoomSteps * ZoomStep_SPECT);
            MainCamera.orthographicSize = zoomValue;
        }
    }
}
