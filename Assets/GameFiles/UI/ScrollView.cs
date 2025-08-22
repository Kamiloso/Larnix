using System;
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
        [SerializeField] public UnityEngine.UI.ScrollRect ScrollRect;
        [SerializeField] RectTransform Container;

        private readonly Stack<(RectTransform, float)> Elements = new();
        private float NextY = 0f;

        public void BottomAddElement(RectTransform rt, float spacing = 0f)
        {
            float spacedSize = rt.rect.height + spacing;
            PushElement((rt, spacedSize));
        }

        public void TopAddElement(RectTransform rt, float spacing = 0f)
        {
            Stack<(RectTransform, float)> otherStack = new();

            while (Elements.Count > 0)
            {
                var element = PopElement();
                otherStack.Push(element);
            }

            BottomAddElement(rt, spacing);

            while (otherStack.Count > 0)
            {
                var element = otherStack.Pop();
                PushElement(element);
            }
        }

        public void BubbleUp(RectTransform rt)
        {
            Stack<(RectTransform, float)> otherStack = new();
            (RectTransform, float) bubble = default;

            while (Elements.Count > 0)
            {
                var element = PopElement();
                if (!ReferenceEquals(element.Item1, rt)) otherStack.Push(element);
                else bubble = element;
            }

            PushElement(bubble);

            while (otherStack.Count > 0)
            {
                var element = otherStack.Pop();
                PushElement(element);
            }
        }

        private void PushElement((RectTransform, float) element)
        {
            element.Item1.SetParent(Container, false);
            element.Item1.anchoredPosition = new Vector2(0f, -NextY);
            ChangeScale(element.Item2);
            Elements.Push(element);
        }

        public (RectTransform, float) PopElement()
        {
            var element = Elements.Pop();
            ChangeScale(-element.Item2);
            return element;
        }

        public void RemoveWhere(Func<RectTransform, bool> condition)
        {
            Stack<(RectTransform, float)> otherStack = new();

            while (Elements.Count > 0)
            {
                var element = PopElement();
                if (condition(element.Item1))
                {
                    Destroy(element.Item1.gameObject);
                }
                else
                {
                    otherStack.Push(element);
                }
            }

            while (otherStack.Count > 0)
            {
                var element = otherStack.Pop();
                PushElement(element);
            }
        }

        public void ClearAll()
        {
            while (Elements.Count > 0)
            {
                var element = PopElement();
                Destroy(element.Item1.gameObject);
            }
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
