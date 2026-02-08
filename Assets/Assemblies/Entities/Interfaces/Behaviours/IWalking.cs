using Larnix.Core;
using Larnix.Core.Physics;
using System.Collections;
using System.Collections.Generic;
using Larnix.Core.Utils;

namespace Larnix.Entities
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
                    _ => Common.Rand().Next() % 2 == 0 ? "LEFT" : "RIGHT",
                };

                Data["walking.state"].String = nextAction;
                Data["walking.next_in"].Int = Common.Rand().Next(
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
