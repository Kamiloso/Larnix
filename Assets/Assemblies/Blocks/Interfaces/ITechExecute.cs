using System.Collections;
using System.Collections.Generic;
using Larnix.Blocks.Structs;
using Larnix.Core;
using Larnix.Core.Json;

namespace Larnix.Blocks.All
{
    public interface ITechExecute : IBlockInterface
    {
        void Init()
        {
            This.Subscribe(BlockOrder.TechCmdExecute,
                () => ExecuteCommand());
        }

        private void ExecuteCommand()
        {
            Storage data = This.BlockData.Data;
            
            string command = data["tech_execute.command"].String;
            ICmdExecutor.InsertParameters(ref command, new Dictionary<string, string>
            {
                ["$x"] = This.Position.x.ToString(),
                ["$y"] = This.Position.y.ToString(),
            });

            WorldAPI.ExecuteCommand(command);

            BlockData1 replaceBlock = new(
                id: (BlockID)data["tech_execute.replace.id"].Int,
                variant: (byte)data["tech_execute.replace.variant"].Int,
                data: Storage.FromString(data["tech_execute.replace.data"].String)
            );
            WorldAPI.ReplaceBlock(This.Position, This.IsFront, replaceBlock);
        }
    }
}
