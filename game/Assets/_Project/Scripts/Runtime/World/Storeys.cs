using UnityEngine;

namespace MyGame
{
    // A building of several storeys (the tower). While the Apprentice is inside it, every storey above the one
    // they're on fades away (dissolving, quickly), so the camera looks down into the room they're in; outside, it's
    // whole. Their colliders stay. `footprint`: the building's half-size (x, z) around this object.
    public sealed class Storeys : MonoBehaviour
    {
        [System.Serializable]
        public sealed class Storey
        {
            public float floor;          // the height of its floor (world)
            public GameObject[] parts;   // its models, torches and all
            [HideInInspector] public Renderer[] renderers;
            [HideInInspector] public float shown = 1f;
        }

        public Storey[] storeys = new Storey[0];
        public Vector2 footprint = new Vector2(3.5f, 3.5f);
        public float fadeTime = 0.25f;

        public int Level { get; private set; } = -1; // the storey the Apprentice is on inside, or -1 outside
        public bool Shown(int storey) => storeys[storey].shown > 0.5f;

        Apprentice apprentice;
        MaterialPropertyBlock block;

        void Start()
        {
            apprentice = FindAnyObjectByType<Apprentice>();
            block = new MaterialPropertyBlock();
            foreach (var s in storeys)
            {
                var list = new System.Collections.Generic.List<Renderer>();
                foreach (var p in s.parts)
                    if (p != null)
                        list.AddRange(p.GetComponentsInChildren<Renderer>(true));
                s.renderers = list.ToArray();
            }
        }

        void Update()
        {
            Level = -1;
            if (apprentice != null)
            {
                Vector3 d = apprentice.transform.position - transform.position;
                if (Mathf.Abs(d.x) < footprint.x + 0.1f && Mathf.Abs(d.z) < footprint.y + 0.1f)
                    for (int i = 0; i < storeys.Length; i++)
                        if (apprentice.transform.position.y + 0.5f >= storeys[i].floor)
                            Level = i;
            }
            for (int i = 0; i < storeys.Length; i++)
            {
                var s = storeys[i];
                float target = Level < 0 || i <= Level ? 1f : 0f;
                if (Mathf.Approximately(s.shown, target))
                    continue;
                s.shown = Mathf.MoveTowards(s.shown, target, Time.deltaTime / Mathf.Max(0.01f, fadeTime));
                foreach (var r in s.renderers)
                {
                    if (r == null)
                        continue;
                    r.enabled = s.shown > 0.01f;
                    if (s.shown >= 1f)
                        r.SetPropertyBlock(null);
                    else
                    {
                        block.Clear();
                        block.SetFloat("_Dissolve", 1f - s.shown);
                        block.SetColor("_DissolveEdgeColor", Color.black); // (no glowing edge: it's only stone)
                        r.SetPropertyBlock(block);
                    }
                }
            }
        }
    }
}
