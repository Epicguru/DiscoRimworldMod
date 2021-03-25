using Verse;

namespace Disco
{
    public class Settings : ModSettings
    {
        public static float FinalMusicVolume
        {
            get
            {
                if (UseCustomVolume)
                    return CustomMusicVolume;
                return Prefs.VolumeMusic;
            }
        }

        // DISCO
        [TweakValue("_Disco!")]
        public static bool UseCustomVolume = true;
        [TweakValue("_Disco!", 0, 1)]
        public static float CustomMusicVolume = 0.65f;
        [TweakValue("_Disco!", 100, 32768)]
        public static int DiscoMaxFloorSize = 5000;
        [TweakValue("_Disco!", 0, 1)]
        public static float DiscoFloorColorIntensity = 0.65f;
    }
}
