using UnityEngine;

namespace MyGame
{
    // The Laden: couldn't let go of things. Its heap of belongings blocks hits from the front while it walks or
    // waits (the hit bounces and the Apprentice recoils: mashing is punished). The heap is lifted during the
    // wind-up and lies on the ground during recovery, so those, and its flanks and back, are the openings.
    public sealed class Laden : Soured
    {
        [Header("Laden")]
        public float blockHalfAngle = 70f;

        protected override bool BlocksHit(Hit hit)
        {
            if (CurrentState != State.Idle && CurrentState != State.Chase)
                return false;
            Vector3 toAttacker = hit.From - transform.position;
            toAttacker.y = 0f;
            return Vector3.Angle(transform.forward, toAttacker) <= blockHalfAngle;
        }
    }
}
