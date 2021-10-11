using System.Collections.Generic;
using RimWorld;
using Verse.AI;

namespace Disco
{
    public class JobDriver_DanceBreakdance : JobDriver_Dance
    {
        protected override IEnumerable<Toil> MakeNewToils()
        {
            // Go to the dance floor or move across it slightly.
            var goToFloor = GoToDanceFloorOrMoveSlightly();
            if (goToFloor != null)
                yield return goToFloor;

            // Stand still.
            yield return Toils_General.StopDead();

            // Spin side-down.
            yield return SetPosture(PawnPosture.LayingOnGroundNormal);
            yield return Toils_General.Wait(120);

            // Get up and do a little shuffle.
            yield return SetPosture(PawnPosture.Standing);
            yield return Toils_General.Wait(15);
            yield return FaceDir(1, 0);
            yield return Toils_General.Wait(15);
            yield return FaceDir(0, -1);
            yield return Toils_General.Wait(15);
            yield return FaceDir(-1, 0);
            yield return Toils_General.Wait(15);
            yield return FaceDir(0, -1);
            yield return Toils_General.Wait(25);

            // Spin face-up
            yield return SetPosture(PawnPosture.LayingOnGroundFaceUp);
            yield return Toils_General.Wait(120);

            // Get up and do a little shuffle.
            yield return SetPosture(PawnPosture.Standing);
            yield return Toils_General.Wait(15);
            yield return FaceDir(1, 0);
            yield return Toils_General.Wait(15);
            yield return FaceDir(0, -1);
            yield return Toils_General.Wait(15);
            yield return FaceDir(-1, 0);
            yield return Toils_General.Wait(15);
            yield return FaceDir(0, -1);
            yield return Toils_General.Wait(25);
        }
    }
}
