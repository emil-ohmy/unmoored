using UnityEngine;

namespace MyGame
{
    // Somewhere the boat can put in: where it moors (its pose on the water) and where the Apprentice steps ashore.
    // While rowing, a dock in reach offers "Step ashore" (the boat's, not a thing in the world: never registered).
    public sealed class Dock : MonoBehaviour, IInteractable
    {
        public string displayName = "the landing";
        public Transform mooring; // the boat's pose, tied up here
        public Transform ashore;  // where the Apprentice steps off to
        public float reach = 3f;  // how close the boat must come
        public bool farShore;     // where the dead go on (a Lingerer's Crossing ends here)

        public Vector3 InteractPosition => mooring.position;
        public float InteractRadius => reach;
        public bool Available => true;
        public string PromptLabel => "Step ashore";
        public void Interact(Apprentice apprentice) => apprentice.StepAshore(this);
    }
}
