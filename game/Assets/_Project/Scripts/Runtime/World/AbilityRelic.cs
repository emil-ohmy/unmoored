using UnityEngine;

namespace MyGame
{
    // Where an Ability is taken: in the slice, the Ferryman's old hook on a pedestal at the crossroads, standing in for
    // the Boss that will one day give it. Sealed until its guardians have all been defeated (at least once); then
    // Take gives the Hook for good.
    public sealed class AbilityRelic : MonoBehaviour, IInteractable
    {
        public Soured[] guardians;
        public GameObject relic;       // hidden once taken
        public PickupGlow glow;        // a key item: faint while sealed, bright once it can be taken
        public float interactRadius = 1.6f;

        public bool Taken { get; private set; }
        public bool Unsealed
        {
            get
            {
                foreach (var g in guardians)
                    if (g != null && g.TimesDefeated == 0)
                        return false;
                return true;
            }
        }

        void OnEnable() => Interactables.Register(this);
        void OnDisable() => Interactables.Unregister(this);

        public Vector3 InteractPosition => transform.position;
        public float InteractRadius => interactRadius;
        public bool Available => !Taken;
        public string PromptLabel => Unsealed ? "Take" : "Sealed";

        public void Interact(Apprentice apprentice)
        {
            if (!Unsealed)
            {
                Sfx.Instance?.GateRattle(transform.position + Vector3.up);
                return;
            }
            Taken = true;
            if (relic != null)
                relic.SetActive(false);
            if (glow != null)
                glow.gameObject.SetActive(false);
            apprentice.ReceiveKeyItem("The Hook", Apprentice.HookHint, apprentice.GiveHook); // held up, Zelda-ish
        }

        void Update()
        {
            if (glow != null && !Taken)
                glow.Lit = Unsealed;
        }
    }
}
