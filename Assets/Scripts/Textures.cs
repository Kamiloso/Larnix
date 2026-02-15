using UnityEngine;
using UnityEngine.Tilemaps;
using Larnix.Blocks;
using Larnix;
using Larnix.Core.Vectors;
using Larnix.Blocks.All;

public class Textures
{
    public static Tile CreateTileObject(BlockID ID, byte variant)
    {
        Vec2Int rotation = Vec2Int.Up;

        IRotational iface = BlockFactory.GetSlaveInstance<IRotational>(ID);
        if (iface != null)
        {
            rotation = iface.STATIC_RotationDirection(variant);
            variant = iface.STATIC_RotationVariant(variant);
        }

        string path = "Blocks/" + ID.ToString() + "-" + variant + ".png";
        string fallbackPath = "Blocks/" + ID.ToString() + ".png";

        Texture2D texture = StreamingTextureLoader.Instance.LoadTextureSync(path);
        if (texture == null) texture = StreamingTextureLoader.Instance.LoadTextureSync(fallbackPath);
        if (texture == null) texture = StreamingTextureLoader.PinkTexture;

        Texture2D rotated = rotation == Vec2Int.Up ?
            texture : RotateTexture(texture, rotation);
        
        return MakeTileFromTexture(rotated);
    }

    public static Tile CreateBorderTileObject(byte borderByte, byte alphaByte)
    {
        const int SIZE = 16;

        Color transparent = new Color(0, 0, 0, 0);
        Color borderColor = new Color(0, 0, 0, alphaByte / 255f);

        // clear texture
        Color[] pixels = new Color[SIZE * SIZE];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = transparent;

        bool HasBit(int bit) => (borderByte & (1 << bit)) != 0;

        // up edge
        if (!HasBit(1))
            for (int x = 0; x < SIZE; x++)
                pixels[(SIZE - 1) * SIZE + x] = borderColor;

        // down edge
        if (!HasBit(6))
            for (int x = 0; x < SIZE; x++)
                pixels[x] = borderColor;

        // left edge
        if (!HasBit(3))
            for (int y = 0; y < SIZE; y++)
                pixels[y * SIZE] = borderColor;

        // right edge
        if (!HasBit(4))
            for (int y = 0; y < SIZE; y++)
                pixels[y * SIZE + (SIZE - 1)] = borderColor;

        // corners
        if (!HasBit(0)) pixels[(SIZE - 1) * SIZE] = borderColor;                 // top-left
        if (!HasBit(2)) pixels[(SIZE - 1) * SIZE + (SIZE - 1)] = borderColor;    // top-right
        if (!HasBit(5)) pixels[0] = borderColor;                                 // bottom-left
        if (!HasBit(7)) pixels[SIZE - 1] = borderColor;                          // bottom-right

        Texture2D texture = new Texture2D(SIZE, SIZE, TextureFormat.RGBA32, false, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
            mipMapBias = 0,
            anisoLevel = 0
        };

        texture.SetPixels(pixels);
        texture.Apply();

        return MakeTileFromTexture(texture);
    }

    private static Tile MakeTileFromTexture(Texture2D texture)
    {
        Sprite sprite = Sprite.Create(
            texture: texture,
            rect: new Rect(0, 0, texture.width, texture.height),
            pivot: new Vector2(0.5f, 0.5f),
            pixelsPerUnit: System.Math.Max(texture.width, texture.height),
            extrude: 0,
            meshType: SpriteMeshType.FullRect
        );

        Tile tile = ScriptableObject.CreateInstance<Tile>();
        tile.sprite = sprite;
        tile.color = Color.white;
        tile.colliderType = Tile.ColliderType.None;

        return tile;
    }

    private static Texture2D RotateTexture(Texture2D original, Vec2Int rotation)
    {
        if (rotation == Vec2Int.Up)
            return original;

        int width = original.width;
        int height = original.height;

        int rot = rotation == Vec2Int.Right ? 1 :
                rotation == Vec2Int.Down  ? 2 :
                rotation == Vec2Int.Left  ? 3 : 0;

        int newWidth  = (rot % 2 == 0) ? width  : height;
        int newHeight = (rot % 2 == 0) ? height : width;

        Color[] src = original.GetPixels();
        Color[] dst = new Color[src.Length];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int srcIndex = y * width + x;

                int nx = x;
                int ny = y;

                switch (rot)
                {
                    case 1: // 90 CW
                        nx = y;
                        ny = width - 1 - x;
                        break;

                    case 2: // 180 CW
                        nx = width - 1 - x;
                        ny = height - 1 - y;
                        break;

                    case 3: // 270 CW
                        nx = height - 1 - y;
                        ny = x;
                        break;
                }

                int dstIndex = ny * newWidth + nx;
                dst[dstIndex] = src[srcIndex];
            }
        }

        Texture2D rotated = new Texture2D(newWidth, newHeight, original.format, false, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
            mipMapBias = 0,
            anisoLevel = 0
        };

        rotated.SetPixels(dst);
        rotated.Apply(updateMipmaps: false, makeNoLongerReadable: false);

        return rotated;
    }
}
