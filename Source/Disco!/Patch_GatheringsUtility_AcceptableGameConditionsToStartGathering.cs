using HarmonyLib;
using RimWorld;
using Verse;

namespace Disco
{
    [HarmonyPatch(typeof(GatheringsUtility), "AcceptableGameConditionsToStartGathering")]
    static class Patch_GatheringsUtility_AcceptableGameConditionsToStartGathering
    {
        static bool Prefix(Map map, GatheringDef gatheringDef, ref bool __result)
        {
            if (gatheringDef == DiscoDefOf.DSC_DiscoGathering)
            {
                if (!GatheringsUtility.AcceptableGameConditionsToContinueGathering(map) || GatheringsUtility.AnyLordJobPreventsNewGatherings(map) || map.dangerWatcher.DangerRating != StoryDanger.None)
                {
                    __result = false;
                    return false;
                }

                int colonistsSpawnedCount = map.mapPawns.FreeColonistsSpawnedCount;
                if (colonistsSpawnedCount < 3)
                    return false;

                int num = 0;
                foreach (Pawn pawn in map.mapPawns.FreeColonistsSpawned)
                {
                    if (pawn.health.hediffSet.BleedRateTotal > 0.05)
                    {
                        __result = false;
                        return false;
                    }
                    if (pawn.Drafted)
                        num++;
                }

                __result = num / (double)colonistsSpawnedCount < 0.75 && GatheringsUtility.EnoughPotentialGuestsToStartGathering(map, gatheringDef);
                return false;
            }

            return true;
        }
    }
}
