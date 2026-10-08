using UnityEngine;

namespace MyGame
{
    // A dock's bell. With the boat tied up somewhere else (and nobody in it), ring it, and the Ferryman's boat comes
    // gliding in out of the water to this dock (Boat.Summon).
    public sealed class DockBell : MonoBehaviour, IInteractable
    {
        public Dock dock;
        public Boat boat;
        public float interactRadius = 1.5f;
        public AudioClip[] ringSound;

        public int Rung { get; private set; } // (tests)

        void OnEnable() => Interactables.Register(this);
        void OnDisable() => Interactables.Unregister(this);

        public Vector3 InteractPosition => transform.position;
        public float InteractRadius => interactRadius;
        public bool Available => boat != null && dock != null && !boat.Rowing && boat.MooredAt != dock;
        public string PromptLabel => "Ring for the boat";

        public void Interact(Apprentice apprentice)
        {
            Rung++;
            Sfx.Instance?.Attack(ringSound, transform.position + Vector3.up * 1.5f, 1f, "dock_bell");
            boat.Summon(dock);
        }
    }
}
