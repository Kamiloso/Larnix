using Larnix.Core;
using Larnix.Core.Physics;
using System.Collections;
using System.Collections.Generic;
using Larnix.Core.Utils;

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

            // change action

            if (--Data["action.ticks"].Int <= 0)
            {
                string action = Data["action.current"].String;
                string nextAction = action switch
                {
                    "WALK_LEFT" => "",
                    "WALK_RIGHT" => "",
                    _ => Common.Rand().Next() % 2 == 0 ? "WALK_LEFT" : "WALK_RIGHT",
                };

                Data["action.current"].String = nextAction;
                Data["action.ticks"].Int = Common.Rand().Next(
                    MIN_THINKING_TIME(),
                    MAX_THINKING_TIME()
                    );
            }

            // perform action

            string finalAction = Data["action.current"].String;
            return new InputData
            {
                Jump = touchesWall,
                Left = finalAction == "WALK_LEFT",
                Right = finalAction == "WALK_RIGHT",
            };
        }
    }
}
