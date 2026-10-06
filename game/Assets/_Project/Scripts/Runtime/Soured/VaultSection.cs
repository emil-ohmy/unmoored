using UnityEngine;

namespace MyGame
{
    // A great section of the glasshouse's vault, torn loose by Father Moth's gathering: a big glow on the floor where it'll
    // come down (the tell: pale blue, not his orange), then it crashes in and stays, standing at a slant, a wall of iron
    // and glass taller than a man, its panes glowing a cold pale blue, and on the floor behind it a pale patch of light:
    // stand there. Behind it (it between you and him) his blast can't reach you.
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
        const float Width = 5f, Height = 3.2f;
        static readonly Color Glow = new Color(0.55f, 0.82f, 1f, 0.55f); // (pale blue: shelter, not harm)
        Transform safe;                        // the patch of light behind it, once it's down
        Mesh safeMesh;
        Color[] safeColours;
        Light safeLight;

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
            // Heavy iron: two ribs, three purlins, a mullion; panes in all four lights (it's whole: a wall).
            foreach (float x in new[] { -Width / 2f + 0.12f, Width / 2f - 0.12f })
                Part(iron, new Vector3(x, Height / 2f, 0f), new Vector3(0.26f, Height, 0.26f));
            foreach (float y in new[] { 0.15f, Height * 0.5f, Height - 0.12f })
                Part(iron, new Vector3(0f, y, 0f), new Vector3(Width, 0.18f, 0.2f));
            Part(iron, new Vector3(0f, Height * 0.5f, -0.02f), new Vector3(0.16f, Height, 0.16f));
            foreach (float x in new[] { -Width * 0.25f, Width * 0.25f })
                foreach (float y in new[] { Height * 0.27f, Height * 0.74f })
                    Part(glass, new Vector3(x, y, 0f), new Vector3(Width * 0.46f, Height * 0.44f, 0.04f));
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
            {
                // Glowing: the patch behind it breathing, a mote or two rising off it.
                float pulse = 0.75f + 0.25f * Mathf.Sin(Time.time * 3f);
                for (int i = 0; i < 4; i++)
                    safeColours[i] = new Color(Glow.r, Glow.g, Glow.b, Glow.a * pulse);
                safeMesh.colors = safeColours;
                safeLight.intensity = 2.6f * pulse;
                if (Random.value < 6f * Time.deltaTime)
                    Effects.Instance?.Wisp(safe.position + new Vector3(Random.Range(-1.4f, 1.4f), 0.1f, Random.Range(-1.4f, 1.4f)), new Color(0.6f, 0.85f, 1f),
                        Vector3.up * Random.Range(0.4f, 0.9f), Random.Range(0.8f, 1.4f));
                return;
            }
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
            // Behind it, a pale patch of light on the floor (stand there), and its own cold light.
            Vector3 behind = spot - (facing * Vector3.forward) * 1.9f;
            safe = new GameObject("Shelter", typeof(MeshFilter), typeof(MeshRenderer)).transform;
            safe.SetParent(transform, false);
            safe.position = behind + Vector3.up * 0.05f;
            safeMesh = FallingDebris.Mark(1.9f);
            safeColours = new Color[4];
            safe.GetComponent<MeshFilter>().sharedMesh = safeMesh;
            var sr = safe.GetComponent<MeshRenderer>();
            sr.sharedMaterial = Effects.Instance != null ? Effects.Instance.sparkMaterial : null;
            sr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            safeLight = new GameObject("Shelter Light").AddComponent<Light>();
            safeLight.transform.SetParent(transform, false);
            safeLight.transform.position = behind + Vector3.up * 1.6f;
            safeLight.type = LightType.Point;
            safeLight.color = new Color(0.6f, 0.82f, 1f);
            safeLight.range = 6f;
            safeLight.shadows = LightShadows.None;
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
            if (safeMesh != null)
                Destroy(safeMesh);
        }
    }
}
