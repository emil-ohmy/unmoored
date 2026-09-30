namespace MyGame
{
    // The bow lantern's level, 1 to 5 (the character screen's last page): how many of the whole journey's Mooring
    // posts have been lit. It asks nothing and says little. Fully lit, it is the key to the secret last Boss and ending
    // (docs/brief.md, Progression; not built yet). The game is one scene for now, so the count is the scene's.
    public static class Lantern
    {
        public const int Levels = 5;
        public const int PlannedPosts = 12; // the whole game's Mooring posts, as planned (docs/brief.md)
        public static int DevLevels;        // (the lab) levels given at the Tester's Post; none in a scene just loaded

        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Clear()
        {
            DevLevels = 0;
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= Loaded;
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += Loaded;
        }

        static void Loaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode) => DevLevels = 0;

        public static readonly string[] Lines =
        {
            "A small, cold light.",
            "It burns a little higher.",
            "It remembers every light you've lit.",
            "It is waiting for something.",
            "It's ready.",
        };

        // 0..1: the posts lit (and the lab's levels) against all of them.
        public static float Progress =>
            UnityEngine.Mathf.Clamp01((MooringPost.LitCount + DevLevels * PlannedPosts / (float)(Levels - 1)) / PlannedPosts);

        public static int Level => Progress >= 1f ? Levels : 1 + UnityEngine.Mathf.Min(Levels - 2, (int)(Progress * (Levels - 1)));

        // 0..1: how far towards the next level (its flame, faintly kindling).
        public static float TowardNext => Level >= Levels ? 1f : Progress * (Levels - 1) - (Level - 1);

        public static string Line => Lines[Level - 1];
    }
}
