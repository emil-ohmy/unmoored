using UnityEngine;

namespace MyGame
{
    // A scarecrow in the Orchard's top rows: straw in an old coat on a stake, a sack for a face, keeping watch over
    // nothing. One among them isn't one (the Watchman). Struck, burnt or hooked, it bursts into straw, and the Moths
    // nesting in it come out at the Apprentice. It never moves, never notices anyone; it's whole again after a Rest.
    public sealed class Scarecrow : Soured
    {
        [Header("The scarecrow")]
        public Transform pole;   // its stake (it stands where the scarecrow last stood, after it's burst)
        public MothFlock nest;   // the Moths in its straw (none: only straw)
        public static event System.Action<Scarecrow> Burst; // (tests)

        protected override void Awake()
        {
            base.Awake();
            passive = true;
        }

        // Caught on the Hook: it comes apart.
        public override HookResult OnHooked(Apprentice by)
        {
            Slay(by.transform.position);
            return HookResult.Shrugged;
        }

        protected override void OnEntered(State state)
        {
            if (state != State.Dying)
                return;
            Effects.Instance?.Straw(transform.position + Vector3.up * 1.2f, 70);
            if (nest != null)
                nest.Release(transform.position + Vector3.up * 1.6f);
            Burst?.Invoke(this);
        }

        public override void Reform()
        {
            base.Reform();
            StandPole(pole, transform);
            if (nest != null)
                nest.Withdraw();
        }

        // A stake under a scarecrow (or the Watchman, while he's still one of them).
        public static void StandPole(Transform pole, Transform figure)
        {
            if (pole != null)
                pole.position = new Vector3(figure.position.x, pole.position.y, figure.position.z);
        }
    }
}
