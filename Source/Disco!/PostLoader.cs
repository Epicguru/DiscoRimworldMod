using Verse;

namespace Disco
{
    [StaticConstructorOnStartup]
    public static class PostLoader
    {
        static PostLoader()
        {
            Settings.ApplyCustomSongs();
        }
    }
}
