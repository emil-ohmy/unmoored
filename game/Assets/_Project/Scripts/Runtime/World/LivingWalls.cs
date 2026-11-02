using UnityEngine;

namespace MyGame
{
    // The Hold's walls, made of what was kept there: they breathe. A slow breath in and out (the Toon shader moves every
    // "..._Breathe" surface along its normal by _WallBreath, -1 out .. 1 in), heard in time with it; ripples run along
    // them in towards the Apprentice (_WallFocus). Outside the Hold nothing sets these, and nothing breathes.
    public sealed class LivingWalls : MonoBehaviour
    {
        public float breathIn = 3.5f, breathOut = 4.5f;
        public float rippleSpeed = 1.6f;          // (the ripples' phase, radians a second: they run in towards you)
        public AudioClip[] inSound, outSound;
        public float volume = 1f; // (quiet already, as made)

        public float Breath { get; private set; } // -1 (let go) .. 1 (drawn in)
        public int Breaths { get; private set; }  // (tests)

        static readonly int BreathId = Shader.PropertyToID("_WallBreath"), FocusId = Shader.PropertyToID("_WallFocus");
        Transform apprentice;
        float t, phase;
        bool drawing = true, started;

        void Start()
        {
            var a = FindAnyObjectByType<Apprentice>();
            apprentice = a != null ? a.transform : null;
        }

        void OnDisable()
        {
            Shader.SetGlobalFloat(BreathId, 0f);
            Shader.SetGlobalVector(FocusId, Vector4.zero);
        }

        void Update()
        {
            if (!started)
            {
                started = true;
                Breathe(drawing);
            }
            t += Time.deltaTime;
            float length = drawing ? breathIn : breathOut;
            if (t >= length)
            {
                t -= length;
                drawing = !drawing;
                Breaths++;
                Breathe(drawing);
                length = drawing ? breathIn : breathOut;
            }
            float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / length));
            Breath = drawing ? Mathf.Lerp(-1f, 1f, u) : Mathf.Lerp(1f, -1f, u);
            phase += rippleSpeed * Time.deltaTime;
            Vector3 at = apprentice != null ? apprentice.position : transform.position;
            Shader.SetGlobalFloat(BreathId, Breath);
            Shader.SetGlobalVector(FocusId, new Vector4(at.x, at.y, at.z, phase));
        }

        // Heard all round (it's the place itself): played where the Apprentice is.
        void Breathe(bool inward)
        {
            Vector3 at = apprentice != null ? apprentice.position + Vector3.up * 3f : transform.position;
            Sfx.Instance?.Attack(inward ? inSound : outSound, at, volume, inward ? "hold_breath_in" : "hold_breath_out");
        }
    }
}
