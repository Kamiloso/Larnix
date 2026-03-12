using Larnix.Client.Relativity;
using Larnix.Core.Vectors;
using UnityEngine;
using UnityEngine.Tilemaps;
using Larnix.GameCore.Structs;
using Larnix.Client.Graphics;

namespace Larnix.Client.Terrain.Selector
{
    public class SelectorDisplay : MonoBehaviour
    {
        public const float TRANSPARENCY = 0.70f;
        public const float BACK_DARKING = 0.45f;

        [SerializeField] SpriteRenderer SpriteRenderer;

        public void Prepare()
        {
            SpriteRenderer.transform.localScale = Vector3.one;
            SpriteRenderer.transform.localRotation = Quaternion.identity;
        }

        public void Hide()
        {
            SpriteRenderer.enabled = false;
        }

        public void ShowAt(Vec2Int POS)
        {
            SpriteRenderer.transform.SetLarnixPos(POS);
            SpriteRenderer.enabled = true;
        }

        public void DisplayTool(Tile tile, bool pointsRight, bool front)
        {
            SpriteRenderer.sprite = tile.sprite;
            SpriteRenderer.color = Color.white;
            SpriteRenderer.transform.localScale = new Vector3(pointsRight ? 1f : -1f, 1f, 1f) * 0.9f;
            SpriteRenderer.transform.localRotation = Quaternion.Euler(0f, 0f,
                (front ? 0f : 1f) * (pointsRight ? -1f : 1f) * 90f);
            SpriteRenderer.sortingLayerName = front ? "FrontBlocks" : "BackBlocks";
        }

        public void DisplayBlockPreview(Tile tile, bool front)
        {
            SpriteRenderer.sprite = tile.sprite;

            Color transpColor = new(1, 1, 1, TRANSPARENCY);

            if (front)
            {
                SpriteRenderer.sortingLayerName = "FrontBlocks";
                SpriteRenderer.color = transpColor;
            }
            else
            {
                Color darkerColor = new(0, 0, 0, TRANSPARENCY);
                SpriteRenderer.sortingLayerName = "BackBlocks";
                SpriteRenderer.color = Color.Lerp(transpColor, darkerColor, BACK_DARKING);
            }
        }

        public void DisplayEmpty()
        {
            var emptySprite = Tiles.GetSprite(BlockHeader1.Air, true);
            SpriteRenderer.sprite = emptySprite;
        }
    }
}
