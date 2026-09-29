using UnityEngine;

namespace MyGame
{
    // Something a Lingerer lost, found out in the world (Hesper's son's boat-lantern): an item that glows, taken with the
    // stoop, then carried until it's given to them.
    public sealed class Keepsake : MonoBehaviour, IInteractable
    {
        public string id = "keepsake";
        public string title = "A Keepsake";
        public string hint = "";
        public float interactRadius = 1.4f;

        public bool Taken { get; private set; }
        public string SaveId => id;

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
                apprentice.Equipment.Carry(id);
                apprentice.AnnounceFind(title, hint);
                gameObject.SetActive(false);
            });
        }

        // Loading a save: already taken (carried, or given).
        public void RestoreTaken()
        {
            Taken = true;
            gameObject.SetActive(false);
        }
    }
}
