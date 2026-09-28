using UnityEngine;

namespace MyGame
{
    // The Laden: couldn't let go of things. Unaware, it carries its heap low (face showing) and does NOT block.
    // Once alert it lifts the heap in front of its face, and after guardRaiseTime it blocks hits from the front:
    // the oar bounces off with sparks and the Apprentice is shoved back (mashing is punished). Openings: before it
    // notices you, while the guard is coming up, the wind-up (heap overhead), the recovery (heap on the ground),
    // and its flanks and back.
    public sealed class Laden : Soured
    {
        [Header("Laden")]
        public float blockHalfAngle = 70f;
        public float guardRaiseTime = 0.3f; // matches the GuardUp clip; the block only counts once the heap is up

        float guardSince = float.MaxValue;

        public bool Guarding => CurrentState == State.Chase && Time.time - guardSince >= guardRaiseTime;

        protected override string ChaseClip => "GuardWalk";

        protected override void OnEntered(State state)
        {
            if (state == State.Chase)
            {
                guardSince = Time.time;
                PlayShort("GuardUp", 0.35f);
            }
            else
                guardSince = float.MaxValue;
        }

        protected override bool BlocksHit(Hit hit)
        {
            if (!Guarding)
                return false;
            Vector3 toAttacker = hit.From - transform.position;
            toAttacker.y = 0f;
            return Vector3.Angle(transform.forward, toAttacker) <= blockHalfAngle;
        }
    }
}
