using UnityEngine;

namespace MyGame
{
    // An Ember (a Healing charge piece): kept in an iron dish, found behind a secret. Taking it adds a Healing charge for
    // good (stats are never bought: they're found).
    public sealed class ChargePiece : MonoBehaviour, IInteractable
    {
        public float interactRadius = 1.4f;
        public bool Taken { get; private set; }
        public string SaveId { get; private set; }

        void Awake() => SaveId = SaveKey.Of(this);

        void OnEnable() => Interactables.Register(this);
        void OnDisable() => Interactables.Unregister(this);

        public Vector3 InteractPosition => transform.position;
        public float InteractRadius => interactRadius;
        public bool Available => !Taken;
        public string PromptLabel => "Take";

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

        public void Interact(Apprentice apprentice)
        {
            Taken = true; // (no second prompt while the Apprentice stoops for it)
            apprentice.PickUp(transform, () =>
            {
                Sfx.Instance?.Heal(transform.position + Vector3.up * 0.6f);
                apprentice.AddHealingCharge();
                gameObject.SetActive(false);
            });
        }
    }
}
