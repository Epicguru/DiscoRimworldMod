using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Disco
{
    public class LordToil_Disco : LordToil_Gathering
    {
        private float joyPerTick = 3.5E-05f;
        public const float DefaultJoyPerTick = 3.5E-05f;

        private LordToilData_Disco Data => (LordToilData_Disco)data;

        public LordToil_Disco(IntVec3 spot, GatheringDef gatheringDef, float joyPerTick = 3.5E-05f)
            : base(spot, gatheringDef)
        {
            this.joyPerTick = joyPerTick;
            this.data = new LordToilData_Disco();
        }

        public override void LordToilTick()
        {
            List<Pawn> ownedPawns = lord.ownedPawns;
            for (int index = 0; index < ownedPawns.Count; ++index)
            {
                if (GatheringsUtility.InGatheringArea(ownedPawns[index].Position, spot, Map))
                {
                    ownedPawns[index].needs.joy.GainJoy(this.joyPerTick, JoyKindDefOf.Social);
                    if (!Data.presentForTicks.ContainsKey(ownedPawns[index]))
                        Data.presentForTicks.Add(ownedPawns[index], 0);
                    Data.presentForTicks[ownedPawns[index]]++;
                }
            }
        }
    }
}
