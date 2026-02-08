using System.Collections;
using System.Collections.Generic;
using Larnix.Blocks.Structs;
using Larnix.Core.Json;
using Larnix.Core.Vectors;

namespace Larnix.Blocks
{
    public interface ITechExecute : IBlockInterface
    {
        void Init()
        {
            This.FrameEventSequentialLate += (sender, args) => ExecuteCommand();
        }

        private void ExecuteCommand()
        {
            int X = This.Position.x;
            int Y = This.Position.y;
            string FRONT = This.IsFront ? "front" : "back";

            Storage data = This.BlockData.Data;
            
            string command = data["tech_execute.command"].String
                .Replace("$x", X.ToString())
                .Replace("$y", Y.ToString())
                .Replace("$front", FRONT);

            WorldAPI.ExecuteCommand(command);

            BlockData1 replaceBlock = new(
                id: (BlockID)data["tech_execute.replace.id"].Int,
                variant: (byte)data["tech_execute.replace.variant"].Int
            );
            WorldAPI.ReplaceBlock(This.Position, This.IsFront, replaceBlock);
        }
    }
}
