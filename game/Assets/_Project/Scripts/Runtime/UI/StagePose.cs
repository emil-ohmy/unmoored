using UnityEngine;

namespace MyGame
{
    // Holds a model on a page's stage (the Ledger's) in one pose: a clip of its own, at a point in it (the Unparted's
    // Fast, clinging to Hold's back; the rest standing as they idle). Set each time it's shown.
    public sealed class StagePose : MonoBehaviour
    {
        public string clip = "Idle";
        [Range(0f, 1f)] public float at = 0f;

        void OnEnable()
        {
            var anim = GetComponentInChildren<Animation>();
            if (anim == null)
                return;
            foreach (AnimationState state in anim)
            {
                int bar = state.name.LastIndexOf('|');
                if ((bar >= 0 ? state.name.Substring(bar + 1) : state.name) != clip)
                    continue;
                anim.Play(state.name);
                state.normalizedTime = at;
                state.speed = 0f; // (held: the page is paused anyway)
                anim.Sample();
                return;
            }
        }
    }
}
