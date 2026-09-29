using UnityEngine;

namespace MyGame
{
    // The Laden: couldn't let go of things. Two attacks: an overhead slam along a line (dash sideways; long
    // recovery) and a wide, low sweep (back off or dash through; short recovery). It approaches with its heap
    // carried low on the hip: open. Within
    // guardRange it plants itself, lifts the heap in front of its face (a glint marks the moment the guard is
    // up) and shuffles in behind it; now hits from the front bounce off with sparks and a shove (mashing is
    // punished). Openings: from range, before it notices you, while the guard rises, the wind-up (heap
    // overhead), the recovery (heap on the ground), and its flanks and back.
    public class Laden : Soured
    {
        [Header("Laden sweep (the second attack)")]
        public SouredAttack sweep = new SouredAttack
        {
            name = "Sweep", windupClip = "SweepWindup", strikeClip = "Sweep", recoverClip = "SweepRecover",
            telegraph = 0.6f, strike = 0.25f, recover = 0.7f, impactAt = 0.5f,
            lunge = 0.15f, maxLunge = 1.2f, trackUntil = 0.9f, damage = 22f, reach = 2.5f, arc = 150f, knockback = 8f,
        };
        [Range(0f, 1f)] public float sweepChanceInFront = 0.3f; // from the front it mixes slams and sweeps
        public float flankAngle = 50f; // beyond this the Apprentice is at its side or back: always sweep

        [Header("Laden guard")]
        public float guardRange = 3.4f;
        public float guardSpeed = 0.8f;       // braced shuffle while guarding
        public float guardRaiseTime = 0.3f;   // matches the GuardUp clip; the block only counts once the heap is up
        public float blockHalfAngle = 70f;

        [Header("The Hook: too heavy to pull, it tears the heap from its grip")]
        public float ripGuardTime = 6f; // no guard for this long
        public float ripStagger = 1.4f;

        float guardSince = float.MaxValue, guardBrokenUntil = -10f;
        bool guardWanted, glinted;

        public bool Guarding => CurrentState == State.Chase && guardWanted && !GuardBroken && Time.time - guardSince >= guardRaiseTime;
        public bool GuardBroken => Time.time < guardBrokenUntil;

        public override HookResult OnHooked(Apprentice by)
        {
            if (!IsAlive || CurrentState == State.Busy)
                return HookResult.Shrugged;
            Rip(by, ripGuardTime, ripStagger);
            return HookResult.Ripped;
        }

        // The Hook tears at its heap: it staggers, junk flying off it, and it can't raise its guard for a while.
        protected void Rip(Apprentice by, float guardTime, float stagger)
        {
            guardBrokenUntil = Time.time + guardTime;
            guardWanted = false;
            StaggerNow(stagger);
            Vector3 towards = by.transform.position - transform.position;
            towards.y = 0f;
            Vector3 at = transform.position + towards.normalized * (radius + 0.2f) + Vector3.up * 1.1f;
            DebrisBurst.Throw(at, towards.normalized, HeapMaterials(), 14);
            Sfx.Instance?.BlockClang(at);
            CameraRig.Instance?.Shake(0.25f);
            HitStop.Trigger(0.08f);
        }

        Material[] heap;

        // What its heap is made of (its junk's materials, for the pieces that fly off).
        Material[] HeapMaterials()
        {
            if (heap != null)
                return heap;
            var list = new System.Collections.Generic.List<Material>();
            foreach (var r in GetComponentsInChildren<Renderer>())
                foreach (var m in r.sharedMaterials)
                    if (m != null && (m.name.StartsWith("Heap") || m.name.StartsWith("Frame") || m.name.StartsWith("Clock") || m.name.StartsWith("Anchor")))
                        list.Add(m);
            return heap = list.ToArray();
        }

        protected override string ChaseClip(float distance) => guardWanted ? "GuardWalk" : "Walk";
        protected override float ChaseSpeed(float distance) => guardWanted ? guardSpeed : chaseSpeed;

        protected override void OnChaseDistance(float distance)
        {
            bool wanted = distance <= guardRange && !GuardBroken;
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

        // Slam straight ahead (long punish window); sweep anyone at its flank or back, and sometimes in front, so
        // circling it isn't a free win and a slam can't be assumed (short punish window).
        protected override SouredAttack ChooseAttack(Vector3 toTarget, float distance)
        {
            if (distance <= sweep.reach && Vector3.Angle(transform.forward, toTarget) > flankAngle)
                return sweep;
            if (distance <= attackRange)
                return Random.value < sweepChanceInFront ? sweep : primary;
            return null;
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
