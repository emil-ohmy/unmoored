using UnityEngine;

namespace MyGame
{
    // A message scrawled on the floor, faintly glowing: the genre's own way of teaching (the game's dry voice, not the
    // world's: docs/brief.md, Tone and voice). Stand on it and Read: its line comes up like a find, the controls in it
    // shown for the device in use ({dodge}, {light}, {heal}, {interact}...: Hud). It glows a little brighter while
    // you're near, and dims once it's been read.
    public sealed class FloorMessage : MonoBehaviour, IInteractable
    {
        [TextArea] public string message = "Try dodging";   // (the genre's template: "Try X", "Be wary of X", "X ahead")
        [TextArea] public string line = "";                 // the dry line under it, with the controls
        public float interactRadius = 1.4f;
        public Renderer glow;                               // (MyGame/Particle: its brightness is _Intensity)
        public float brightness = 1.1f;

        public int Read { get; private set; } // (tests)

        Apprentice apprentice;
        MaterialPropertyBlock block;
        float shown;

        void OnEnable() => Interactables.Register(this);
        void OnDisable() => Interactables.Unregister(this);

        void Start()
        {
            apprentice = FindAnyObjectByType<Apprentice>();
            block = new MaterialPropertyBlock();
        }

        public Vector3 InteractPosition => transform.position;
        public float InteractRadius => interactRadius;
        public bool Available => true;
        public string PromptLabel => "Read the message";

        public void Interact(Apprentice by)
        {
            Read++;
            by.AnnounceFind(message, line);
        }

        void Update()
        {
            if (glow == null)
                return;
            bool near = apprentice != null && (apprentice.transform.position - transform.position).sqrMagnitude < 9f;
            float want = (near ? 1f : 0.55f) * (Read > 0 ? 0.6f : 1f) * (0.9f + 0.1f * Mathf.Sin(Time.time * 1.7f + transform.position.x));
            shown = Mathf.MoveTowards(shown, want, Time.deltaTime * 1.5f);
            glow.GetPropertyBlock(block);
            block.SetFloat("_Intensity", brightness * shown);
            glow.SetPropertyBlock(block);
        }
    }
}
