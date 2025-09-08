using UnityEngine;

namespace Larnix.Blocks
{
    public interface IFalling : IMovingBehaviour
    {
        void Init()
        {
            This.FrameEventRandom += (sender, args) => Fall();
        }

        int FALL_PERIOD();

        private void Fall()
        {
            if (WorldAPI.FramesSinceServerStart() % FALL_PERIOD() != 0)
                return;

            Vector2Int localpos = This.Position;
            Vector2Int downpos = localpos - new Vector2Int(0, 1);

            if (CanMove(localpos, downpos, This.IsFront))
            {
                Move(localpos, downpos, This.IsFront);
            }
        }
    }
}
