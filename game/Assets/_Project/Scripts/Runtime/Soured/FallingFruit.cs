using UnityEngine;

namespace MyGame
{
    // A withered orange shaken down from the glasshouse's vault (phase 2): a glow on the floor where it'll land (the tell),
    // brightening, then it drops and splits. Whoever stands in the glow when it lands is struck (no guard stops it: step
    // out of it, or dodge).
    public sealed class FallingFruit : MonoBehaviour
    {
        public static int Live { get; private set; } // (tests)

        Apprentice target;
        Vector3 spot;
        float fall, age, radius, damage;
        Transform fruit;
        Mesh mesh;
        Color[] colours;
        AudioClip[] landSound;
        static readonly Color Glow = new Color(1f, 0.55f, 0.18f, 0.55f);

        public static FallingFruit Drop(Vector3 spot, Apprentice target, Material fruitMaterial, float fall, float radius, float damage, AudioClip[] landSound)
        {
            var go = new GameObject("Falling Fruit", typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.position = spot + Vector3.up * 0.04f;
            var f = go.AddComponent<FallingFruit>();
            f.spot = spot;
            f.target = target;
            f.fall = fall;
            f.radius = radius;
            f.damage = damage;
            f.landSound = landSound;
            // The mark: a soft round glow on the floor (the soft dot of the sparks' texture, tinted).
            f.mesh = new Mesh { name = "Fruit Mark" };
            f.mesh.vertices = new[] { new Vector3(-radius, 0f, -radius), new Vector3(radius, 0f, -radius), new Vector3(radius, 0f, radius), new Vector3(-radius, 0f, radius) };
            f.mesh.uv = new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f) };
            f.mesh.triangles = new[] { 0, 2, 1, 0, 3, 2, 0, 1, 2, 0, 2, 3 };
            f.colours = new Color[4];
            go.GetComponent<MeshFilter>().sharedMesh = f.mesh;
            var r = go.GetComponent<MeshRenderer>();
            r.sharedMaterial = Effects.Instance != null ? Effects.Instance.sparkMaterial : null;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            // The fruit, high above, dropping.
            var ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ball.name = "Withered Orange";
            Destroy(ball.GetComponent<Collider>());
            ball.transform.SetParent(go.transform, false);
            ball.transform.localScale = Vector3.one * 0.32f;
            var br = ball.GetComponent<MeshRenderer>();
            if (fruitMaterial != null)
                br.sharedMaterial = fruitMaterial;
            br.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            f.fruit = ball.transform;
            f.Refresh(0f);
            return f;
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
            // Down: split on the stone; whoever's in the glow is struck.
            Effects.Instance?.Rubble(spot, 0.5f);
            Effects.Instance?.Leaves(spot + Vector3.up * 0.2f, Vector3.zero, 8);
            Sfx.Instance?.Attack(landSound, spot, 0.9f, "fruit_fall");
            if (target != null && target.IsAlive)
            {
                Vector3 to = target.transform.position - spot;
                to.y = 0f;
                if (to.magnitude <= radius && Mathf.Abs(target.transform.position.y - spot.y) < 1f)
                    target.TakeHit(damage, (to.sqrMagnitude > 0.01f ? to.normalized : Vector3.forward) * 4f, null, blockable: false);
            }
            Destroy(gameObject);
        }

        void Refresh(float u)
        {
            float a = Glow.a * Mathf.Lerp(0.25f, 1f, u * u);
            for (int i = 0; i < 4; i++)
                colours[i] = new Color(Glow.r, Glow.g, Glow.b, a);
            mesh.colors = colours;
            if (fruit != null)
                fruit.localPosition = Vector3.up * Mathf.Lerp(6f, 0.15f, u * u); // (falling: faster and faster)
        }

        void OnDestroy()
        {
            if (mesh != null)
                Destroy(mesh);
        }
    }
}
