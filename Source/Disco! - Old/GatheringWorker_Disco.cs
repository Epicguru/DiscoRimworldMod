using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace Disco
{
    public class GatheringWorker_Disco : GatheringWorker
    {
        private Building_DJStand tempStand;

        protected override LordJob CreateLordJob(IntVec3 spot, Pawn organizer)
        {
            if (tempStand == null)
            {
                Core.Error("Failed to find DJ table!");
                return null;
            }

            var job =  new LordJob_Joinable_Disco(spot, organizer, this.def, tempStand);
            tempStand = null;
            return job;
        }

        protected override bool TryFindGatherSpot(Pawn organizer, out IntVec3 spot)
        {
            //return RCellFinder.TryFindGatheringSpot_NewTemp(organizer, this.def, false, out spot);
            var tracker = organizer.Map?.GetComponent<DiscoTracker>();
            spot = default;
            if (tracker == null)
            {
                Core.Warn($"Organizer {organizer.LabelShortCap}'s map is null or does not have a DiscoTracker comp");
                return false;
            }

            var stands = tracker.GetAllValidDJStands();
            if (stands.EnumerableNullOrEmpty())
                return false;

            tempStand = stands.RandomElementByWeight(dj => dj.FloorBounds.Area);
            spot =  tempStand.GetGatherSpot();
            return true;
        }

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
