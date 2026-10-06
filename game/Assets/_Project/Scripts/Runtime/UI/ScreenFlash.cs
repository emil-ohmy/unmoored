namespace MyGame
{
    // A flash of the whole screen (Father Moth's release: blinding white, then fading): the HUD draws it.
    public static class ScreenFlash
    {
        public static event System.Action<UnityEngine.Color, float, float> Flashed; // colour, held at full, faded over

        public static void Flash(UnityEngine.Color colour, float hold, float fade) => Flashed?.Invoke(colour, hold, fade);
    }
}
