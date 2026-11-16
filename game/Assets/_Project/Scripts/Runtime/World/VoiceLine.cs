using UnityEngine;

namespace MyGame
{
    // The voice in the dark (the opening's: "Apprentice. Take light."), teaching the Hold: come within `radius` of where
    // it waits and it speaks, once, as you walk: its line (what matters in it *marked*, in the Info colour) and the dry
    // line under it with the controls ({dodge}, {heal}...: Hud.Controls), low on the screen like subtitles. Nothing to
    // press, no stopping. (The messages on the floor were the genre's; one is left, for old times' sake: FloorMessage.)
    public sealed class VoiceLine : MonoBehaviour
    {
        [TextArea] public string line = "*Dodge*, Apprentice.";
        [TextArea] public string hint = "";
        public float radius = 3.5f;
        public AudioClip voice;     // a whisper as it speaks

        public bool Spoken { get; private set; } // (tests)

        Apprentice apprentice;

        void Start() => apprentice = FindAnyObjectByType<Apprentice>();

        void Update()
        {
            if (Spoken || apprentice == null || !apprentice.IsAlive || apprentice.CurrentState == Apprentice.State.Waking)
                return;
            Vector3 d = apprentice.transform.position - transform.position;
            d.y = 0f;
            if (d.sqrMagnitude > radius * radius)
                return;
            Spoken = true;
            FindAnyObjectByType<Hud>()?.Speak(line, hint);
            if (voice != null)
                AudioSource.PlayClipAtPoint(voice, apprentice.transform.position + Vector3.up, 0.6f);
        }

        void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.91f, 0.76f, 0.44f, 0.6f);
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}
