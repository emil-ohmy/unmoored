using UnityEngine;

namespace MyGame
{
    // Debris shaken down from the glasshouse's vault (Father Moth's last phase, as he gathers): a glow on the floor where
    // it'll land (the tell), brightening, then a length of iron rib with its broken panes comes down and smashes. Whoever
    // stands in the glow is struck (no guard stops it: step out of it, or dodge).
    public sealed class FallingDebris : MonoBehaviour
    {
        public static int Live { get; private set; } // (tests)

        Apprentice target;
        Vector3 spot;
        float fall, age, radius, damage;
        Transform piece;
        Mesh mesh;
        Color[] colours;
        AudioClip[] landSound;
        static readonly Color Glow = new Color(1f, 0.62f, 0.3f, 0.5f);

        public static FallingDebris Drop(Vector3 spot, Apprentice target, Material iron, Material glass, float fall, float radius, float damage, AudioClip[] landSound)
        {
            var go = new GameObject("Falling Debris", typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.position = spot + Vector3.up * 0.05f;
            var d = go.AddComponent<FallingDebris>();
            d.spot = spot;
            d.target = target;
            d.fall = fall;
            d.radius = radius;
            d.damage = damage;
            d.landSound = landSound;
            d.mesh = Mark(radius);
            d.colours = new Color[4];
            go.GetComponent<MeshFilter>().sharedMesh = d.mesh;
            var r = go.GetComponent<MeshRenderer>();
            r.sharedMaterial = Effects.Instance != null ? Effects.Instance.sparkMaterial : null;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            // The piece, high above: a length of rib, a pane or two still in it, turning as it falls.
            d.piece = new GameObject("Debris").transform;
            d.piece.SetParent(go.transform, false);
            d.piece.localRotation = Quaternion.Euler(Random.Range(-30f, 30f), Random.Range(0f, 360f), Random.Range(-30f, 30f));
            Part(d.piece, iron, new Vector3(0f, 0f, 0f), new Vector3(radius * 1.6f, 0.14f, 0.14f));
            Part(d.piece, iron, new Vector3(0f, 0f, 0.45f), new Vector3(radius * 1.4f, 0.08f, 0.08f));
            Part(d.piece, glass, new Vector3(Random.Range(-0.3f, 0.3f), 0f, 0.22f), new Vector3(radius * 0.8f, 0.02f, 0.42f));
            d.Refresh(0f);
            return d;
        }

        static void Part(Transform parent, Material material, Vector3 at, Vector3 size)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = at;
            go.transform.localScale = size;
            var r = go.GetComponent<MeshRenderer>();
            if (material != null)
                r.sharedMaterial = material;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        // A soft round glow on the floor (the sparks' soft dot, tinted), `radius` across.
        public static Mesh Mark(float radius)
        {
            var mesh = new Mesh { name = "Fall Mark" };
            mesh.vertices = new[] { new Vector3(-radius, 0f, -radius), new Vector3(radius, 0f, -radius), new Vector3(radius, 0f, radius), new Vector3(-radius, 0f, radius) };
            mesh.uv = new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f) };
            mesh.triangles = new[] { 0, 2, 1, 0, 3, 2, 0, 1, 2, 0, 2, 3 };
            return mesh;
        }

        void OnEnable() => Live++;
        void OnDisable() => Live--;

        void Update()
        {
            age += Time.deltaTime;
            float u = Mathf.Clamp01(age / fall);
            Refresh(u);
            if (u < 1f)
                return;
            // Down: it smashes on the stone; whoever's in the glow is struck.
            Effects.Instance?.Rubble(spot, 0.9f);
            for (int i = 0; i < 6; i++)
                Effects.Instance?.BlockSpark(spot + Vector3.up * 0.2f + Random.insideUnitSphere * 0.4f, Vector3.up);
            Sfx.Instance?.Attack(landSound, spot, 0.9f, "debris_fall");
            if (target != null && target.IsAlive)
            {
                Vector3 to = target.transform.position - spot;
                to.y = 0f;
                if (to.magnitude <= radius && Mathf.Abs(target.transform.position.y - spot.y) < 1f)
                {
                    target.TakeHit(damage, (to.sqrMagnitude > 0.01f ? to.normalized : Vector3.forward) * 5f, null, blockable: false);
                    CameraRig.Instance?.Shake(0.3f);
                }
            }
            Destroy(gameObject);
        }

        void Refresh(float u)
        {
            float a = Glow.a * Mathf.Lerp(0.25f, 1f, u * u);
            for (int i = 0; i < 4; i++)
                colours[i] = new Color(Glow.r, Glow.g, Glow.b, a);
            mesh.colors = colours;
            if (piece != null)
            {
                piece.localPosition = Vector3.up * Mathf.Lerp(7f, 0.1f, u * u); // (falling: faster and faster)
                piece.Rotate(40f * Time.deltaTime, 90f * Time.deltaTime, 0f, Space.Self);
            }
        }

        void OnDestroy()
        {
            if (mesh != null)
                Destroy(mesh);
        }
    }
}
