using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI.Group;

namespace Disco
{
    public class LordToilData_Disco : LordToilData
    {
        public HashSet<Pawn> wasPresent = new HashSet<Pawn>();

        public override void ExposeData()
        {
            if (Scribe.mode == LoadSaveMode.Saving)
                this.wasPresent.RemoveWhere(x => x.Destroyed);

            Scribe_Collections.Look(ref wasPresent, "dsc_wasPresent", LookMode.Reference);
            wasPresent ??= new HashSet<Pawn>();
        }
    }
}
