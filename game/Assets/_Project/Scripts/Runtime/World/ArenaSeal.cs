using UnityEngine;

namespace MyGame
{
    // A Boss arena shut while its fight is on. The Anchoress's: as she wakes, chains rise dripping out of the water
    // behind you and stretch across the way in, from pillar to pillar, rattling taut (she has anchored it shut); an
    // unseen wall holds you in behind them. When the fight ends (you fall, or she's beaten) they slacken and sink.
    public sealed class ArenaSeal : MonoBehaviour
    {
        public Transform left, right;          // the pillars the chains run between
        public float[] heights = { 0.35f, 0.8f, 1.25f };
        public float riseTime = 0.9f, sinkTime = 1.2f;
        public Material linkMaterial;
        public AudioClip[] riseSound, sinkSound;

        public bool Sealed { get; private set; }
        public float Raised => raised; // 0 under the water, 1 taut across the way in

        IBoss boss;
        BoxCollider wall;
        LinkChain[] chains;
        float raised;

        void Start()
        {
            boss = FindBoss();
            chains = new LinkChain[heights.Length];
            for (int i = 0; i < chains.Length; i++)
                chains[i] = new LinkChain("Seal Chain", linkMaterial, 72, new Vector3(0.05f, 0.1f, 0.18f), 0.15f);
            // The wall: across the way in, from pillar to pillar, taller than anyone.
            wall = gameObject.AddComponent<BoxCollider>();
            Vector3 a = left.position, b = right.position;
            transform.SetPositionAndRotation((a + b) * 0.5f, Quaternion.LookRotation(Vector3.Cross(Vector3.up, b - a)));
            wall.size = new Vector3(Vector3.Distance(a, b), 3f, 0.4f);
            wall.center = Vector3.up * 1.5f;
            wall.enabled = false;
        }

        static IBoss FindBoss()
        {
            foreach (var b in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
                if (b is IBoss boss)
                    return boss;
            return null;
        }

        void Update()
        {
            bool fight = boss != null && BossFight.Current == boss;
            if (fight != Sealed)
            {
                Sealed = fight;
                wall.enabled = fight;
                Vector3 mid = (left.position + right.position) * 0.5f;
                Sfx.Instance?.Attack(fight ? riseSound : sinkSound, mid + Vector3.up, 1f, fight ? "seal_rise" : "seal_sink");
                if (fight)
                {
                    CameraRig.Instance?.Shake(0.15f);
                    for (int i = 0; i < 5; i++) // bursting up out of the water
                        Effects.Instance?.Splash(Vector3.Lerp(left.position, right.position, (i + 0.5f) / 5f) + Vector3.down * 0.4f);
                }
            }
            raised = Mathf.MoveTowards(raised, Sealed ? 1f : 0f, Time.deltaTime / (Sealed ? riseTime : sinkTime));
            Draw();
        }

        void Draw()
        {
            float r = UiKit.Smooth(0f, 1f, raised);
            for (int i = 0; i < chains.Length; i++)
            {
                // Out of the water at the pillars' feet, up to its height; slack while it rises, taut once up.
                float h = Mathf.Lerp(-0.9f, heights[i], r);
                Vector3 a = left.position + Vector3.up * h, b = right.position + Vector3.up * h;
                chains[i].Draw(a, b, Mathf.Lerp(0.9f, 0.12f, r), -5f, raised > 0.01f);
            }
        }

        void OnDestroy()
        {
            if (chains == null)
                return;
            foreach (var c in chains)
                c?.Destroy();
        }
    }
}
