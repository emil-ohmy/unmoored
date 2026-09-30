using UnityEngine;

namespace MyGame
{
    // The oar's look follows its step (Oar.cs): the model carries every step's trims, each its own material, and this
    // hides the ones the oar hasn't reached (dissolved away completely) and stains the wood. Tar and twine for the
    // Oarhand's, iron for the Boatman's, carved notches for the Steersman's; the Ferryman's is pale, with his mark
    // glowing on the blade (the tar and the Apprentice's red band gone).
    [RequireComponent(typeof(Apprentice))]
    public sealed class OarLook : MonoBehaviour
    {
        // The wood and the blade at each step.
        public static readonly Color[] Wood =
        {
            Hex(0xb08a5c), Hex(0x7c5c3c), Hex(0x6e5236), Hex(0x5e4632), Hex(0xbfb49c),
        };
        public static readonly Color[] Blade =
        {
            Hex(0x8f6d48), Hex(0x6b4d33), Hex(0x5d4430), Hex(0x4f3a2a), Hex(0xada28a),
        };

        // Each trim, and the steps it shows at.
        static readonly (string material, int from, int to)[] Trims =
        {
            ("OarBand", 0, 3), ("OarTar", 1, 3), ("OarTwine", 1, 3), ("OarIron", 2, 4), ("OarCarving", 3, 4),
            ("OarFerryman", 4, 4),
        };

        Apprentice apprentice;
        SkinnedMeshRenderer body;
        MaterialPropertyBlock block;
        int wood = -1, blade = -1;
        int[] trimSlots;

        public int ShownStep { get; private set; } = -1;

        void Awake()
        {
            apprentice = GetComponent<Apprentice>();
            body = GetComponentInChildren<SkinnedMeshRenderer>();
            block = new MaterialPropertyBlock();
            if (body == null)
                return;
            var materials = body.sharedMaterials;
            wood = Slot(materials, "OarWood");
            blade = Slot(materials, "OarBlade");
            trimSlots = new int[Trims.Length];
            for (int i = 0; i < Trims.Length; i++)
                trimSlots[i] = Slot(materials, Trims[i].material);
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

        // Whether the model shows this trim now (tests).
        public bool Showing(string material)
        {
            for (int i = 0; i < Trims.Length; i++)
                if (Trims[i].material == material)
                    return ShownStep >= Trims[i].from && ShownStep <= Trims[i].to;
            return false;
        }

        void Show()
        {
            if (body == null || apprentice.Equipment == null)
                return;
            int step = apprentice.Equipment.OarStep;
            if (step == ShownStep)
                return;
            ShownStep = step;
            Tint(wood, Wood[step]);
            Tint(blade, Blade[step]);
            for (int i = 0; i < Trims.Length; i++)
            {
                if (trimSlots[i] < 0)
                    continue;
                bool shown = step >= Trims[i].from && step <= Trims[i].to;
                block.Clear();
                if (!shown)
                    block.SetFloat("_Dissolve", 1.5f); // past the noise's top: nothing left
                body.SetPropertyBlock(block, trimSlots[i]);
            }
        }

        void Tint(int slot, Color colour)
        {
            if (slot < 0)
                return;
            block.Clear();
            block.SetColor("_BaseColor", colour);
            body.SetPropertyBlock(block, slot);
        }

        static int Slot(Material[] materials, string name) =>
            System.Array.FindIndex(materials, m => m != null && m.name.StartsWith(name));

        static Color Hex(int rgb) => new Color(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f);
    }
}
