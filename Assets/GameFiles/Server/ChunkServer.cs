using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Larnix.Blocks;

namespace Larnix.Server
{
    public class ChunkServer : MonoBehaviour
    {
        public Vector2Int Chunkpos { get; private set; }

        private GameObject[,,] Blocks = new GameObject[2, 16, 16];

        [SerializeField] Transform Front;
        [SerializeField] Transform Back;

        public void Initialize(Vector2Int chunkpos)
        {
            this.Chunkpos = chunkpos;

            for(int x = 0; x < 16; x++)
                for(int y = 0; y < 16; y++)
                {
                    BlockData blockData = UnityEngine.Random.Range(0, 5) == 0 ?
                        new BlockData(new SingleBlockData{ ID = BlockID.Stone }, new()) :
                        new BlockData(new(), new());

                    SetBlock(x, y, BlockLayer.Front, blockData);
                    SetBlock(x, y, BlockLayer.Back, blockData);
                }
        }

        private void SetBlock(int x, int y, BlockLayer layer, BlockData blockData)
        {
            SingleBlockData block = layer == BlockLayer.Front ? blockData.Front : blockData.Back;
            Vector2Int blockCoords = BlockCoords(x, y);

            GameObject blockObj = Prefabs.CreateBlock(block.ID, Prefabs.Mode.Server);

            blockObj.transform.position = (Vector2)blockCoords;
            blockObj.transform.SetParent(layer == BlockLayer.Front ? Front : Back, true);
            blockObj.transform.name = block.ID.ToString() + " [ " + blockCoords.x + ", " + blockCoords.y + "]";

            if (Blocks[(byte)layer, x, y] != null)
                Destroy(Blocks[(byte)layer, x, y]);

            Blocks[(byte)layer, x, y] = blockObj;
        }

        private Vector2Int BlockCoords(int x, int y)
        {
            return new Vector2Int(
                16 * Chunkpos.x + x,
                16 * Chunkpos.y + y
                );
        }
    }
}
