using UnityEngine;

namespace MyGame
{
    // A great section of the glasshouse's vault, torn loose by Father Moth's gathering: a big glow on the floor where it'll
    // come down (the tell), then it crashes in and stays, standing at a slant, a wall of iron and glass as tall as a man.
    // Behind it (it between you and him) his release can't reach you.
    public sealed class VaultSection : MonoBehaviour
    {
        public bool Down { get; private set; }

        Apprentice target;
        Vector3 spot;
        Quaternion facing;
        float fall, age, damage;
        Transform section;
        BoxCollider wall;
        Mesh mesh;
        Color[] colours;
        AudioClip[] landSound;
        const float Width = 3.6f, Height = 2.6f;
        static readonly Color Glow = new Color(1f, 0.7f, 0.35f, 0.45f);

        // Down at `spot`, its broad face turned to `towards` (him).
        public static VaultSection Drop(Vector3 spot, Vector3 towards, Apprentice target, Material iron, Material glass, float fall, float damage, AudioClip[] landSound)
        {
            var go = new GameObject("Vault Section", typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.position = spot + Vector3.up * 0.05f;
            var v = go.AddComponent<VaultSection>();
            v.spot = spot;
            v.target = target;
            v.fall = fall;
            v.damage = damage;
            v.landSound = landSound;
            Vector3 face = towards - spot;
            face.y = 0f;
            v.facing = Quaternion.LookRotation(face.sqrMagnitude > 0.01f ? face.normalized : Vector3.forward);
            v.mesh = FallingDebris.Mark(Width * 0.75f);
            v.colours = new Color[4];
            go.GetComponent<MeshFilter>().sharedMesh = v.mesh;
            var r = go.GetComponent<MeshRenderer>();
            r.sharedMaterial = Effects.Instance != null ? Effects.Instance.sparkMaterial : null;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            // The section: two ribs, purlins across, panes (some broken out), as tall as a man and wider.
            v.section = new GameObject("Section").transform;
            v.section.SetParent(go.transform, false);
            void Part(Material material, Vector3 at, Vector3 size)
            {
                var p = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Destroy(p.GetComponent<Collider>());
                p.transform.SetParent(v.section, false);
                p.transform.localPosition = at;
                p.transform.localScale = size;
                var pr = p.GetComponent<MeshRenderer>();
                if (material != null)
                    pr.sharedMaterial = material;
            }
            foreach (float x in new[] { -Width / 2f + 0.1f, Width / 2f - 0.1f })
                Part(iron, new Vector3(x, Height / 2f, 0f), new Vector3(0.18f, Height, 0.18f));
            foreach (float y in new[] { 0.15f, Height * 0.5f, Height - 0.1f })
                Part(iron, new Vector3(0f, y, 0f), new Vector3(Width, 0.12f, 0.14f));
            Part(glass, new Vector3(-Width * 0.25f, Height * 0.27f, 0f), new Vector3(Width * 0.45f, Height * 0.42f, 0.03f));
            Part(glass, new Vector3(Width * 0.25f, Height * 0.72f, 0f), new Vector3(Width * 0.45f, Height * 0.4f, 0.03f));
            Part(iron, new Vector3(0f, Height * 0.5f, -0.02f), new Vector3(0.1f, Height, 0.1f)); // (the mullion, whole)
            v.Refresh(0f);
            return v;
        }

        // Whether it stands between `from` and `to` (the release can't get through it).
        public bool Shields(Vector3 from, Vector3 to)
        {
            if (!Down || wall == null)
                return false;
            Vector3 d = to - from;
            return wall.Raycast(new Ray(from, d.normalized), out _, d.magnitude);
        }

        void Update()
        {
            if (Down)
                return;
            age += Time.deltaTime;
            float u = Mathf.Clamp01(age / fall);
            Refresh(u);
            if (u < 1f)
                return;
            // Down: it crashes in and stays, standing at a slant; whatever was under it is struck.
            Down = true;
            section.localPosition = Vector3.zero;
            section.rotation = facing * Quaternion.Euler(-12f, 0f, Random.Range(-6f, 6f));
            var holder = new GameObject("Wall"); // (a collider standing as it stands: you can't walk through it, nor can his release)
            holder.transform.SetParent(transform, false);
            holder.transform.rotation = section.rotation;
            wall = holder.AddComponent<BoxCollider>();
            wall.center = new Vector3(0f, Height / 2f, 0f);
            wall.size = new Vector3(Width, Height, 0.5f);
            GetComponent<MeshRenderer>().enabled = false;
            Effects.Instance?.Rubble(spot, 2f);
            for (int i = 0; i < 14; i++)
                Effects.Instance?.BlockSpark(spot + Vector3.up * 0.3f + Random.insideUnitSphere * 1.2f, Vector3.up);
            Sfx.Instance?.Attack(landSound, spot, 1f, "vault_section");
            CameraRig.Instance?.Shake(0.6f);
            if (target != null && target.IsAlive)
            {
                Vector3 to = target.transform.position - spot;
                to.y = 0f;
                if (to.magnitude <= Width * 0.7f)
                    target.TakeHit(damage, (to.sqrMagnitude > 0.01f ? to.normalized : Vector3.forward) * 8f, null, blockable: false);
            }
        }

        void Refresh(float u)
        {
            float a = Glow.a * Mathf.Lerp(0.3f, 1f, u * u) * (0.85f + 0.15f * Mathf.Sin(Time.time * 12f));
            for (int i = 0; i < 4; i++)
                colours[i] = new Color(Glow.r, Glow.g, Glow.b, a);
            mesh.colors = colours;
            section.localPosition = Vector3.up * Mathf.Lerp(8f, 0f, u * u);
            section.rotation = facing * Quaternion.Euler(Mathf.Lerp(70f, -12f, u), 0f, 0f);
        }

        void OnDestroy()
        {
            if (mesh != null)
                Destroy(mesh);
        }
    }
}
