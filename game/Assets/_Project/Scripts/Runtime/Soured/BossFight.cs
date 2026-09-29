namespace MyGame
{
    // A Boss, as the HUD sees one: its name plate, whether its fight is on, and its health (with the marks where its
    // phases turn).
    public interface IBoss
    {
        string Title { get; }      // the name plate: "The Antediluvian Anchoress"
        string Epithet { get; }    // the rest of her title, for the reveal: "of the Forsaken Crossing"
        bool IsAlive { get; }
        float HealthFraction { get; }
        float[] PhaseMarks { get; } // health fractions where a new phase begins
        int Phase { get; }          // 1, 2...
        UnityEngine.AudioClip[] Music { get; } // its music, one track per phase (sharing one loop, to crossfade in step)
        string Farewell { get; }    // the line when it's beaten: "She Let Go"
    }

    // The Boss fight going on now, if any (the one the HUD shows).
    public static class BossFight
    {
        public static IBoss Current { get; private set; }
        public static event System.Action<IBoss> Defeated; // its farewell (the HUD shows it)

        public static void Defeat(IBoss boss) => Defeated?.Invoke(boss);

        public static void Begin(IBoss boss) => Current = boss;

        public static void End(IBoss boss)
        {
            if (Current == boss)
                Current = null;
        }
    }
}
