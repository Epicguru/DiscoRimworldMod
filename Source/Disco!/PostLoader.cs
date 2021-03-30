using Verse;

namespace Disco
{
    [StaticConstructorOnStartup]
    public static class PostLoader
    {
        static PostLoader()
        {
            Core.Instance.GetSettings<Settings>();
            Settings.ApplyCustomSongs();
        }
    }
}
