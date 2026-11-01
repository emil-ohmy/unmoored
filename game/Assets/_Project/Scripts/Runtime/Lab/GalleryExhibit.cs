using System;
using UnityEngine;

namespace MyGame
{
    // One creature in the animation gallery (Lab_Gallery): a rigged model on its plinth, no fight in it, only its clips;
    // and its attacks as the game times them (each its wind-up, strike and recovery, the clips stretched to fit: taken
    // from the creature as the scenes build it).
    public sealed class GalleryExhibit : MonoBehaviour
    {
        [Serializable]
        public sealed class Attack
        {
            public string name;
            public string windup, strike, recover;
            public float telegraph, strikeTime, recoverTime;
        }

        public string title;
        public float height = 2f;       // (how tall it stands: the camera's framing)
        public float radius = 1f;
        public Attack[] attacks = new Attack[0];

        Animation animation_;
        string[] clips;

        public Animation Animation => animation_ != null ? animation_ : animation_ = GetComponentInChildren<Animation>();

        // Its clips by short name ("Idle", not "KeenerRig|Idle"), in the order they were made.
        public string[] Clips
        {
            get
            {
                if (clips != null)
                    return clips;
                var list = new System.Collections.Generic.List<string>();
                if (Animation != null)
                    foreach (AnimationState state in Animation)
                        list.Add(Short(state.name));
                return clips = list.ToArray();
            }
        }

        public AnimationState State(string clip)
        {
            if (Animation == null || string.IsNullOrEmpty(clip))
                return null;
            foreach (AnimationState state in Animation)
                if (Short(state.name) == clip)
                    return state;
            return null;
        }

        public static string Short(string name)
        {
            int bar = name.LastIndexOf('|');
            return bar >= 0 ? name.Substring(bar + 1) : name;
        }
    }
}
