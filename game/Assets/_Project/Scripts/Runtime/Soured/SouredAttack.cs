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
        public bool ranged;                 // a shot: at the impact moment it looses a projectile instead of striking
        public float projectileSpeed = 12f;
        public int volley = 1;              // projectiles per ranged attack, loosed one after another
        public float volleyInterval = 0.2f;
        public float volleyTrack = 6f;      // degrees each later projectile may turn towards the target
        public AudioClip[] releaseSound;    // per projectile loosed
        public bool scripted;               // the subclass lands this blow itself (no impact at impactAt)

        [Header("Sounds: the tell (the wind-up), the swing (every strike) and the impact (when it lands, hit or miss)")]
        public AudioClip[] windupSound;
        public AudioClip[] swingSound;
        public AudioClip[] impactSound; // for blows that hit the ground (a slam); none for a sweep through the air
        public float swingPitch = 1f;

        public float ImpactTime => telegraph + strike * impactAt; // seconds from the start of the wind-up

        public SouredAttack Copy() => (SouredAttack)MemberwiseClone(); // (a variant: shares the sound arrays)
    }
}
