using Verse;
using Verse.AI;

namespace Disco
{
    public class ThinkNode_ConditionalIsDJ : ThinkNode_Conditional
    {
        protected override bool Satisfied(Pawn pawn)
        {
            if (pawn.DestroyedOrNull())
                return false;

            var lordJob = pawn.Map?.lordManager?.LordOf(pawn)?.LordJob;
            if (lordJob is not LordJob_Joinable_Disco discoLord)
                return false;

            bool isDJ = discoLord.Organizer == pawn;
            return isDJ;
        }
    }
}
