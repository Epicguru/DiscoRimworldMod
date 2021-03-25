using System.Collections.Generic;
using Verse;
using Verse.AI.Group;

namespace Disco
{
    public class LordToilData_Disco : LordToilData
    {
        public Dictionary<Pawn, int> presentForTicks = new Dictionary<Pawn, int>();

        public override void ExposeData()
        {
            if (Scribe.mode == LoadSaveMode.Saving)
                presentForTicks.RemoveAll(x => x.Key.Destroyed);

            Scribe_Collections.Look(ref this.presentForTicks, "presentForTicks", LookMode.Reference);
        }
    }
}
