using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Larnix.Socket.Commands;
using Larnix.Entities;

namespace Larnix.Client
{
    public class MainPlayer : MonoBehaviour
    {
        [SerializeField] Camera Camera;
        [SerializeField] EntityProjection EntityProjection;

        private Vector2 Position;
        private float Rotation;

        private const float StepSize = 0.1f;

        private const float CameraZoomMin = 3.0f;
        private const float CameraZoomMax = 8.0f;
        private const float CameraZoomStep = 0.5f;

        private float CameraZoom = 5.5f; // default zoom

        private void Awake()
        {
            References.MainPlayer = this;
            transform.parent.gameObject.SetActive(false);
        }

        private void FixedUpdate()
        {
            // Arrows reaction

            if (Input.GetKey(KeyCode.UpArrow))
                Position += new Vector2(0f, StepSize);

            if (Input.GetKey(KeyCode.DownArrow))
                Position += new Vector2(0f, -StepSize);

            if (Input.GetKey(KeyCode.RightArrow))
                Position += new Vector2(StepSize, 0f);

            if (Input.GetKey(KeyCode.LeftArrow))
                Position += new Vector2(-StepSize, 0f);

            // Rotation spin (temporary)

            Rotation += 1f;
            while (Rotation > 360f)
                Rotation -= 360f;
        }

        private void Update()
        {
            // Send update to server

            PlayerUpdate playerUpdate = new PlayerUpdate(
                Position,
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

            // Update entity object

            EntityProjection.UpdateTransform(new EntityData {
                ID = EntityData.EntityID.Player,
                Position = Position,
                Rotation = Rotation,
                NBT = null
            });
        }

        private void LateUpdate()
        {
            // Camera update
            
            Camera.orthographicSize = CameraZoom;
        }

        public void LoadPlayerData(PlayerInitialize msg)
        {
            Position = msg.Position;
            transform.parent.gameObject.SetActive(true);
        }
    }
}
