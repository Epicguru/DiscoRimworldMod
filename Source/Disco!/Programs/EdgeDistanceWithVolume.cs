namespace Disco.Programs
{
    public class EdgeDistanceWithVolume : EdgeDistance
    {
        public float Amplitude;

        public EdgeDistanceWithVolume(ProgramDef def) : base(def)
        {
        }

        public override void Init()
        {
            base.Init();

            Amplitude = Def.Get("jumpAmplitude", 4);
        }

        protected override float PostProcessSolidDistance()
        {
            return base.PostProcessSolidDistance() + DJStand.CurrentSongAmplitude * Amplitude;
        }
    }
}
