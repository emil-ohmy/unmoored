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

        // Every clip played this much faster or slower (a Wound running down: everything about it slows).
        public float Tempo { get; private set; } = 1f;
        float baseSpeed = 1f; // (the clip now playing: its own speed, before the tempo)

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
            baseSpeed = 1f;
            state.speed = Tempo;
            animation.CrossFade(full, fade);
            Current = clip;
        }

        // Speed of the looping clip now playing (e.g. the run cycle kept in step with the actual speed).
        public void SetLoopSpeed(float speed)
        {
            if (fullNames.TryGetValue(Current, out var full) && animation[full].wrapMode == WrapMode.Loop)
            {
                baseSpeed = speed;
                animation[full].speed = speed * Tempo;
            }
        }

        public void SetTempo(float tempo)
        {
            Tempo = tempo;
            if (fullNames.TryGetValue(Current, out var full))
                animation[full].speed = baseSpeed * tempo;
        }

        // (from: where in the clip to start, 0..1: a swing picked up partway)
        public void PlayOnce(string clip, float duration, float fade = 0.05f, float from = 0f)
        {
            if (!fullNames.TryGetValue(clip, out var full))
                return;
            var state = animation[full];
            state.wrapMode = WrapMode.ClampForever;
            baseSpeed = duration > 0f ? state.length / duration : 1f;
            state.speed = baseSpeed * Tempo;
            state.time = from * state.length;
            if (Current == clip)
                animation.Play(full);
            else
                animation.CrossFade(full, fade);
            Current = clip;
        }
    }
}
