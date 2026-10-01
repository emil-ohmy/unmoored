using UnityEngine;

namespace MyGame
{
    // Provisions lying about (a Pitch Pot or two, a flask of broth): taken with the stoop, into the Satchel. Left where
    // it is if there's no room (each kind has a cap): come back for it.
    public sealed class ProvisionFind : MonoBehaviour, IInteractable
    {
        public string provision = "pitch_pot";
        public int count = 1;
        public float interactRadius = 1.3f;
        public bool Taken { get; private set; }
        public string SaveId { get; private set; }

        public Provision Kind => Provision.ById(provision);

        void Awake() => SaveId = SaveKey.Of(this);

        void OnEnable() => Interactables.Register(this);
        void OnDisable() => Interactables.Unregister(this);

        public Vector3 InteractPosition => transform.position;
        public float InteractRadius => interactRadius;
        public bool Available => !Taken && Kind != null;
        public string PromptLabel => "Take";

        public void Interact(Apprentice apprentice)
        {
            var p = Kind;
            if (apprentice.Equipment.Carried(p) >= p.Cap)
            {
                apprentice.AnnounceFind(p.Name, "No room for more. It'll keep");
                return;
            }
            Taken = true;
            apprentice.PickUp(transform, () =>
            {
                int took = apprentice.Equipment.AddProvision(p, count);
                apprentice.AnnounceFind(count > 1 ? $"{p.Name} ({took})" : p.Name,
                    $"{apprentice.Equipment.Carried(p)} of {p.Cap} in the Satchel");
                gameObject.SetActive(false);
            });
        }

        // (the lab) Where it was again.
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
