using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Larnix.UI
{
    public class ScrollView : MonoBehaviour
    {
        [SerializeField] RectTransform Container;

        private readonly Stack<(RectTransform, float)> Elements = new();
        private float NextY = 0f;

        public void PushElement(RectTransform rt, float spacing = 0f)
        {
            rt.SetParent(Container, false);
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(0f, -NextY);

            float spacedSize = rt.rect.height + spacing;
            ChangeScale(spacedSize);
            Elements.Push((rt, spacedSize));
        }

        public bool RemoveTopElement()
        {
            if (Elements.Count > 0)
            {
                var element = Elements.Pop();
                Destroy(element.Item1.gameObject);
                ChangeScale(-element.Item2);
                return true;
            }
            return false;
        }

        public void ClearAll()
        {
            while (Elements.Count > 0)
                RemoveTopElement();
        }

        public List<RectTransform> GetAllTransforms()
        {
            return Elements.Select(pair => pair.Item1).ToList();
        }

        private void ChangeScale(float deltaHeight)
        {
            NextY += deltaHeight;
            Container.sizeDelta += new Vector2(0f, deltaHeight);

            const float EPSILON = 0.01f;
            if(NextY < EPSILON)
            {
                NextY = 0f;
                Container.sizeDelta = new Vector2(Container.sizeDelta.x, 0f);
            }
        }
    }
}
