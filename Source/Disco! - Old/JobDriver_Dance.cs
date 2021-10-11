using RimWorld;
using Verse;
using Verse.AI;

namespace Disco
{
    public abstract class JobDriver_Dance : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        protected Toil SetPosture(PawnPosture posture)
        {
            return new Toil()
            {
                initAction = () =>
                {
                    pawn.jobs.posture = posture;
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };
        }

        protected Toil FaceDir(int offX, int offY)
        {
            return new Toil()
            {
                initAction = () =>
                {
                    pawn.rotationTracker.FaceCell(pawn.Position + new IntVec3(offX, 0, offY));
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };
        }

        protected Toil GoToDanceFloorOrMoveSlightly()
        {
            var lordJob = pawn.Map?.lordManager?.LordOf(pawn)?.LordJob;
            if (lordJob is not LordJob_Joinable_Disco discoLord)
            {
                Core.Warn($"Tried to give job to pawn {pawn.LabelShortCap} but that pawn does not have a lord job of type 'LordJob_Joinable_Disco'");
                return null;
            }
            return Toils_Goto.GotoCell(discoLord.DJStand.DancingCells.RandomElement(), PathEndMode.OnCell);
        }
    }
}
