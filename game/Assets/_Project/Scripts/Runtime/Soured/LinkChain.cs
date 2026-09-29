using UnityEngine;

namespace MyGame
{
    // A chain drawn link by link between two points (the Anchoress's anchor chain, her hook's line): alternate links
    // turned a quarter, sagging in the middle, never through the floor.
    public sealed class LinkChain
    {
        readonly Transform[] links;
        readonly float spacing;

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

        public void Draw(Vector3 a, Vector3 b, float sag, float floorY, bool shown)
        {
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
