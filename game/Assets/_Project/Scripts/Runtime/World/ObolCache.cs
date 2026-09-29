using UnityEngine;

namespace MyGame
{
    // An Obol cache: a small box of Obols, found behind a secret. An item: it glows softly (see PickupGlow).
    public sealed class ObolCache : MonoBehaviour, IInteractable
    {
        public int obols = 120;
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
