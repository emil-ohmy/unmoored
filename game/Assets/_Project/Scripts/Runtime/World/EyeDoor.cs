using UnityEngine;

namespace MyGame
{
    // The door of eyes (the Hold's grotto): a great doorway in a built wall, shut by two leaves of the kept's living
    // stone, enormous eyes set in them (WallEye: shut till you come near, then they open one after another and follow
    // you). It won't open: not yet (something for later). Try it, and every eye fixes on you, and it says its piece.
    // This transform: at the doorway's foot, its forward out of the wall.
    public sealed class EyeDoor : MonoBehaviour, IInteractable
    {
        public WallEye[] eyes;
        public string says = "...";
        public float interactRadius = 2.4f;
        public AudioClip[] triedSound;

        public int Tried { get; private set; } // (tests)

        void OnEnable() => Interactables.Register(this);
        void OnDisable() => Interactables.Unregister(this);

        public Vector3 InteractPosition => transform.position + transform.forward * 1.2f;
        public float InteractRadius => interactRadius;
        public bool Available => true;
        public string PromptLabel => "Open";

        public void Interact(Apprentice by)
        {
            Tried++;
            foreach (var eye in eyes)
                if (eye != null)
                    eye.Stare(2.5f);
            Sfx.Instance?.Attack(triedSound, transform.position + Vector3.up * 2.5f, 0.9f, "eye_door");
            by.AnnounceFind(says, "");
        }
    }
}
