using Larnix.Core;
using Larnix.Core.Physics;
using System.Collections;
using System.Collections.Generic;
using Larnix.Core.Utils;
using ActionType = Larnix.Entities.EntityNBT.ActionType;

namespace Larnix.Entities
{
    public interface IWalkingCreature : IPhysics
    {
        InputData IPhysics.inputData => GenerateNextInput();

        int MIN_THINKING_TIME();
        int MAX_THINKING_TIME();

        private InputData GenerateNextInput()
        {
            bool touchesWall = (outputData?.OnLeftWall ?? false) ||
                               (outputData?.OnRightWall ?? false);

            if (--NBT.NextActionTicks <= 0) // change action
            {
                ActionType action = NBT.Action;
                ActionType nextAction = action switch
                {
                    ActionType.WalkingLeft => ActionType.None,
                    ActionType.WalkingRight => ActionType.None,
                    _ => Common.Rand().Next() % 2 == 0 ? ActionType.WalkingLeft : ActionType.WalkingRight,
                };

                NBT.Action = nextAction;
                NBT.NextActionTicks = Common.Rand().Next(
                    MIN_THINKING_TIME(),
                    MAX_THINKING_TIME()
                    );
            }

            // perform action

            ActionType finalAction = NBT.Action;
            return new InputData
            {
                Jump = touchesWall,
                Left = finalAction == ActionType.WalkingLeft,
                Right = finalAction == ActionType.WalkingRight,
            };
        }
    }
}
