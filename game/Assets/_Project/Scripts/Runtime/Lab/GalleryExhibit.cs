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
            public string attack;       // (its name on the creature: which swipe trails draw it)
            public bool ranged;         // (a shot: no swipe)
        }

        // One step of its chain: a clip played as the fight plays it (`loop`: looping for its time; else once, stretched
        // over its time), crossfaded in over `fade` (the game's: 0.15 s into a loop, 0.05 s into a one-off).
        [Serializable]
        public sealed class Step
        {
            public string clip;
            public float duration;
            public bool loop;
            public float fade;
            public string note;         // (what's happening: "notices you", "the slam")
            public string swipe;        // (a blow in reach: the attack whose swipe trails draw through it)
        }

        public string title;
        public float height = 2f;       // (how tall it stands: the camera's framing)
        public float radius = 1f;
        public Attack[] attacks = new Attack[0];
        public Step[] chain = new Step[0]; // its clips one after another as a fight chains them

        SwingTrail[] trails;
        public SwingTrail[] Trails => trails ??= GetComponents<SwingTrail>(); // (its swipes, as a fight draws them)

        // Its swipe trails drawing (as Soured's: through the first nine tenths of a blow in reach) or not, timed by `clock`.
        public void Swipe(string attack, bool on, System.Func<float> clock)
        {
            foreach (var trail in Trails)
            {
                trail.Clock = clock;
                trail.Emitting = on && attack != null && trail.For(attack);
            }
        }

        // Its pose, `clip` at `time`: every bone's local rotation and position.
        public (Quaternion[] rotations, Vector3[] positions) PoseAt(string clip, float time)
        {
            var bones = Bones;
            var state = State(clip);
            if (state == null || bones.Length == 0)
                return (new Quaternion[0], new Vector3[0]);
            foreach (AnimationState s in Animation)
            {
                s.enabled = false;
                s.weight = 0f;
            }
            state.enabled = true;
            state.weight = 1f;
            state.time = time;
            Animation.Sample();
            state.enabled = false;
            var r = new Quaternion[bones.Length];
            var p = new Vector3[bones.Length];
            for (int i = 0; i < bones.Length; i++)
            {
                r[i] = bones[i].localRotation;
                p[i] = bones[i].localPosition;
            }
            return (r, p);
        }

        // How far it jumps between two poses: the bone that turns most (degrees) and the furthest any bone shifts (m).
        public (float degrees, string bone, float shift) Jump(string fromClip, float fromTime, string toClip, float toTime)
        {
            var a = PoseAt(fromClip, fromTime);
            var b = PoseAt(toClip, toTime);
            var bones = Bones;
            float most = 0f, shift = 0f;
            string which = "";
            for (int i = 0; i < Mathf.Min(a.rotations.Length, b.rotations.Length); i++)
            {
                float angle = Quaternion.Angle(a.rotations[i], b.rotations[i]);
                if (angle > most)
                {
                    most = angle;
                    which = bones[i].name;
                }
                shift = Mathf.Max(shift, (a.positions[i] - b.positions[i]).magnitude);
            }
            return (most, which, shift);
        }

        // Whether a clip loops (its last pose its first: seamless).
        public bool Loops(string clip)
        {
            var s = State(clip);
            return s != null && Jump(clip, 0f, clip, s.length).degrees < 1.5f;
        }

        Transform[] bones;
        public Transform[] Bones
        {
            get
            {
                if (bones != null)
                    return bones;
                var skin = GetComponentInChildren<SkinnedMeshRenderer>();
                return bones = skin != null ? skin.bones : new Transform[0];
            }
        }

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
