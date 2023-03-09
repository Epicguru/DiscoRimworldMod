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

        public LordToil_Disco(IntVec3 spot, GatheringDef gatheringDef, float joyPerTick = DefaultJoyPerTick)
            : base(spot, gatheringDef)
        {
            this.joyPerTick = joyPerTick;
            this.data = new LordToilData_Disco();
        }

        public override void LordToilTick()
        {
            List<Pawn> ownedPawns = lord.ownedPawns;
            if (ownedPawns == null)
                return;

            for (int index = 0; index < ownedPawns.Count; ++index)
            {
                var pawn = ownedPawns[index];
                if (pawn == null)
                    continue;

                if (GatheringsUtility.InGatheringArea(pawn.Position, spot, Map))
                {
                    ownedPawns[index].needs?.joy?.GainJoy(joyPerTick, JoyKindDefOf.Social);
                    Data.wasPresent.Add(ownedPawns[index]);
                }
            }
        }
    }
}
