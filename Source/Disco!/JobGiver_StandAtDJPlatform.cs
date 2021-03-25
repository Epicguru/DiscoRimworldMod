using Verse;
using Verse.AI;

namespace Disco
{
    public class JobGiver_StandAtDJPlatform : ThinkNode_JobGiver
    {
        protected override Job TryGiveJob(Pawn pawn)
        {
            var lordJob = pawn.Map?.lordManager?.LordOf(pawn)?.LordJob;
            if (lordJob is not LordJob_Joinable_Disco discoLord)
            {
                Core.Error($"Tried to give job to pawn {pawn.LabelShortCap} but that pawn does not have a lord job of type 'LordJob_Joinable_Disco'");
                return null;
            }

            Job job = JobMaker.MakeJob(DiscoDefOf.DSC_Job_StandAtDJPlatform, discoLord.DJStand.InteractionCell, discoLord.DJStand.Position);
            job.doUntilGatheringEnded = true;
            return job;
        }
    }
}
