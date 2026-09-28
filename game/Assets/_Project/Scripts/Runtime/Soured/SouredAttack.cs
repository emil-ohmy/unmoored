using UnityEngine;

namespace MyGame
{
    // One of a Soured's attacks: an animated wind-up (the telegraph), the strike, and a recovery (the punish window).
    [System.Serializable]
    public sealed class SouredAttack
    {
        public string name = "Slam";
        public string windupClip = "Windup", strikeClip = "Strike", recoverClip = "Recover";
        public float telegraph = 0.9f;
        public float strike = 0.3f;
        public float recover = 1.2f;
        [Range(0f, 1f)] public float impactAt = 0.5f; // fraction of the strike when the blow lands
        public float lunge = 0.5f;                       // lunge distance when the target is already in reach
        public float maxLunge = 2.2f;                    // how far it will lunge after a target that backed off
        [Range(0f, 1f)] public float trackUntil = 0.7f;  // share of the wind-up it keeps turning to follow the target
        public float damage = 25f;
        public float reach = 2.3f;
        [Range(0f, 180f)] public float arc = 60f;     // half-angle around its facing that the blow covers
        public float knockback = 7f;

        public float ImpactTime => telegraph + strike * impactAt; // seconds from the start of the wind-up
    }
}
