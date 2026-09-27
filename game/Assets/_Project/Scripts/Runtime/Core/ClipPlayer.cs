using System.Collections.Generic;
using UnityEngine;

namespace MyGame
{
    // Plays a character's Legacy clips by short name ("Run", not "ApprenticeRig|Run"), either looping or
    // once over a given duration (the clip is sped up or slowed down to fit gameplay timing).
    public sealed class ClipPlayer
    {
        readonly Animation animation;
        readonly Dictionary<string, string> fullNames = new Dictionary<string, string>();

        public string Current { get; private set; } = "";

        public ClipPlayer(Animation animation)
        {
            this.animation = animation;
            foreach (AnimationState state in animation)
            {
                int bar = state.name.LastIndexOf('|');
                fullNames[bar >= 0 ? state.name.Substring(bar + 1) : state.name] = state.name;
            }
        }

        public bool Has(string clip) => fullNames.ContainsKey(clip);

        public void Loop(string clip, float fade = 0.15f)
        {
            if (Current == clip || !fullNames.TryGetValue(clip, out var full))
                return;
            var state = animation[full];
            state.wrapMode = WrapMode.Loop;
            state.speed = 1f;
            animation.CrossFade(full, fade);
            Current = clip;
        }

        public void PlayOnce(string clip, float duration, float fade = 0.05f)
        {
            if (!fullNames.TryGetValue(clip, out var full))
                return;
            var state = animation[full];
            state.wrapMode = WrapMode.ClampForever;
            state.speed = duration > 0f ? state.length / duration : 1f;
            state.time = 0f;
            if (Current == clip)
                animation.Play(full);
            else
                animation.CrossFade(full, fade);
            Current = clip;
        }
    }
}
