using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Larnix.Socket.Commands;
using Larnix.Client.Entities;
using Larnix.Entities;
using System;

namespace Larnix.Client
{
    public class MainPlayer : MonoBehaviour
    {
        [SerializeField] Camera Camera;
        [SerializeField] EntityProjection EntityProjection;
        [SerializeField] Transform RaycastCenter; // simply player's head
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

        private const float GRAVITY = 0.5f;
        private const float MAX_VERTICAL_SPEED = 50f;
        private const float GROUND_LEVEL = 0f;
        private const float HORIZONTAL_FORCE = 2f;
        private const float MAX_HORIZONTAL_SPEED = 7f;
        private const float JUMP_SIZE = 15f;
        private const float HORIZONTAL_DRAG = 2f;

        private Vector2 velocity = Vector2.zero;

        private void FixedUpdate()
        {
            // Fixed counter increment

            FixedCounter++;

            // Temporary physics

            const bool PHYSICS_MODE = false;
            if (PHYSICS_MODE)
            {
                int want_horizontal = (Input.GetKey(KeyCode.RightArrow) ? 1 : 0) - (Input.GetKey(KeyCode.LeftArrow) ? 1 : 0);
                if (want_horizontal != 0)
                {
                    velocity += want_horizontal * HORIZONTAL_FORCE * Vector2.right;
                }
                else
                {
                    int sgn1 = Math.Sign(velocity.x);
                    velocity -= new Vector2(sgn1 * HORIZONTAL_DRAG, 0f);
                    int sgn2 = Math.Sign(velocity.x);
                    if (sgn1 != sgn2) velocity = new Vector2(0f, velocity.y);
                }

                velocity += GRAVITY * Vector2.down;

                if (velocity.x > MAX_HORIZONTAL_SPEED) velocity = new Vector2(MAX_HORIZONTAL_SPEED, velocity.y);
                if (velocity.x < -MAX_HORIZONTAL_SPEED) velocity = new Vector2(-MAX_HORIZONTAL_SPEED, velocity.y);
                if (velocity.y > MAX_VERTICAL_SPEED) velocity = new Vector2(velocity.x, MAX_VERTICAL_SPEED);
                if (velocity.y < -MAX_VERTICAL_SPEED) velocity = new Vector2(velocity.x, -MAX_VERTICAL_SPEED);

                transform.position += (Vector3)velocity * Time.fixedDeltaTime;

                bool on_ground = false;
                if (transform.position.y <= GROUND_LEVEL)
                {
                    transform.position = new Vector2(transform.position.x, GROUND_LEVEL);
                    velocity = new Vector2(velocity.x, 0f);
                    on_ground = true;
                }

                if (on_ground && Input.GetKey(KeyCode.UpArrow))
                {
                    velocity += new Vector2(0f, JUMP_SIZE);
                }
            }
            else
            {
                const float SPEED = 10f;

                if (Input.GetKey(KeyCode.W))
                    transform.position += new Vector3(0f, SPEED) * Time.fixedDeltaTime;

                if (Input.GetKey(KeyCode.S))
                    transform.position += new Vector3(0f, -SPEED) * Time.fixedDeltaTime;

                if (Input.GetKey(KeyCode.D))
                    transform.position += new Vector3(SPEED, 0f) * Time.fixedDeltaTime;

                if (Input.GetKey(KeyCode.A))
                    transform.position += new Vector3(-SPEED, 0f) * Time.fixedDeltaTime;
            }

            // Rotation reaction

            RotationUpdate();

            // Update entity object
            
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
            References.TileSelector.Enable();

            RotationUpdate();
            UpdateEntityObject(0f);
        }

        public void SetDead()
        {
            if (!transform.parent.gameObject.activeSelf)
                throw new System.InvalidOperationException("Player is already dead!");

            transform.parent.gameObject.SetActive(false);
            References.TileSelector.Disable();

            EntityProjection.ResetSmoother();
        }
    }
}
