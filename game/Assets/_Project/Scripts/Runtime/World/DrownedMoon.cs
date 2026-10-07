using UnityEngine;

namespace MyGame
{
    // The Drowned Moon, poured out of Father Moth as he falls: a circle of liquid moonlight on the floor beside him,
    // rippling, a pale crescent shifting in it, its cold light on the leaves. It fills as the stream runs into it; full,
    // it can be taken: lifted out like a coin from a puddle (the key-item ceremony), and it sets into the oar blade
    // (the Stroke). Drawn with the effects' own additive materials (the soft dot, the ring), lying flat on the floor.
    public sealed class DrownedMoon : MonoBehaviour, IInteractable
    {
        public static readonly Color Moonlight = new Color(0.74f, 0.86f, 1f);

        public float radius = 0.85f;
        public float fillTime = 2.2f;
        public float interactRadius = 1.8f;

        public float Fill { get; private set; }
        public bool Full => Fill >= 1f;
        public bool Taken { get; private set; }

        Mesh glowMesh, bodyMesh, crescentMesh, coinMesh;
        Mesh[] rings;
        Transform crescent, coin;
        Light lamp;
        PickupGlow glow;
        Apprentice taker;
        float takenAt, drain = 1f, phase;

        // Pours it out at `spot` (on the floor): it fills over fillTime.
        public static DrownedMoon Pour(Vector3 spot)
        {
            var go = new GameObject("The Drowned Moon");
            go.SetActive(false); // (its glow reads its settings as it wakes)
            go.transform.position = spot;
            var moon = go.AddComponent<DrownedMoon>();
            moon.Build();
            go.SetActive(true);
            return moon;
        }

        // Already full, waiting to be taken (the Tester's Post; a save).
        public static DrownedMoon AtRest(Vector3 spot)
        {
            var moon = Pour(spot);
            moon.Fill = 1f;
            return moon;
        }

        void Build()
        {
            phase = Random.value * 10f;
            var effects = Effects.Instance;
            Material dot = effects != null ? effects.sparkMaterial : null, ring = effects != null ? effects.flashMaterial : null;
            glowMesh = Flat("Moonlight on the Floor", dot, radius * 3.2f, 0.01f).mesh;
            bodyMesh = Flat("The Pool", dot, radius * 2.2f, 0.025f).mesh;
            rings = new Mesh[3];
            for (int i = 0; i < rings.Length; i++)
                rings[i] = Flat("Ripple", ring, radius * 2f, 0.03f + 0.002f * i).mesh;
            crescent = Shape("Crescent", dot, CrescentMesh(radius * 0.42f), 0.035f).transform;
            crescentMesh = crescent.GetComponent<MeshFilter>().sharedMesh;
            lamp = new GameObject("Moon Pool Light").AddComponent<Light>();
            lamp.transform.SetParent(transform, false);
            lamp.transform.localPosition = Vector3.up * 0.45f;
            lamp.type = LightType.Point;
            lamp.color = Moonlight;
            lamp.range = 4.5f;
            lamp.intensity = 0f;
            lamp.shadows = LightShadows.None;
            glow = gameObject.AddComponent<PickupGlow>(); // a key item: bright, cold
            glow.tier = PickupGlow.Tier.Key;
            glow.colour = Moonlight;
            glow.height = 0.7f;
            glow.glowing = new Renderer[0];
            glow.Lit = false;
        }

        // A quad lying flat on the floor, centred, `size` across, at `lift` above it.
        (Mesh mesh, MeshRenderer renderer) Flat(string name, Material material, float size, float lift)
        {
            float h = size * 0.5f;
            var mesh = new Mesh { name = name };
            mesh.vertices = new[] { new Vector3(-h, 0f, -h), new Vector3(-h, 0f, h), new Vector3(h, 0f, h), new Vector3(h, 0f, -h) };
            mesh.uv = new[] { new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(1f, 0f) };
            mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            mesh.colors = new Color[4];
            mesh.RecalculateBounds();
            var renderer = Shape(name, material, mesh, lift);
            return (mesh, renderer);
        }

        MeshRenderer Shape(string name, Material material, Mesh mesh, float lift)
        {
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(transform, false);
            go.transform.localPosition = Vector3.up * lift;
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return renderer;
        }

        // A crescent in the XZ plane, horns along Z: the outer arc a half circle, the inner one pressed in towards it.
        // Every UV at the soft dot's centre (solid), so its colour is all in its vertices.
        static Mesh CrescentMesh(float r)
        {
            const int n = 14;
            var verts = new Vector3[(n + 1) * 2];
            var tris = new int[n * 6];
            for (int k = 0; k <= n; k++)
            {
                float a = Mathf.Lerp(-90f, 90f, k / (float)n) * Mathf.Deg2Rad;
                verts[k * 2] = new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
                verts[k * 2 + 1] = new Vector3(Mathf.Cos(a) * r * 0.4f, 0f, Mathf.Sin(a) * r);
            }
            for (int k = 0; k < n; k++)
            {
                int o = k * 2;
                tris[k * 6] = o; tris[k * 6 + 1] = o + 2; tris[k * 6 + 2] = o + 1;
                tris[k * 6 + 3] = o + 1; tris[k * 6 + 4] = o + 2; tris[k * 6 + 5] = o + 3;
            }
            var uv = new Vector2[verts.Length];
            for (int i = 0; i < uv.Length; i++)
                uv[i] = new Vector2(0.5f, 0.5f);
            var mesh = new Mesh { name = "Crescent", vertices = verts, triangles = tris, uv = uv, colors = new Color[verts.Length] };
            mesh.RecalculateBounds();
            return mesh;
        }

        static void Tint(Mesh mesh, Color colour)
        {
            if (mesh == null)
                return;
            var colours = new Color[mesh.vertexCount];
            for (int i = 0; i < colours.Length; i++)
                colours[i] = colour;
            mesh.colors = colours;
        }

        void OnEnable() => Interactables.Register(this);
        void OnDisable() => Interactables.Unregister(this);

        public Vector3 InteractPosition => transform.position;
        public float InteractRadius => interactRadius;
        public bool Available => Full && !Taken;
        public string PromptLabel => "Take";

        // Lifted out of the pool: the ceremony; it goes into the oar at the flash.
        public void Interact(Apprentice apprentice)
        {
            Taken = true;
            taker = apprentice;
            takenAt = Time.time;
            glow.enabled = false;
            foreach (var l in GetComponentsInChildren<Light>())
                if (l != lamp)
                    l.enabled = false;
            // The coin of it, as thin as glass, rising with the hand.
            var go = new GameObject("The Drowned Moon (lifted)");
            go.transform.position = transform.position + Vector3.up * 0.05f;
            coin = go.transform;
            Material dot = Effects.Instance != null ? Effects.Instance.sparkMaterial : null;
            var face = new GameObject("Face", typeof(MeshFilter), typeof(MeshRenderer));
            face.transform.SetParent(coin, false);
            coinMesh = new Mesh { name = "Coin" };
            coinMesh.vertices = new[] { new Vector3(-0.3f, -0.3f, 0f), new Vector3(-0.3f, 0.3f, 0f), new Vector3(0.3f, 0.3f, 0f), new Vector3(0.3f, -0.3f, 0f) };
            coinMesh.uv = new[] { new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(1f, 0f) };
            coinMesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            Tint(coinMesh, new Color(0.85f, 0.93f, 1f, 1f));
            face.GetComponent<MeshFilter>().sharedMesh = coinMesh;
            var r = face.GetComponent<MeshRenderer>();
            r.sharedMaterial = dot;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            apprentice.ReceiveKeyItem("The Stroke", Apprentice.StrokeHint, apprentice.GiveStroke, grantAtFlash: true);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            float t = Time.time + phase;
            if (!Taken && Fill < 1f)
            {
                Fill = Mathf.Min(1f, Fill + dt / Mathf.Max(0.01f, fillTime));
                if (Full)
                    Effects.Instance?.KeyItemRing(transform.position + Vector3.up * 0.2f);
            }
            if (glow != null)
                glow.Lit = Full && !Taken;
            if (Taken)
                drain = Mathf.MoveTowards(drain, 0f, dt / 0.7f);
            float f = Mathf.Sqrt(Fill) * drain; // (the pool spreads fast, then slower)
            // The liquid: its brightness rippling with two slow waves, its edge breathing.
            float ripple = 0.85f + 0.1f * Mathf.Sin(t * 2.3f) + 0.05f * Mathf.Sin(t * 5.1f + 1.3f);
            float breathe = 1f + 0.03f * Mathf.Sin(t * 1.7f);
            transform.GetChild(0).localScale = Vector3.one * Mathf.Max(0.001f, f * breathe);
            transform.GetChild(1).localScale = Vector3.one * Mathf.Max(0.001f, f * breathe);
            Tint(glowMesh, new Color(0.45f, 0.6f, 0.95f, 0.4f * ripple * f));
            Tint(bodyMesh, new Color(0.8f, 0.9f, 1f, 0.95f * ripple * f));
            // Rings spreading out over it, one after another.
            for (int i = 0; i < rings.Length; i++)
            {
                float u = Mathf.Repeat(t * 0.45f + i / (float)rings.Length, 1f);
                var ringT = transform.GetChild(2 + i);
                ringT.localScale = Vector3.one * Mathf.Max(0.001f, (0.2f + 0.85f * u) * f);
                Tint(rings[i], new Color(0.85f, 0.93f, 1f, 0.55f * Mathf.Sin(u * Mathf.PI) * f));
            }
            // The crescent drifting in it, turning slowly, as a reflection does when the water moves.
            crescent.localPosition = new Vector3(Mathf.Sin(t * 0.6f) * 0.12f, 0.035f, Mathf.Cos(t * 0.45f) * 0.1f) * Mathf.Max(f, 0.001f);
            crescent.localRotation = Quaternion.Euler(0f, 20f * Mathf.Sin(t * 0.3f), 0f);
            crescent.localScale = new Vector3(1f + 0.08f * Mathf.Sin(t * 1.9f), 1f, 1f) * Mathf.Max(0.001f, f);
            Tint(crescentMesh, new Color(0.95f, 0.98f, 1f, 0.9f * f));
            lamp.intensity = 2.4f * f * ripple;
            if (!Taken && Full && Effects.Instance != null && Random.value < 4f * dt) // a mote rising off it now and then
                Effects.Instance.Wisp(transform.position + Random.insideUnitSphere * radius * 0.6f + Vector3.up * 0.1f, Moonlight);
            UpdateCoin();
            if (Taken && drain <= 0f && coin == null)
                gameObject.SetActive(false);
        }

        // The coin rises with the hand, faces the camera, and is gone into the oar at the flash.
        void UpdateCoin()
        {
            if (coin == null)
                return;
            if (taker == null || taker.HasStroke || Time.time - takenAt > 3f)
            {
                if (Effects.Instance != null)
                    for (int i = 0; i < 12; i++)
                        Effects.Instance.Wisp(coin.position, Moonlight, Random.insideUnitSphere * 1.4f, Random.Range(0.4f, 0.9f));
                Destroy(coin.gameObject);
                coin = null;
                return;
            }
            float u = Mathf.Clamp01((Time.time - takenAt) / 0.5f);
            coin.position = Vector3.Lerp(coin.position, taker.HeldPoint, u * u);
            var cam = Camera.main;
            if (cam != null)
                coin.rotation = Quaternion.LookRotation(coin.position - cam.transform.position);
            coin.localScale = Vector3.one * (0.8f + 0.06f * Mathf.Sin(Time.time * 6f));
        }

        void OnDestroy()
        {
            if (coin != null)
                Destroy(coin.gameObject);
        }
    }
}
