using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Larnix.Socket.Commands;
using Larnix.Entities;
using System;

namespace Larnix.Client
{
    public class MainPlayer : MonoBehaviour
    {
        [SerializeField] Camera Camera;
        [SerializeField] EntityProjection EntityProjection;
        private uint FixedCounter = 0;

        private float Rotation;

        private const float StepSize = 0.1f;

        private const float CameraZoomMin = 2.4f;
        private const float CameraZoomMax = 6.0f;
        private const float CameraZoomStep = 0.3f;

        private float CameraZoom = 4.2f; // default zoom

        private void Awake()
        {
            References.MainPlayer = this;
            transform.parent.gameObject.SetActive(false);
        }

        private void FixedUpdate()
        {
            // Arrows reaction

            if (Input.GetKey(KeyCode.UpArrow))
                transform.position += new Vector3(0f, StepSize, 0f);

            if (Input.GetKey(KeyCode.DownArrow))
                transform.position += new Vector3(0f, -StepSize, 0f);

            if (Input.GetKey(KeyCode.RightArrow))
                transform.position += new Vector3(StepSize, 0f, 0f);

            if (Input.GetKey(KeyCode.LeftArrow))
                transform.position += new Vector3(-StepSize, 0f, 0f);

            // Rotation reaction

            RotationGet();

            // Update entity object
            
            UpdateEntityObject((++FixedCounter) * Time.fixedDeltaTime);
        }

        private void Update()
        {
            // Send update to server

            PlayerUpdate playerUpdate = new PlayerUpdate(
                transform.position,
                Rotation
                );
            if (!playerUpdate.HasProblems)
            {
                References.Client.Send(playerUpdate.GetPacket());
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
            Camera.transform.position = new Vector3(plr.x, plr.y, Camera.transform.position.z);
            Camera.orthographicSize = CameraZoom;
        }

        private void RotationGet()
        {
            float mx = Input.mousePosition.x - (Screen.width / 2);
            float my = Input.mousePosition.y - (Screen.height / 2);
            Rotation = Mathf.Atan2(my, mx) * 180f / Mathf.PI;
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

        public void LoadPlayerData(PlayerInitialize msg)
        {
            transform.position = msg.Position;
            RotationGet();
            transform.parent.gameObject.SetActive(true);
            UpdateEntityObject(0f);
        }
    }
}
