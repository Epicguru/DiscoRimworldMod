using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace Disco
{
    public class GatheringWorker_Disco : GatheringWorker
    {
        protected override LordJob CreateLordJob(IntVec3 spot, Pawn organizer)
        {
            var djTable = organizer?.Map?.GetComponent<DiscoTracker>()?.GetAllValidDJStands()?.FirstOrFallback();
            if (djTable == null)
            {
                Core.Error("Failed to find DJ table!");
                return null;
            }

            return new LordJob_Joinable_Disco(spot, organizer, this.def, djTable);
        }

        protected override bool TryFindGatherSpot(Pawn organizer, out IntVec3 spot) => RCellFinder.TryFindGatheringSpot_NewTemp(organizer, this.def, false, out spot);

        protected override void SendLetter(IntVec3 spot, Pawn organizer)
        {
            base.SendLetter(spot, organizer);

            organizer.mindState.priorityWork.ClearPrioritizedWorkAndJobQueue();
            var current = organizer.jobs.curJob;
            var currentDriver = organizer.jobs.curDriver;
            organizer.jobs.EndCurrentJob(JobCondition.InterruptForced, true);
            Core.Log($"Ended {organizer}'s current job: {current} ({currentDriver})");
        }
    }
}
