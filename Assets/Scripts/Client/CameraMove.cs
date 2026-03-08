using Larnix.Entities;
using Larnix.Scoping;
using UnityEngine;
using Larnix.Core.Vectors;
using Larnix.Core;
using IHasCollider = Larnix.Entities.All.IHasCollider;

namespace Larnix.Client
{
    public class CameraMove : MonoBehaviour
    {
        [SerializeField] Camera MainCamera;
        [SerializeField] Transform FollowTransform;

        private Debugger Debugger => GlobRef.Get<Debugger>();

        private int ZoomSteps = DEFAULT_STEPS;

        private const int DEFAULT_STEPS = 0;
        private const int MIN_STEPS = -5;
        private const int MAX_STEPS = 5;

        private const float ZOOM_BASE = 8.0f;
        private const float STEP_SIZE = 0.5f;

        private const float ZOOM_BASE_SPECT = 18.0f;
        private const float STEP_SIZE_SPECT = 1.5f;

        private void LateUpdate()
        {
            // Ctrl + Scroll reaction
            float scroll = MyInput.GetScrollCtrl();
            if (scroll > 0f && ZoomSteps > MIN_STEPS) ZoomSteps--;
            if (scroll < 0f && ZoomSteps < MAX_STEPS) ZoomSteps++;

            // Camera position
            Vec2 offset = EntityFactory.GetSlaveInstance<IHasCollider>(EntityID.Player).COLLIDER_OFFSET();
            MainCamera.transform.position = new Vector3(
                FollowTransform.position.x + (float)offset.x,
                FollowTransform.position.y + (float)offset.y,
                MainCamera.transform.position.z
            );

            // Camera zoom
            float zoomValue = !Debugger.SpectatorMode ?
                (ZOOM_BASE + ZoomSteps * STEP_SIZE) :
                (ZOOM_BASE_SPECT + ZoomSteps * STEP_SIZE_SPECT);
            MainCamera.orthographicSize = zoomValue;
        }
    }
}
