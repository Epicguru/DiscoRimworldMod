using RimWorld;
using UnityEngine;
using Verse;

namespace Disco.Programs
{
    public class NowPlayingMessage : DiscoProgram
    {
        public NowPlayingMessage(ProgramDef def) : base(def)
        {
        }

        public override void Init()
        {
            Messages.Message($"Now playing: {Def.Get("credits", "Unknown")}", MessageTypeDefOf.PositiveEvent);
            Remove();
        }

        public override Color ColorFor(IntVec3 cell)
        {
            return default;
        }
    }
}
