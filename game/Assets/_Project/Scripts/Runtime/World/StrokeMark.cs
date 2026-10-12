using UnityEngine;

namespace MyGame
{
    // The Drowned Moon's mark on what the Stroke can move (a machine's drive, its strike-plate, a pendulum's bob, a lift's
    // deck: silver, a glowing crescent; everything else in the Hall is brass). It glows faintly always; with the Stroke
    // and within its reach, it brightens and pulses (as the Hook's post does when it can be thrown at); while the
    // machine runs, it shines steady.
    public sealed class StrokeMark : MonoBehaviour
    {
        public Clockwork clockwork;
        public Transform[] points = new Transform[0]; // where the Stroke's aimed (strike points)
        public float reach = 4.8f;

        public float Glow { get; private set; } // (tests)

        Renderer[] renderers;
        int[][] slots;
        Color[][] base_;
        MaterialPropertyBlock block;
        Apprentice apprentice;

        void Start()
        {
            renderers = GetComponentsInChildren<Renderer>();
            slots = new int[renderers.Length][];
            base_ = new Color[renderers.Length][];
            for (int r = 0; r < renderers.Length; r++)
            {
                var mats = renderers[r].sharedMaterials;
                var list = new System.Collections.Generic.List<int>();
                var colours = new System.Collections.Generic.List<Color>();
                for (int i = 0; i < mats.Length; i++)
                    if (mats[i] != null && mats[i].name.StartsWith("MoonMark"))
                    {
                        list.Add(i);
                        colours.Add(mats[i].GetColor("_EmissionColor"));
                    }
                slots[r] = list.ToArray();
                base_[r] = colours.ToArray();
            }
            block = new MaterialPropertyBlock();
        }

        public bool InReach
        {
            get
            {
                if (apprentice == null)
                    apprentice = FindAnyObjectByType<Apprentice>();
                if (apprentice == null || !apprentice.HasStroke || !apprentice.IsAlive)
                    return false;
                foreach (var p in points)
                    if (p != null && (p.position - apprentice.transform.position).sqrMagnitude <= reach * reach)
                        return true;
                return false;
            }
        }

        void Update()
        {
            float t = Time.time;
            Glow = clockwork != null && clockwork.Running ? 1.4f : InReach ? 1.5f + 0.7f * Mathf.Sin(t * 6f) : 0.45f;
            for (int r = 0; r < renderers.Length; r++)
                for (int k = 0; k < slots[r].Length; k++)
                {
                    block.Clear();
                    block.SetColor("_EmissionColor", base_[r][k] * Glow);
                    renderers[r].SetPropertyBlock(block, slots[r][k]);
                }
        }
    }
}
