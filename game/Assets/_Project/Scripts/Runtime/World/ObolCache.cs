using UnityEngine;

namespace MyGame
{
    // An Obol cache: a small box of Obols, found behind a secret. An item: it glows softly (see PickupGlow).
    public sealed class ObolCache : MonoBehaviour, IInteractable
    {
        public int obols = 120;
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
            Taken = true;
            apprentice.PickUp(transform, () =>
            {
                Sfx.Instance?.ObolClink(transform.position + Vector3.up * 0.3f);
                apprentice.GainObols(obols);
                apprentice.AnnounceFind("An Obol Cache", $"{obols} Obols");
                gameObject.SetActive(false);
            });
        }
    }
}
