using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace Disco
{
    public class LordJob_Joinable_Disco : LordJob_Joinable_Gathering
    {
#if V12
        public int DurationTicks => this.durationTicks;
        private int durationTicks;
#endif

        public override bool AllowStartNewGatherings => false;
        protected virtual ThoughtDef AttendeeThought => DiscoDefOf.DSC_AttendedDiscoThought;
        protected virtual TaleDef AttendeeTale => DiscoDefOf.DSC_AttendedDiscoTale;
        protected virtual ThoughtDef OrganizerThought => DiscoDefOf.DSC_AttendedDiscoThought;
        protected virtual TaleDef OrganizerTale => DiscoDefOf.DSC_AttendedDiscoTale;
        public Building_DJStand DJStand;

        public LordJob_Joinable_Disco() { }

        public LordJob_Joinable_Disco(IntVec3 spot, Pawn organizer, GatheringDef gatheringDef, Building_DJStand stand) : base(spot, organizer, gatheringDef)
        {
            this.durationTicks = Rand.RangeInclusive(10000, 15000);
            this.DJStand = stand;
        }

        public override string GetReport(Pawn pawn) => "DSC.AtTheDisco".Translate();

        protected override LordToil CreateGatheringToil(IntVec3 spot, Pawn organizer, GatheringDef gatheringDef)
        {
            return new LordToil_Disco(spot, gatheringDef);
        }

        public override StateGraph CreateGraph()
        {
            StateGraph stateGraph = new StateGraph();

            LordToil disco = CreateGatheringToil(spot, organizer, gatheringDef);
            stateGraph.AddToil(disco);

            LordToil_End lordToilEnd = new LordToil_End();
            stateGraph.AddToil(lordToilEnd);

             Transition callOffTransition = new Transition(disco, lordToilEnd);
            callOffTransition.AddTrigger(new Trigger_TickCondition(this.ShouldBeCalledOff));
            callOffTransition.AddTrigger(new Trigger_PawnKilled());
            callOffTransition.AddTrigger(new Trigger_PawnLost(PawnLostCondition.LeftVoluntarily, this.organizer));
            callOffTransition.AddPreAction(new TransitionAction_Custom(() => ApplyOutcome((LordToil_Disco)disco)));
            callOffTransition.AddPreAction(new TransitionAction_Message(this.gatheringDef.calledOffMessage, MessageTypeDefOf.NegativeEvent, new TargetInfo(spot, Map)));
            stateGraph.AddTransition(callOffTransition);

            timeoutTrigger = GetTimeoutTrigger();
            Transition naturalEndTransition = new Transition(disco, lordToilEnd);
            naturalEndTransition.AddTrigger(this.timeoutTrigger);
            naturalEndTransition.AddPreAction(new TransitionAction_Custom(() => ApplyOutcome((LordToil_Disco)disco)));
            naturalEndTransition.AddPreAction(new TransitionAction_Message(gatheringDef.finishedMessage, MessageTypeDefOf.SituationResolved, new TargetInfo(spot, Map)));
            stateGraph.AddTransition(naturalEndTransition);

            Core.Log("Created disco state graph.");

            return stateGraph;
        }

        protected override bool ShouldBeCalledOff()
        {
            return base.ShouldBeCalledOff() || (DJStand?.NoPowerShutdown ?? true);
        }

#if V12
        protected virtual Trigger_TicksPassed GetTimeoutTrigger() => new Trigger_TicksPassed(this.durationTicks);
#endif
        private void ApplyOutcome(LordToil_Disco toil)
        {
            List<Pawn> ownedPawns = this.lord.ownedPawns;
            LordToilData_Disco data = (LordToilData_Disco)toil.data;
            int given = 0;
            for (int index = 0; index < ownedPawns.Count; ++index)
            {
                Pawn key = ownedPawns[index];
                bool flag = key == organizer;
                if (data.wasPresent.Contains(key))
                {
                    if (ownedPawns[index].needs.mood != null)
                    {
                        ThoughtDef def = flag ? OrganizerThought : AttendeeThought;
                        Thought_Memory newThought = (Thought_Memory)ThoughtMaker.MakeThought(def);
                        newThought.moodPowerFactor = 1;
                        ownedPawns[index].needs.mood.thoughts.memories.TryGainMemory(newThought);
                        given++;
                    }
                    TaleRecorder.RecordTale(flag ? OrganizerTale : AttendeeTale, ownedPawns[index], organizer);
                }
            }

            Core.Log($"Gave positive vibes to {given} pawns.");
        }

        public override float VoluntaryJoinPriorityFor(Pawn p)
        {
            float std = base.VoluntaryJoinPriorityFor(p);
            if (p == Organizer)
                return 200; // The DJ really, really has to be there...
            return std * 1.5f; // Other pawns want to go to the disco quite a lot too.
        }

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_References.Look(ref DJStand, "rf_djStand");
            Scribe_Values.Look(ref durationTicks, "durationTicks");
            if (Scribe.mode != LoadSaveMode.PostLoadInit || this.gatheringDef != null)
                return;

            this.gatheringDef = DiscoDefOf.DSC_DiscoGathering;
        }
    }
}
