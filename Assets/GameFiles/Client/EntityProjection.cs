using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Larnix.Entities;
using Larnix.Socket.Commands;
using Larnix.Entities.Client;
using Unity.Collections;

namespace Larnix.Client
{
    public class EntityProjection : MonoBehaviour
    {
        [SerializeField] Transform NameField;
        [SerializeField] string RenderingLayer;

        private bool Initialized = false;
        private Smoother Smoother;

        public string NBT { get; private set; } = null;

        private void Start()
        {
            SetRenderingLayer();
        }

        private static int previousLayer = (new System.Random()).Next(1, 6007);
        private void SetRenderingLayer()
        {
            int currentLayer = previousLayer * 211 % 6007;
            foreach (var sprite in GetAllRenderers(transform))
            {
                sprite.sortingLayerName = RenderingLayer;
                sprite.sortingOrder += 10 * (-3000 + currentLayer);
            }
            previousLayer = currentLayer;
        }

        private List<SpriteRenderer> GetAllRenderers(Transform tran)
        {
            List<SpriteRenderer> renderers = new List<SpriteRenderer>();
            
            SpriteRenderer sp_rend = tran.GetComponent<SpriteRenderer>();
            if (sp_rend != null)
                renderers.Add(sp_rend);

            foreach(Transform trn in tran)
            {
                renderers.AddRange(GetAllRenderers(trn));
            }

            return renderers;
        }

        public void UpdateTransform(EntityData entityData, double time)
        {
            if (!Initialized)
            {
                Smoother = new Smoother(time, entityData.Position, entityData.Rotation);
                Initialized = true;
            }
            else
            {
                Smoother.AddRecord(
                    time,
                    entityData.Position,
                    entityData.Rotation
                    );
            }
        }

        public void ResetSmoother()
        {
            Initialized = false;
        }

        private void Update()
        {
            // Update smooth & position

            Smoother.UpdateSmooth(Time.deltaTime);
            transform.position = Smoother.GetSmoothedPosition();

            // Animations

            HeadRotor HeadRotor = GetComponent<HeadRotor>();
            if (HeadRotor != null) HeadRotor.HeadRotate(Smoother.GetSmoothedRotation());

            LimbAnimator LimbAnimator = GetComponent<LimbAnimator>();
            if(LimbAnimator != null) LimbAnimator.DoUpdate();
        }
    }
}
