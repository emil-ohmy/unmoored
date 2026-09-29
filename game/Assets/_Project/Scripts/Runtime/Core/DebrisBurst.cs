using UnityEngine;

namespace MyGame
{
    // Bits of something torn apart (junk off a Laden's heap): small chunks thrown out, tumbling, that fall to the
    // ground, lie a moment and shrink away.
    public sealed class DebrisBurst : MonoBehaviour
    {
        Transform[] chunks;
        Vector3[] velocity, spin;
        float floorY, bornAt;
        const float Life = 2.2f;

        public static void Throw(Vector3 at, Vector3 towards, Material[] materials, int count)
        {
            if (materials == null || materials.Length == 0)
                return;
            var burst = new GameObject("Debris").AddComponent<DebrisBurst>();
            burst.bornAt = Time.time;
            burst.floorY = at.y - 1.05f;
            burst.chunks = new Transform[count];
            burst.velocity = new Vector3[count];
            burst.spin = new Vector3[count];
            for (int i = 0; i < count; i++)
            {
                var c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Destroy(c.GetComponent<Collider>());
                c.transform.SetParent(burst.transform, false);
                c.transform.SetPositionAndRotation(at + Random.insideUnitSphere * 0.25f, Random.rotation);
                c.transform.localScale = new Vector3(Random.Range(0.12f, 0.32f), Random.Range(0.08f, 0.22f), Random.Range(0.12f, 0.36f));
                var r = c.GetComponent<MeshRenderer>();
                r.sharedMaterial = materials[Random.Range(0, materials.Length)];
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                burst.chunks[i] = c.transform;
                burst.velocity[i] = (towards * Random.Range(1.5f, 4f) + Random.insideUnitSphere * 1.5f) + Vector3.up * Random.Range(1.5f, 4f);
                burst.spin[i] = Random.insideUnitSphere * 720f;
            }
        }

        void Update()
        {
            float dt = Time.deltaTime, age = Time.time - bornAt;
            for (int i = 0; i < chunks.Length; i++)
            {
                var c = chunks[i];
                Vector3 p = c.position;
                if (p.y > floorY + 0.04f || velocity[i].y > 0f)
                {
                    velocity[i] += Vector3.down * 18f * dt;
                    p += velocity[i] * dt;
                    c.rotation = Quaternion.Euler(spin[i] * dt) * c.rotation;
                    if (p.y <= floorY + 0.04f)
                    {
                        p.y = floorY + 0.04f;
                        velocity[i] = Vector3.zero;
                    }
                    c.position = p;
                }
                if (age > Life - 0.6f)
                    c.localScale *= Mathf.Pow(0.02f, dt / 0.6f); // shrinking away
            }
            if (age >= Life)
                Destroy(gameObject);
        }
    }
}
