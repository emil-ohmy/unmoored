using UnityEngine;

namespace MyGame
{
    // A Healing charge piece: an ember kept in an iron dish, found behind a secret. Taking it adds a Healing charge for
    // good (stats are never bought: they're found).
    public sealed class ChargePiece : MonoBehaviour, IInteractable
    {
        public float interactRadius = 1.4f;
        public bool Taken { get; private set; }

        void OnEnable() => Interactables.Register(this);
        void OnDisable() => Interactables.Unregister(this);

        public Vector3 InteractPosition => transform.position;
        public float InteractRadius => interactRadius;
        public bool Available => !Taken;
        public string PromptLabel => "Take";

        public void Interact(Apprentice apprentice)
        {
            Taken = true;
            Sfx.Instance?.Heal(transform.position + Vector3.up * 0.6f);
            apprentice.AddHealingCharge();
            gameObject.SetActive(false);
        }
    }
}
