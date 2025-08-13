using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class ContureFit : MonoBehaviour
{
    [SerializeField] RectTransform Conture;
    [SerializeField] float ContureBold = 2f; // multiplier
    [SerializeField] float ContureOffset = 5f; // pixels

    private void Update()
    {
        if (transform as RectTransform == null || Conture == null)
            return;

        FitSize();
    }

    private void FitSize()
    {
        RectTransform rectTransform = transform as RectTransform;
        float width = rectTransform.rect.width;
        float height = rectTransform.rect.height;

        Conture.sizeDelta = new Vector2(width, height) / ContureBold + Vector2.one * ContureOffset;
        Conture.localScale = Vector2.one * ContureBold;
    }
}
