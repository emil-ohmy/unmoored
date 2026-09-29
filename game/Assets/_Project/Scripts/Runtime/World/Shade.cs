using UnityEngine;

namespace MyGame
{
    // The Apprentice's Shade: a pale, breathing ghost left where they died, holding every Obol they carried. Walking
    // into it streams the Obols back into the bow lantern. Dying again before that loses them: there is only ever one
    // Shade, and a new death replaces it. It rises out of the fallen body once the bow lantern has gone out.
    public sealed class Shade : MonoBehaviour
    {
        public static Shade Current { get; private set; }

        public int Obols { get; private set; }
        public bool Claimed { get; private set; }

        const float TouchRadius = 1.1f, FadeTime = 0.8f;
        static readonly Color Pale = new Color(0.72f, 0.86f, 1f, 0.5f);

        Apprentice owner;
        MeshRenderer body;
        Light glow;
        MaterialPropertyBlock block;
        Vector3 basePosition;
        float fadeFrom = -1f;
        float appearAt, appearFrom; // it rises out of the body: hidden until then, then fading in as it lifts
        const float RiseTime = 1.0f;

        // Leaves a Shade holding `obols` in the pose the Apprentice fell in. Any earlier Shade is lost.
        public static void Leave(Apprentice apprentice, int obols, Material ghostMaterial, float appearAfter = 0f)
        {
            if (Current != null)
                Current.Fade();
            if (obols <= 0 || ghostMaterial == null)
                return;
            var skinned = apprentice.GetComponentInChildren<SkinnedMeshRenderer>();
            var go = new GameObject("Shade", typeof(MeshFilter), typeof(MeshRenderer));
            var mesh = new Mesh { name = "Shade" };
            skinned.BakeMesh(mesh, true);
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            go.transform.SetPositionAndRotation(skinned.transform.position, skinned.transform.rotation);
            var renderer = go.GetComponent<MeshRenderer>();
            var materials = new Material[mesh.subMeshCount];
            for (int i = 0; i < materials.Length; i++)
                materials[i] = ghostMaterial;
            renderer.sharedMaterials = materials;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            var light = new GameObject("Shade Light").AddComponent<Light>();
            light.transform.SetParent(go.transform, false);
            light.transform.position = apprentice.transform.position + Vector3.up * 1.2f;
            light.type = LightType.Point;
            light.color = new Color(0.62f, 0.77f, 1f);
            light.range = 3.2f;
            light.intensity = 1.4f;
            light.shadows = LightShadows.None;

            var shade = go.AddComponent<Shade>();
            shade.appearAt = Time.time + appearAfter;
            shade.owner = apprentice;
            shade.Obols = obols;
            shade.body = renderer;
            shade.glow = light;
            Current = shade;
        }

        // Loading a save: a Shade waiting where the Apprentice fell, with their Obols (in the pose they stand in now).
        public static void Restore(Apprentice apprentice, int obols, Vector3 position, Quaternion rotation, Material ghostMaterial)
        {
            Leave(apprentice, obols, ghostMaterial);
            if (Current == null)
                return;
            Current.transform.SetPositionAndRotation(position, rotation);
            Current.basePosition = position;
            Current.restored = true;
        }

        bool restored;
        public Vector3 RestPosition => basePosition;

        void Awake()
        {
            block = new MaterialPropertyBlock();
            basePosition = transform.position;
        }

        void OnDestroy()
        {
            if (Current == this)
                Current = null;
        }

        void Update()
        {
            float t = Time.time;
            if (t < appearAt)
            {
                Tint(0f); // not yet: the Apprentice is still falling
                return;
            }
            if (appearFrom <= 0f)
            {
                if (restored)
                    appearFrom = t - RiseTime; // (already risen: it has waited here)
                else
                    Rise();
            }
            float risen = UiKit.Smooth(0f, 1f, (t - appearFrom) / RiseTime);
            if (fadeFrom < 0f)
            {
                // Breathe and bob, a hand's width off the ground.
                transform.position = basePosition + Vector3.up * (0.08f + 0.04f * Mathf.Sin(t * 1.3f) - 0.3f * (1f - risen));
                Tint((0.75f + 0.25f * Mathf.Sin(t * 2.1f)) * risen);
                if (owner != null && owner.IsAlive)
                {
                    Vector3 to = owner.transform.position - basePosition;
                    to.y = 0f;
                    if (to.magnitude < TouchRadius)
                        Claim();
                }
                return;
            }
            float f = Mathf.Clamp01((t - fadeFrom) / FadeTime);
            Tint(1f - f);
            transform.position += Vector3.up * (0.6f * Time.deltaTime);
            if (f >= 1f)
                Destroy(gameObject);
        }

        // Out of the body as it lies now (the pose it fell in), in a breath of pale light.
        void Rise()
        {
            appearFrom = Time.time;
            if (owner == null)
                return;
            var skinned = owner.GetComponentInChildren<SkinnedMeshRenderer>();
            var filter = GetComponent<MeshFilter>();
            if (skinned != null && filter != null && filter.sharedMesh != null)
            {
                skinned.BakeMesh(filter.sharedMesh, true);
                transform.SetPositionAndRotation(skinned.transform.position, skinned.transform.rotation);
                basePosition = transform.position;
            }
            for (int i = 0; i < 16; i++)
                Effects.Instance?.Wisp(body.bounds.center + Vector3.Scale(body.bounds.extents, Random.insideUnitSphere));
        }

        // Touched: its Obols stream back into the bow lantern, and it fades.
        void Claim()
        {
            Claimed = true;
            owner.GainObols(Obols);
            for (int i = 0; i < 24; i++)
                Effects.Instance?.EmitFare(body.bounds.center + Vector3.Scale(body.bounds.extents * 0.8f, Random.insideUnitSphere));
            Sfx.Instance?.SoulAbsorb(transform.position + Vector3.up);
            Fade();
        }

        void Fade()
        {
            if (Current == this)
                Current = null;
            fadeFrom = Time.time;
        }

        void Tint(float strength)
        {
            block.SetColor("_Color", new Color(Pale.r, Pale.g, Pale.b, Pale.a * strength));
            body.SetPropertyBlock(block);
            glow.intensity = 1.4f * strength;
        }
    }
}
