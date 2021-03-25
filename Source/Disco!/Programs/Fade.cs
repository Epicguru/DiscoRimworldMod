using UnityEngine;
using Verse;

namespace Disco.Programs
{
    public class Fade : DiscoProgram
    {
        public int Duration;
        public bool FadeIn;

        private float p;
        private int counter;
        private Color currentColor;

        public Fade(ProgramDef def) : base(def)
        {
        }

        public override void Init()
        {
            Duration = Def.Get("duration", 30);
            FadeIn = Def.Get("fadeIn", true);
        }

        public override void Tick()
        {
            base.Tick();

            if (counter > Duration)
                Remove();

            p = Mathf.Clamp01((float)counter / Duration);
            if (!FadeIn)
                p = 1f - p;

            currentColor = Color.Lerp(default, Color.white, p);

            counter++;
        }

        public override Color ColorFor(IntVec3 cell)
        {
            return currentColor;
        }
    }
}
