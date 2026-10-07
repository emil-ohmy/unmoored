using UnityEngine;

namespace MyGame
{
    // The oar's look follows its step (Oar.cs): the model carries all five oars (tools/blender/oars.py), each step's
    // materials named for it ("Oar2Iron_Metal"), and this shows only the step the oar is at (the others dissolved away
    // completely). Anything else baked from the body (dash ghosts, the Shade) drops the other steps too: HideOthers.
    // Over it: Ember Tar's smoulder, and with the Stroke, the Drowned Moon's restless silver sheen on the blade.
    [RequireComponent(typeof(Apprentice))]
    public sealed class OarLook : MonoBehaviour
    {
        Apprentice apprentice;
        SkinnedMeshRenderer body;
        MaterialPropertyBlock hidden, burning;
        bool glowing, hadStroke;
        float strokeSince = -10f;
        int[] slotSteps;  // each material slot's step (-1: not the oar's)
        bool[] blades;    // ...and whether it's the blade's

        // The sheen's brightness now (0 without the Stroke): tests, and its flare as it sets in.
        public float Sheen { get; private set; }

        public int ShownStep { get; private set; } = -1;

        // The step a material belongs to ("Oar3Silver_Metal": 3), or -1.
        public static int StepOf(Material material)
        {
            if (material == null)
                return -1;
            string n = material.name;
            return n.Length > 3 && n.StartsWith("Oar") && char.IsDigit(n[3]) ? n[3] - '0' : -1;
        }

        void Awake()
        {
            apprentice = GetComponent<Apprentice>();
            body = GetComponentInChildren<SkinnedMeshRenderer>();
            hidden = new MaterialPropertyBlock();
            hidden.SetFloat("_Dissolve", 1.5f); // past the noise's top: nothing left
            burning = new MaterialPropertyBlock();
            if (body == null)
                return;
            var materials = body.sharedMaterials;
            slotSteps = new int[materials.Length];
            blades = new bool[materials.Length];
            for (int i = 0; i < materials.Length; i++)
            {
                slotSteps[i] = StepOf(materials[i]);
                blades[i] = slotSteps[i] >= 0 && materials[i].name.Contains("Blade");
            }
        }

        void Start()
        {
            if (apprentice.Equipment != null)
                apprentice.Equipment.Changed += Show;
            Show();
        }

        void OnDestroy()
        {
            if (apprentice != null && apprentice.Equipment != null)
                apprentice.Equipment.Changed -= Show;
        }

        // Ember Tar: the oar it's on smoulders, a warm flicker in the wood and the metal. The Stroke: the blade carries
        // the Drowned Moon, a cold silver sheen that never settles (two slow waves through it, like light on moving
        // water), flaring as it first sets in.
        void Update()
        {
            if (body == null || slotSteps == null)
                return;
            bool tarred = apprentice.Tarred, stroke = apprentice.HasStroke;
            if (stroke && !hadStroke)
                strokeSince = Time.timeSinceLevelLoad < 1f ? -10f : Time.time; // (not a save loading: no flare)
            hadStroke = stroke;
            if (!tarred && !stroke && !glowing)
                return;
            glowing = tarred || stroke;
            float t = Time.time;
            Color heat = tarred ? new Color(1f, 0.42f, 0.12f) * (0.55f + 0.3f * Mathf.PerlinNoise(t * 6f, 0.2f)) : Color.black;
            Sheen = stroke ? 0.3f + 0.22f * Mathf.PerlinNoise(t * 1.4f, 3.7f) + 0.1f * Mathf.Sin(t * 3.3f)
                             + 2.5f * Mathf.Exp(-(t - strokeSince) / 0.7f) : 0f;
            Color sheen = new Color(0.62f, 0.76f, 1f) * Sheen;
            for (int i = 0; i < slotSteps.Length; i++)
            {
                if (slotSteps[i] < 0 || slotSteps[i] != ShownStep)
                    continue;
                Color glow = heat + (blades[i] ? sheen : Color.black);
                if (glow.maxColorComponent <= 0f)
                    body.SetPropertyBlock(null, i);
                else
                {
                    burning.SetColor("_EmissionColor", glow);
                    body.SetPropertyBlock(burning, i);
                }
            }
        }

        // How many of the body's material slots are this step's (tests: every step is in the model).
        public int SlotsOf(int step)
        {
            int n = 0;
            if (slotSteps != null)
                foreach (int s in slotSteps)
                    if (s == step)
                        n++;
            return n;
        }

        // Whether a slot shows now.
        public bool Showing(int slot) => slotSteps != null && slot < slotSteps.Length && (slotSteps[slot] < 0 || slotSteps[slot] == ShownStep);

        void Show()
        {
            if (body == null || apprentice.Equipment == null)
                return;
            int step = apprentice.Equipment.OarStep;
            if (step == ShownStep)
                return;
            ShownStep = step;
            for (int i = 0; i < slotSteps.Length; i++)
                if (slotSteps[i] >= 0)
                    body.SetPropertyBlock(slotSteps[i] == step ? null : hidden, i);
        }

        // A mesh baked from the body, with every other step's oar taken out of it.
        public static void HideOthers(Mesh mesh, Material[] materials, int step)
        {
            for (int i = 0; i < materials.Length && i < mesh.subMeshCount; i++)
            {
                int s = StepOf(materials[i]);
                if (s >= 0 && s != step)
                    mesh.SetTriangles(System.Array.Empty<int>(), i);
            }
        }
    }
}
