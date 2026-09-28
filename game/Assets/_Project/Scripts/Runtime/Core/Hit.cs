using UnityEngine;

namespace MyGame
{
    // One hit from the Apprentice's oar.
    public struct Hit
    {
        public float Damage;
        public float Stagger; // how much it fills the target's Stagger meter
        public Vector3 Knockback;
        public Vector3 From; // attacker position (for frontal blocks)
        public bool Heavy;
    }

    public enum HitResult { Missed, Landed, Blocked }

    // What happened when a Soured's blow reached the Apprentice.
    public enum Defence { Dodged, Taken, Blocked, Parried }
}
