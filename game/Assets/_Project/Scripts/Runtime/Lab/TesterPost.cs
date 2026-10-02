using UnityEngine;

namespace MyGame
{
    // The lab's Tester's Post: "Test" opens the Tester's menu (spawn any Soured, give the Apprentice anything, reset).
    public sealed class TesterPost : MonoBehaviour, IInteractable
    {
        public LabSpawner spawner;
        public LabReadout readout;
        public float interactRadius = 1.3f;
        [Tooltip("The Soured placed in this lab's own rows (the Orchard's Gleaners and Moths): asleep (gone) from the start, " +
                 "woken at the post, so whatever's being tried can be tried on its own")]
        public GameObject[] residents = new GameObject[0];

        public bool ResidentsAwake { get; private set; }

        void Start() => SetResidents(false);

        public void SetResidents(bool awake)
        {
            ResidentsAwake = awake;
            foreach (var r in residents)
                if (r != null)
                    r.SetActive(awake);
        }

        void OnEnable() => Interactables.Register(this);
        void OnDisable() => Interactables.Unregister(this);

        public Vector3 InteractPosition => transform.position;
        public float InteractRadius => interactRadius;
        public bool Available => true;
        public string PromptLabel => "Test";
        public void Interact(Apprentice apprentice) => apprentice.ShowMenu(new TesterMenu(this, apprentice));
    }
}
