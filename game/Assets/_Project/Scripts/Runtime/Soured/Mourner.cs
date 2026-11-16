using UnityEngine;

namespace MyGame
{
    // The Mourner (the Hold's hidden way; mourner.py): one of the kept, tall and thin, a veil of tar over its face,
    // carrying something small wrapped in grey against its chest that it never sets down. Harder than the Kept: the
    // Hold's lesson in the perfect block. Two blows, both with the long right arm:
    // - Grieve (up close): clutching the bundle in, it rears (the tell), then three blows a step into each: across, back,
    //   and down from overhead, the heaviest. Perfect-block any one of them and its grief breaks off: stunned, open to a
    //   riposte. Its recovery after all three, bowed over the bundle, is the other opening.
    // - Lunge (the primary; from further off): it sinks low (the tell), then springs across the floor at you, the claws
    //   out. A dodge to the side is the answer.
    public sealed class Mourner : Soured
    {
        [Header("Grieve: three blows (the Lunge is the primary)")]
        public SouredAttack grieve = new SouredAttack
        {
            name = "Grieve", windupClip = "Windup", strikeClip = "Grieve", recoverClip = "GrieveRecover",
            telegraph = 0.8f, strike = 1.6f, recover = 0.9f, impactAt = 0f,
            lunge = 0f, maxLunge = 0f, trackUntil = 0.8f, damage = 11f, reach = 2.1f, arc = 70f, knockback = 4f, scripted = true,
        };
        public float[] blowAt = { 0.24f, 0.72f, 1.26f }; // into the strike (the clip's f8, f22, f38)
        public float lastBlowDamage = 18f, lastBlowKnockback = 9f;
        public float blowStep = 3f;     // the shove forward into each blow (about 0.4 m)
        public float blowTurn = 120f;   // degrees a second it turns after you between blows
        public float lungeFrom = 3.2f;  // further than this (and in reach of its lunge), it springs
        [Range(0f, 1f)] public float lungeChance = 0.7f;

        [Header("Sounds")]
        public AudioClip[] blowSwingSound, hurtSound, deathSound;

        public int GrieveBlows { get; private set; } // (tests) blows of the Grieve struck, all told

        int blowsThisRun;

        protected override SouredAttack ChooseAttack(Vector3 toTarget, float distance)
        {
            if (distance <= grieve.reach)
                return grieve;
            if (distance >= lungeFrom && distance <= primary.maxLunge + primary.reach * 0.6f && Random.value < lungeChance * Time.deltaTime * 4f)
                return primary; // (now and then, not the moment you step out of reach)
            return null;
        }

        protected override void OnEntered(State state)
        {
            if (state == State.Strike)
                blowsThisRun = 0;
            if (state == State.Staggered || state == State.Stunned)
                Sfx.Instance?.Attack(hurtSound, transform.position + Vector3.up * 1.4f, 0.9f, "mourner_hurt");
            if (state == State.Dying)
                Sfx.Instance?.Attack(deathSound, transform.position + Vector3.up, 1f, "mourner_death");
        }

        protected override void Tick(float dt)
        {
            if (CurrentState != State.Strike || CurrentAttack != grieve)
                return;
            if (target != null)
            {
                Vector3 to = target.transform.position - transform.position;
                to.y = 0f;
                if (to.sqrMagnitude > 0.01f)
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(to), blowTurn * dt);
            }
            while (blowsThisRun < blowAt.Length && StateTime >= blowAt[blowsThisRun] && CurrentState == State.Strike)
                Blow(blowsThisRun++);
        }

        void Blow(int k)
        {
            bool last = k == blowAt.Length - 1;
            GrieveBlows++;
            Shove(transform.forward * blowStep);
            Sfx.Instance?.Attack(blowSwingSound, transform.position + Vector3.up * 1.3f, 0.9f, "mourner_swing", last ? 0.8f : 1f + 0.08f * k);
            if (target == null || !target.IsAlive)
                return;
            Vector3 toTarget = target.transform.position - transform.position;
            toTarget.y = 0f;
            if (toTarget.magnitude > grieve.reach + 0.35f || Vector3.Angle(transform.forward, toTarget) > grieve.arc)
                return;
            if (HitTarget(last ? lastBlowDamage : grieve.damage, toTarget.normalized * (last ? lastBlowKnockback : grieve.knockback)) == Defence.Parried)
                Stun(); // (a perfect block: its grief breaks off, and it's open)
        }
    }
}
