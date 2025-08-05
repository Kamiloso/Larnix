using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Larnix.Socket.Commands;
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
        [SerializeField] TextMeshProUGUI CoordinatesText;
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

            DynamicCollider.PhysicsUpdate(new InputData
            {
                Left = Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A),
                Right = Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D),
                Jump = Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W),
            });

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

        private float? fps = null;
        private void LateUpdate()
        {
            // Coordinates text update (temporary)

            if(fps == null || FixedCounter % 15 == 0)
                fps = (float)(Math.Round(1f / Time.deltaTime * 10f) / 10f);

            CoordinatesText.text = $"FPS: {fps}\nX: {transform.position.x}\nY: {transform.position.y}";

            // Camera update
            
            Vector2 plr = EntityProjection.transform.position;
            Camera.transform.position = new Vector3(plr.x, plr.y, Camera.transform.position.z) + (Vector3)CameraDeltaPosition;
            Camera.orthographicSize = CameraZoom;
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
            if (transform.parent.gameObject.activeSelf)
                throw new System.InvalidOperationException("Player is already alive!");

            transform.parent.gameObject.SetActive(true);
            CoordinatesText.gameObject.SetActive(true);
            References.TileSelector.Enable();

            DynamicCollider.Enable();

            RotationUpdate();
            UpdateEntityObject(0f);
        }

        public void SetDead()
        {
            if (!transform.parent.gameObject.activeSelf)
                throw new System.InvalidOperationException("Player is already dead!");

            transform.parent.gameObject.SetActive(false);
            CoordinatesText.gameObject.SetActive(false);
            References.TileSelector.Disable();

            DynamicCollider.Disable();

            EntityProjection.ResetSmoother();
        }
    }
}
