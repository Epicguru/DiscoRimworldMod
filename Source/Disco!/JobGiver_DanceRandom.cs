using System;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace Disco
{
    public class JobGiver_DanceRandom : ThinkNode_JobGiver
    {
        private static List<JobDef> allDances;

        protected override Job TryGiveJob(Pawn pawn)
        {
            if (allDances == null)
            {
                allDances = new List<JobDef>(32);
                Type targetType = typeof(JobDriver_Dance);
                foreach(var jdef in DefDatabase<JobDef>.AllDefsListForReading)
                {
                    if (jdef.driverClass.IsSubclassOf(targetType))
                        allDances.Add(jdef);
                }
                Core.Log($"Found {allDances.Count} dance job defs.");
            }

            Job job = JobMaker.MakeJob(allDances.RandomElement());
            return job;
        }
    }
}
