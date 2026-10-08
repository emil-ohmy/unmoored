using UnityEngine;

namespace MyGame
{
    // A seed pod thrown out of Father Moth's autumn hollow (phase 2): a dry husk lobbed to where you stood, where it lies a moment,
    // swelling and glowing (the tell), then bursts into a ring of leaves.
    public sealed class SeedPod : MonoBehaviour
    {
        public static int Live { get; private set; } // (tests)

        Apprentice target;
        Vector3 from, to;
        float flight, fuse, age, leafSpeed, leafDamage;
        int leaves;
        Material leafMaterial;
        Vector3 middle;
        Vector2 bounds;
        Renderer body;
        AudioClip[] burstSound;

        public static SeedPod Lob(Vector3 from, Vector3 to, Apprentice target, Material podMaterial, Material leafMaterial, int leaves,
                                  float leafSpeed, float leafDamage, Vector3 middle, Vector2 bounds, AudioClip[] burstSound)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "Seed Pod";
            Destroy(go.GetComponent<Collider>());
            go.transform.position = from;
            go.transform.localScale = new Vector3(0.32f, 0.32f, 0.45f);
            var pod = go.AddComponent<SeedPod>();
            pod.from = from;
            pod.to = to;
            pod.target = target;
            pod.flight = Mathf.Clamp(Vector3.Distance(from, to) / 9f, 0.4f, 1.1f);
            pod.fuse = 0.6f;
            pod.leaves = leaves;
            pod.leafSpeed = leafSpeed;
            pod.leafDamage = leafDamage;
            pod.leafMaterial = leafMaterial;
            pod.middle = middle;
            pod.bounds = bounds;
            pod.burstSound = burstSound;
            pod.body = go.GetComponent<MeshRenderer>();
            if (podMaterial != null)
                pod.body.sharedMaterial = podMaterial;
            pod.body.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return pod;
        }

        void OnEnable() => Live++;
        void OnDisable() => Live--;

        void Update()
        {
            age += Time.deltaTime;
            if (age < flight)
            {
                // Lobbed: an arc, turning end over end.
                float u = age / flight;
                transform.position = Vector3.Lerp(from, to, u) + Vector3.up * Mathf.Sin(u * Mathf.PI) * 1.6f;
                transform.Rotate(540f * Time.deltaTime, 0f, 0f, Space.Self);
                return;
            }
            // Down: swelling, glowing, about to go.
            float k = Mathf.Clamp01((age - flight) / fuse);
            transform.position = to;
            transform.localScale = new Vector3(0.32f, 0.32f, 0.45f) * (1f + 0.5f * k + 0.08f * Mathf.Sin(age * 40f));
            var block = new MaterialPropertyBlock();
            block.SetColor("_EmissionColor", Color.Lerp(new Color(0.3f, 0.2f, 0.05f), new Color(1.6f, 0.8f, 0.2f), k));
            body.SetPropertyBlock(block);
            if (k < 1f)
                return;
            // Burst: a ring of leaves out from it.
            float turn = Random.Range(0f, 360f / leaves);
            for (int i = 0; i < leaves; i++)
            {
                Vector3 dir = Quaternion.Euler(0f, turn + i * 360f / leaves, 0f) * Vector3.forward;
                DriftLeaf.Fling(to + dir * 0.3f, dir, leafSpeed, leafDamage, target, leafMaterial, middle, bounds, 1.0f);
            }
            Effects.Instance?.Leaves(to, Vector3.zero, 14);
            Sfx.Instance?.Attack(burstSound, to, 0.9f, "seed_pod_burst");
            Destroy(gameObject);
        }
    }
}
