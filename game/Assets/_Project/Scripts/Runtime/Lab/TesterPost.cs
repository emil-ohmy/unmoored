using UnityEngine;

namespace MyGame
{
    // The lab's Tester's Post: "Test" opens the Tester's menu (spawn any Soured, give the Apprentice anything, reset).
    public sealed class TesterPost : MonoBehaviour, IInteractable
    {
        public LabSpawner spawner;
        public LabReadout readout;
        public float interactRadius = 1.3f;

        void OnEnable() => Interactables.Register(this);
        void OnDisable() => Interactables.Unregister(this);

        public Vector3 InteractPosition => transform.position;
        public float InteractRadius => interactRadius;
        public bool Available => true;
        public string PromptLabel => "Test";
        public void Interact(Apprentice apprentice) => apprentice.ShowMenu(new TesterMenu(this, apprentice));
    }
}
