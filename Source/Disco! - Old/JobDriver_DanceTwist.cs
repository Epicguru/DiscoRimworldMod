using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace Disco
{
    public class JobDriver_DanceTwist : JobDriver_Dance
    {
        protected override IEnumerable<Toil> MakeNewToils()
        {
            // Go to the dance floor or move across it slightly.
            var goToFloor = GoToDanceFloorOrMoveSlightly();
            if (goToFloor != null)
                yield return goToFloor;

            // Stand still.
            yield return Toils_General.StopDead();

            // For 15 times...
            IntVec2 lastOff = default;
            for (int i = 0; i < 15; i++)
            {
                // Pick a random direction to look in (not the same as last time)
                IntVec2 look = lastOff;
                while (look == lastOff)
                    look = Rand.Chance(0.5f)
                        ? new IntVec2(Rand.Chance(0.5f) ? 1 : -1, 0)
                        : new IntVec2(0, Rand.Chance(0.5f) ? 1 : -1);
                lastOff = look;

                // Face that direction.
                yield return FaceDir(look.x, look.z);

                // Keep looking for 20 ticks, then repeat.
                yield return Toils_General.Wait(20);
            }
        }
    }
}
