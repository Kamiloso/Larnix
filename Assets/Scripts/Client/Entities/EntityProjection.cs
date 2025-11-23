using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Larnix.Entities;
using Larnix.Client.Entities.Body;
using Larnix.Core.Utils;

namespace Larnix.Client.Entities
{
    public class EntityProjection : MonoBehaviour
    {
        [SerializeField] Transform NameField;
        [SerializeField] string RenderingLayer;
        [SerializeField] float SmoothDelay = 0.1f;

        private bool Initialized = false;
        private Smoother Smoother;

        public double? LastTime { get; private set; } = null;

        public string NBT { get; private set; } = null;

        private void Start()
        {
            SetRenderingLayer();
        }

        private static int previousLayer = Common.Rand().Next(1, 6007);
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
                Smoother = new Smoother(new Smoother.Record{
                    Position = entityData.Position,
                    Rotation = entityData.Rotation,
                    Time = time
                }, delay: SmoothDelay);
                Initialized = true;
            }
            else
            {
                Smoother.AddRecord(new Smoother.Record
                {
                    Position = entityData.Position,
                    Rotation = entityData.Rotation,
                    Time = time
                });
            }

            LastTime = time;
        }

        public void ResetSmoother()
        {
            Initialized = false;
            LastTime = null;
        }

        private void Update()
        {
            // Update smooth & position

            Smoother.UpdateSmooth(Time.deltaTime);
            transform.position = Ref.MainPlayer.ToUnityPos(Smoother.Position);

            // Animations

            HeadRotor HeadRotor = GetComponent<HeadRotor>();
            if (HeadRotor != null) HeadRotor.HeadRotate(Smoother.Rotation);

            LimbAnimator LimbAnimator = GetComponent<LimbAnimator>();
            if(LimbAnimator != null) LimbAnimator.DoUpdate();
        }
    }
}
