using UnityEngine;

namespace MyGame
{
    // The game runs at 60 frames a second, not as many as the machine can draw: uncapped (vsync off, no target), a
    // laptop renders hundreds of frames a second for no visible gain and runs hot. Set once when play starts (tests
    // and recordings set their own rate afterwards).
    public static class FrameRate
    {
        public const int Target = 60;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Cap()
        {
            QualitySettings.vSyncCount = 0; // (the target below sets the pace; vsync on a 120 Hz display would double it)
            Application.targetFrameRate = Target;
        }
    }
}
