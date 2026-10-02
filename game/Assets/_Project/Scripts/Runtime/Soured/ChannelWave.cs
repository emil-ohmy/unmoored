using UnityEngine;

namespace MyGame
{
    // A wave out of the Glasshouse's channel (Father Moth beating down on the water: a taste of the Stroke): a crest of
    // water rolling across the floor from the channel to the far wall, the length of the channel. It knocks down whoever
    // it reaches (no guard stops it: dodge through it) and puts out their flames.
    public sealed class ChannelWave : MonoBehaviour
    {
        public float Front { get; private set; } // how far across it's come
        public bool Struck { get; private set; }

        Apprentice target;
        float direction, speed, range, from, to, damage, knockback;
        Mesh mesh;
        Vector3[] vertices;
        Color[] colours;
        float splashDebt;
        static readonly Color Crest = new Color(0.72f, 0.86f, 0.95f, 0.75f);

        // From the channel at `x` (on the floor at `y`, from `zFrom` to `zTo`), rolling `direction` (+1: east; -1: west).
        public static ChannelWave Roll(float x, float y, float zFrom, float zTo, float direction, Apprentice target, float speed, float range,
                                       float damage, float knockback)
        {
            var go = new GameObject("Channel Wave", typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.position = new Vector3(x, y + 0.06f, 0f);
            var wave = go.AddComponent<ChannelWave>();
            wave.target = target;
            wave.direction = Mathf.Sign(direction);
            wave.speed = speed;
            wave.range = range;
            wave.from = zFrom;
            wave.to = zTo;
            wave.damage = damage;
            wave.knockback = knockback;
            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = Effects.Instance != null ? Effects.Instance.sparkMaterial : null;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            wave.Build();
            return wave;
        }

        // A band across the floor (behind the crest, the crest, a sharp front edge), the channel's length.
        void Build()
        {
            mesh = new Mesh { name = "Channel Wave" };
            vertices = new Vector3[6];
            colours = new Color[6];
            mesh.vertices = vertices;
            mesh.uv = new Vector2[6] { new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f) };
            mesh.triangles = new[] { 0, 1, 3, 0, 3, 2, 2, 3, 5, 2, 5, 4, 0, 3, 1, 0, 2, 3, 2, 5, 3, 2, 4, 5 }; // (both faces: seen whichever way it rolls)
            mesh.MarkDynamic();
            GetComponent<MeshFilter>().sharedMesh = mesh;
            Refresh();
        }

        void Update()
        {
            Front += speed * Time.deltaTime;
            if (Front >= range)
            {
                Destroy(gameObject);
                return;
            }
            // Spray along the crest.
            splashDebt += Time.deltaTime * 18f;
            for (; splashDebt >= 1f; splashDebt -= 1f)
            {
                var at = new Vector3(transform.position.x + direction * Front, transform.position.y, Random.Range(from, to));
                Effects.Instance?.Splash(at);
            }
            if (target == null || !target.IsAlive)
            {
                Refresh();
                return;
            }
            Vector3 p = target.transform.position;
            float across = (p.x - transform.position.x) * direction;
            bool there = Mathf.Abs(across - Front) <= 0.55f && p.z >= from - 0.3f && p.z <= to + 0.3f && p.y - transform.position.y < 0.9f;
            if (there)
            {
                target.Douse(); // (soaked)
                if (!Struck)
                {
                    Struck = true; // once: the crest passes on
                    target.TakeHit(damage, new Vector3(direction, 0f, 0f) * knockback, null, blockable: false);
                }
            }
            Refresh();
        }

        void Refresh()
        {
            float fade = 1f - Mathf.Pow(Front / range, 3f);
            float x = direction * Front, back = direction * Mathf.Max(0f, Front - 1.4f), lip = direction * (Front + 0.25f);
            vertices[0] = new Vector3(back, 0f, from); vertices[1] = new Vector3(back, 0f, to);
            vertices[2] = new Vector3(x, 0f, from); vertices[3] = new Vector3(x, 0f, to);
            vertices[4] = new Vector3(lip, 0f, from); vertices[5] = new Vector3(lip, 0f, to);
            for (int i = 0; i < 6; i++)
                colours[i] = new Color(Crest.r, Crest.g, Crest.b, i == 2 || i == 3 ? Crest.a * fade : 0f);
            mesh.vertices = vertices;
            mesh.colors = colours;
            mesh.RecalculateBounds();
        }

        void OnDestroy()
        {
            if (mesh != null)
                Destroy(mesh);
        }
    }
}
