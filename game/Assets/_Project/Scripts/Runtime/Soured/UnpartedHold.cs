using UnityEngine;

namespace MyGame
{
    // Hold, the greater half of the Unparted: vast and slow, with Fast riding its back. Two blows: a wide sweep of its
    // long arm (a groan as it twists away: step back, or under), and a slam, both arms brought down in front (rearing up:
    // step aside). Too heavy for the Hook, but hooking it with Fast on its back pulls Fast off. Fast scattered first,
    // Hold howls and goes berserk: quicker to strike, quicker to recover, faster on its feet.
    public sealed class UnpartedHold : Soured
    {
        [Header("Hold: the sweep (the slam is the primary)")]
        public SouredAttack sweep = new SouredAttack
        {
            name = "Sweep", windupClip = "SweepWindup", strikeClip = "Sweep", recoverClip = "SweepRecover",
            telegraph = 0.9f, strike = 0.3f, recover = 0.9f, impactAt = 0.55f,
            lunge = 0.4f, maxLunge = 1.2f, trackUntil = 0.7f, damage = 20f, reach = 3.1f, arc = 110f, knockback = 9f,
        };
        public Clinger fast;
        [Tooltip("Berserk (Fast gone): its wind-ups and recoveries take this share of the time")]
        public float berserkPace = 0.7f;
        public float berserkSpeed = 1.45f;
        public AudioClip[] howlSound;

        public bool Berserk { get; private set; }

        SouredAttack sweepBerserk, slamBerserk;

        protected override string ChaseClip(float distance) => "Walk";
        protected override float ChaseSpeed(float distance) => chaseSpeed * (Berserk ? berserkSpeed : 1f);

        protected override SouredAttack ChooseAttack(Vector3 toTarget, float distance)
        {
            if (distance > attackRange)
                return null;
            bool wide = distance < 2.4f && Random.value < 0.6f; // close in: mostly the sweep
            if (!Berserk)
                return wide ? sweep : primary;
            sweepBerserk ??= Quicker(sweep);
            slamBerserk ??= Quicker(primary);
            return wide ? sweepBerserk : slamBerserk;
        }

        SouredAttack Quicker(SouredAttack attack)
        {
            var copy = attack.Copy();
            copy.telegraph *= berserkPace;
            copy.recover *= berserkPace * 0.85f;
            return copy;
        }

        protected override void Tick(float dt)
        {
            if (!Berserk && fast != null && !fast.IsAlive && IsAlive && CurrentState != State.Dying)
            {
                Berserk = true; // Fast is gone
                BeBusy("Howl", 1.2f);
                Sfx.Instance?.Attack(howlSound, transform.position + Vector3.up * 1.6f, 1f, "unparted_howl");
                CameraRig.Instance?.Shake(0.25f);
            }
        }

        // Too heavy to pull; but Fast, on its back, comes off.
        public override HookResult OnHooked(Apprentice by) =>
            fast != null && fast.Mounted ? fast.OnHooked(by) : HookResult.Shrugged;

        public override void Reform()
        {
            Berserk = false;
            base.Reform();
        }
    }
}
