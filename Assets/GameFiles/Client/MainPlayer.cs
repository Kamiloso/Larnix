using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Larnix.Network;
using Larnix.Client.Entities;
using Larnix.Entities;
using System;
using Larnix.Physics;

namespace Larnix.Client
{
    public class MainPlayer : MonoBehaviour
    {
        [SerializeField] Camera Camera;
        [SerializeField] EntityProjection EntityProjection;
        [SerializeField] Transform RaycastCenter; // simply player's head
        [SerializeField] DynamicCollider DynamicCollider;
        [SerializeField] Vector2 CameraDeltaPosition;
        
        private uint FixedCounter = 0;
        private uint LastSentFixedCounter = 0;

        private float Rotation;

        private const float StepSize = 0.1f;

        private const float CameraDefaultZoom = 8.0f;
        private const float CameraZoomMin = 5.5f;
        private const float CameraZoomMax = 10.5f;
        private const float CameraZoomStep = 0.5f;

        private float CameraZoom = CameraDefaultZoom;

        private void Awake()
        {
            References.MainPlayer = this;
            transform.parent.gameObject.SetActive(false);
        }

        private void FixedUpdate()
        {
            FixedCounter++;

            if (!References.Debug.SpectatorMode)
            {
                DynamicCollider.PhysicsUpdate(new InputData
                {
                    Left = Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A),
                    Right = Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D),
                    Jump = Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W),
                });
            }
            else
            {
                const float WILL_SIZE = 45f;
                const float STRONG_WILL_SIZE = 90f;

                bool left = Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A);
                bool right = Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D);
                bool up = Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W);
                bool down = Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S);

                bool turbo = Input.GetKey(KeyCode.Space);

                Vector2 will = (turbo ? STRONG_WILL_SIZE : WILL_SIZE) * Time.fixedDeltaTime * (
                    (right ? 1 : 0) * Vector2.right +
                    (left ? 1 : 0) * Vector2.left +
                    (up ? 1 : 0) * Vector2.up +
                    (down ? 1 : 0) * Vector2.down
                );

                DynamicCollider.NoPhysicsUpdate((Vector2)transform.position + will);
            }

            RotationUpdate();
            UpdateEntityObject(FixedCounter * Time.fixedDeltaTime);
        }

        private void Update()
        {
            // Send update to server

            if(LastSentFixedCounter != FixedCounter)
            {
                PlayerUpdate playerUpdate = new PlayerUpdate(
                    transform.position,
                    Rotation,
                    FixedCounter
                    );
                if (!playerUpdate.HasProblems)
                {
                    References.Client.Send(playerUpdate.GetPacket(), false); // fast mode (over raw udp)
                }

                LastSentFixedCounter = FixedCounter;
            }

            // Ctrl + Scroll reaction

            if (Input.GetKey(KeyCode.LeftControl))
            {
                float scroll = Input.GetAxis("Mouse ScrollWheel");

                if (scroll > 0f && CameraZoom > CameraZoomMin)
                    CameraZoom -= CameraZoomStep;

                if (scroll < 0f && CameraZoom < CameraZoomMax)
                    CameraZoom += CameraZoomStep;
            }
        }

        private void LateUpdate()
        {
            // Camera update
            
            Vector2 plr = EntityProjection.transform.position;
            Camera.transform.position = new Vector3(plr.x, plr.y, Camera.transform.position.z) + (Vector3)CameraDeltaPosition;
            Camera.orthographicSize = CameraZoom + (References.Debug.SpectatorMode ? 10f : 0f);
        }

        private void RotationUpdate()
        {
            Vector2 mouse_pos = Input.mousePosition;
            Vector2 head_pos = Camera.WorldToScreenPoint(RaycastCenter.position);

            Vector2 raycast_vect = mouse_pos - head_pos;

            Rotation = Mathf.Atan2(raycast_vect.y, raycast_vect.x) * 180f / Mathf.PI;
        }

        private void UpdateEntityObject(double time)
        {
            EntityProjection.UpdateTransform(new EntityData
            {
                ID = EntityID.Player,
                Position = transform.position,
                Rotation = Rotation,
                NBT = null
            }, time);
        }

        public Vector2 GetPosition()
        {
            return transform.position;
        }

        public void LoadPlayerData(PlayerInitialize msg)
        {
            transform.position = msg.Position;
        }

        public void SetAlive()
        {
            if (IsAlive)
                throw new System.InvalidOperationException("Player is already alive!");

            transform.parent.gameObject.SetActive(true);
            References.TileSelector.Enable();

            DynamicCollider.Enable();

            RotationUpdate();
            UpdateEntityObject(0f);
        }

        public void SetDead()
        {
            if (!IsAlive)
                throw new System.InvalidOperationException("Player is already dead!");

            transform.parent.gameObject.SetActive(false);
            References.TileSelector.Disable();

            DynamicCollider.Disable();

            EntityProjection.ResetSmoother();
        }

        public bool IsAlive
        {
            get => transform.parent.gameObject.activeSelf;
        }
    }
}
