using UnityEngine;

namespace MyGame
{
    // A chain drawn link by link between two points (the Anchoress's anchor chain, her hook's line): alternate links
    // turned a quarter, sagging in the middle, never through the floor.
    public sealed class LinkChain
    {
        readonly Transform[] links;
        readonly float spacing;
        Vector3[] flying, spin; // snapped: each link flying free
        float floorY;

        public bool Snapped => flying != null;

        public LinkChain(string name, Material material, int maxLinks, Vector3 linkSize, float spacing)
        {
            this.spacing = spacing;
            var parent = new GameObject(name).transform;
            links = new Transform[maxLinks];
            for (int i = 0; i < maxLinks; i++)
            {
                var link = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Object.Destroy(link.GetComponent<Collider>());
                link.name = "Link";
                link.transform.SetParent(parent, false);
                link.transform.localScale = linkSize;
                var renderer = link.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                link.SetActive(false);
                links[i] = link.transform;
            }
        }

        // It breaks: every link shown flies apart, tumbling, and scatters on the floor.
        public void Snap(Vector3 from, float floor)
        {
            floorY = floor;
            flying = new Vector3[links.Length];
            spin = new Vector3[links.Length];
            for (int i = 0; i < links.Length; i++)
            {
                Vector3 out_ = links[i].position - from;
                out_.y = 0f;
                flying[i] = (out_.sqrMagnitude > 0.01f ? out_.normalized : Random.onUnitSphere) * Random.Range(1.5f, 4.5f) + Vector3.up * Random.Range(2f, 5f);
                spin[i] = Random.insideUnitSphere * 900f;
            }
        }

        // Snapped links fly, fall and come to rest on the floor (Draw does nothing more once snapped).
        public void TickSnapped(float dt)
        {
            if (flying == null)
                return;
            for (int i = 0; i < links.Length; i++)
            {
                if (!links[i].gameObject.activeSelf)
                    continue;
                Vector3 p = links[i].position;
                if (p.y <= floorY + 0.03f && flying[i].y <= 0f)
                    continue; // at rest
                flying[i] += Vector3.down * 18f * dt;
                p += flying[i] * dt;
                if (p.y <= floorY + 0.03f)
                {
                    p.y = floorY + 0.03f;
                    flying[i] = Vector3.zero;
                    links[i].rotation = Quaternion.Euler(90f, Random.Range(0f, 360f), 0f);
                }
                else
                    links[i].rotation = Quaternion.Euler(spin[i] * dt) * links[i].rotation;
                links[i].position = p;
            }
        }

        public void Draw(Vector3 a, Vector3 b, float sag, float floorY, bool shown)
        {
            if (flying != null)
                return;
            float distance = Vector3.Distance(a, b);
            int count = shown ? Mathf.Clamp(Mathf.CeilToInt(distance / spacing), 2, links.Length) : 0;
            for (int i = 0; i < links.Length; i++)
            {
                bool on = i < count;
                if (links[i].gameObject.activeSelf != on)
                    links[i].gameObject.SetActive(on);
                if (!on)
                    continue;
                float t = (i + 0.5f) / count;
                Vector3 p = Point(a, b, sag, t, floorY);
                Vector3 along = Point(a, b, sag, Mathf.Min(1f, t + 0.02f), floorY) - Point(a, b, sag, Mathf.Max(0f, t - 0.02f), floorY);
                links[i].SetPositionAndRotation(p, Quaternion.LookRotation(along.sqrMagnitude > 1e-6f ? along : Vector3.forward) * Quaternion.Euler(0f, 0f, i % 2 * 90f));
            }
        }

        static Vector3 Point(Vector3 a, Vector3 b, float sag, float t, float floorY)
        {
            Vector3 p = Vector3.Lerp(a, b, t) + Vector3.down * (sag * 4f * t * (1f - t));
            p.y = Mathf.Max(p.y, floorY);
            return p;
        }

        public void Destroy()
        {
            if (links.Length > 0 && links[0] != null)
                Object.Destroy(links[0].parent.gameObject);
        }
    }
}
