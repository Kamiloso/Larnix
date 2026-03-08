using Larnix.GameCore;
using Larnix.GameCore.Physics;
using System.Collections;
using System.Collections.Generic;
using Larnix.GameCore.Utils;
using Larnix.Core.Misc;

namespace Larnix.Entities.All
{
    public interface IWalking : IPhysics
    {
        InputData IPhysics.inputData => GenerateNextInput();

        int MIN_THINKING_TIME();
        int MAX_THINKING_TIME();

        private InputData GenerateNextInput()
        {
            bool touchesWall = (outputData?.OnLeftWall ?? false) ||
                               (outputData?.OnRightWall ?? false);

            // change action

            if (--Data["walking.next_in"].Int <= 0)
            {
                string action = Data["walking.state"].String;
                string nextAction = action switch
                {
                    "LEFT" => "",
                    "RIGHT" => "",
                    _ => RandUtils.NextBool() ? "LEFT" : "RIGHT",
                };

                Data["walking.state"].String = nextAction;
                Data["walking.next_in"].Int = RandUtils.GetInt(
                    MIN_THINKING_TIME(),
                    MAX_THINKING_TIME()
                    );
            }

            // perform action

            string finalAction = Data["walking.state"].String;
            return new InputData
            {
                Jump = touchesWall,
                Left = finalAction == "LEFT",
                Right = finalAction == "RIGHT",
            };
        }
    }
}
