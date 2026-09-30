using UnityEngine;

namespace MyGame
{
    // The oar's look follows its step (Oar.cs): the model carries all five oars (tools/blender/oars.py), each step's
    // materials named for it ("Oar2Iron_Metal"), and this shows only the step the oar is at (the others dissolved away
    // completely). Anything else baked from the body (dash ghosts, the Shade) drops the other steps too: HideOthers.
    [RequireComponent(typeof(Apprentice))]
    public sealed class OarLook : MonoBehaviour
    {
        Apprentice apprentice;
        SkinnedMeshRenderer body;
        MaterialPropertyBlock hidden;
        int[] slotSteps; // each material slot's step (-1: not the oar's)

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
            if (body == null)
                return;
            var materials = body.sharedMaterials;
            slotSteps = new int[materials.Length];
            for (int i = 0; i < materials.Length; i++)
                slotSteps[i] = StepOf(materials[i]);
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
