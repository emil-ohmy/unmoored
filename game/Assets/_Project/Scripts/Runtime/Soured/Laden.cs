using UnityEngine;

namespace MyGame
{
    // The Laden: couldn't let go of things. It approaches with its heap carried low on the hip: open. Within
    // guardRange it plants itself, lifts the heap in front of its face (a glint marks the moment the guard is
    // up) and shuffles in behind it; now hits from the front bounce off with sparks and a shove (mashing is
    // punished). Openings: from range, before it notices you, while the guard rises, the wind-up (heap
    // overhead), the recovery (heap on the ground), and its flanks and back.
    public sealed class Laden : Soured
    {
        [Header("Laden guard")]
        public float guardRange = 3.4f;
        public float guardSpeed = 0.8f;       // braced shuffle while guarding
        public float guardRaiseTime = 0.3f;   // matches the GuardUp clip; the block only counts once the heap is up
        public float blockHalfAngle = 70f;

        float guardSince = float.MaxValue;
        bool guardWanted, glinted;

        public bool Guarding => CurrentState == State.Chase && guardWanted && Time.time - guardSince >= guardRaiseTime;

        protected override string ChaseClip(float distance) => guardWanted ? "GuardWalk" : "Walk";
        protected override float ChaseSpeed(float distance) => guardWanted ? guardSpeed : chaseSpeed;

        protected override void OnChaseDistance(float distance)
        {
            bool wanted = distance <= guardRange;
            if (wanted == guardWanted)
            {
                if (Guarding && !glinted)
                {
                    glinted = true; // once per raise: the guard is up now
                    Effects.Instance?.GuardGlint(transform.position + transform.forward * (radius + 0.2f) + Vector3.up * 1.6f);
                }
                return;
            }
            guardWanted = wanted;
            glinted = false;
            if (wanted)
            {
                guardSince = Time.time;
                PlayShort("GuardUp", guardRaiseTime + 0.05f);
            }
            else
                guardSince = float.MaxValue;
        }

        protected override void OnEntered(State state)
        {
            // Every other state drops the guard; coming back to Chase decides again from the distance.
            guardWanted = false;
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
