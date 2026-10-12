using UnityEngine;

namespace MyGame
{
    // The Hall of the Last Hour's clock: a beat a second, the beat its music keeps (the music's place in its loop, so
    // a blow lands on the music's tick; the loop is a whole number of bars); with no music playing, the game's own
    // time. The Punctual keep to it.
    public static class HallClock
    {
        public const float Beat = 1f;
        public const int BeatsInBar = 4;

        public static double Now
        {
            get
            {
                double music = MusicPlayer.Instance != null ? MusicPlayer.Instance.Clock : -1.0;
                return music >= 0.0 ? music : Time.timeSinceLevelLoadAsDouble;
            }
        }

        // The number of the beat it is now (it starts again with the music's loop: a change, not a count).
        public static long BeatIndex => (long)System.Math.Floor(Now / Beat);
        // How far through the beat it is (0..1).
        public static float Through => (float)(Now / Beat - System.Math.Floor(Now / Beat));
        public static bool BarStarts(long beat) => ((beat % BeatsInBar) + BeatsInBar) % BeatsInBar == 0;
    }
}
