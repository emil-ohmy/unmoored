using UnityEngine;

namespace MyGame
{
    // Something found that makes the Apprentice stronger for good (stats are never bought: docs/brief.md): a Ferry
    // Nail (the oar, strengthened at a Mooring post), a Warm Stone (health) or a Held Breath (stamina). An item: it
    // glows where it lies; taken with the stoop.
    public sealed class UpgradePiece : MonoBehaviour, IInteractable
    {
        public enum Kind { FerryNail, WarmStone, HeldBreath }

        public Kind kind;
        public float interactRadius = 1.3f;
        public bool Taken { get; private set; }
        public string SaveId { get; private set; }

        void Awake() => SaveId = SaveKey.Of(this);

        void OnEnable() => Interactables.Register(this);
        void OnDisable() => Interactables.Unregister(this);

        public Vector3 InteractPosition => transform.position;
        public float InteractRadius => interactRadius;
        public bool Available => !Taken;
        public string PromptLabel => "Take";

        public void Interact(Apprentice apprentice)
        {
            Taken = true;
            apprentice.PickUp(transform, () =>
            {
                switch (kind)
                {
                    case Kind.FerryNail: apprentice.AddFerryNail(); break;
                    case Kind.WarmStone: apprentice.AddWarmStone(); break;
                    default: apprentice.AddHeldBreath(); break;
                }
                gameObject.SetActive(false);
            });
        }

        // (the lab) Where it was again, to be found again.
        public void PutBack()
        {
            Taken = false;
            gameObject.SetActive(true);
        }

        // Loading a save: already taken.
        public void RestoreTaken()
        {
            Taken = true;
            gameObject.SetActive(false);
        }
    }
}
