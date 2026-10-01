using UnityEngine;

namespace MyGame
{
    // Leaves fading ghost images of the Apprentice while its dash is invulnerable, so the i-frames are visible.
    [RequireComponent(typeof(Apprentice))]
    public sealed class DashGhosts : MonoBehaviour
    {
        public Material ghostMaterial;
        public float interval = 0.05f;
        public float lifetime = 0.3f;
        public Color colour = new Color(0.78f, 0.9f, 1f, 0.55f);

        const int PoolSize = 8;

        struct Ghost
        {
            public GameObject Go;
            public Mesh Mesh;
            public MeshRenderer Renderer;
            public float Born;
        }

        Apprentice apprentice;
        SkinnedMeshRenderer skinned;
        Ghost[] pool;
        MaterialPropertyBlock block;
        int next;
        float lastSpawn = -10f;

        void Awake()
        {
            apprentice = GetComponent<Apprentice>();
            skinned = GetComponentInChildren<SkinnedMeshRenderer>();
            block = new MaterialPropertyBlock();
            pool = new Ghost[PoolSize];
            var materials = new Material[skinned.sharedMaterials.Length];
            for (int i = 0; i < materials.Length; i++)
                materials[i] = ghostMaterial;
            for (int i = 0; i < PoolSize; i++)
            {
                var go = new GameObject("Dash Ghost", typeof(MeshFilter), typeof(MeshRenderer));
                go.SetActive(false);
                var mesh = new Mesh { name = "Dash Ghost" };
                go.GetComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = go.GetComponent<MeshRenderer>();
                renderer.sharedMaterials = materials;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                pool[i] = new Ghost { Go = go, Mesh = mesh, Renderer = renderer, Born = -10f };
            }
        }

        void OnDestroy()
        {
            if (pool == null)
                return;
            foreach (var ghost in pool)
                if (ghost.Go != null)
                    Destroy(ghost.Go);
        }

        void Update()
        {
            bool inIFrames = apprentice.CurrentState == Apprentice.State.Dashing && apprentice.Invulnerable;
            if (inIFrames && ghostMaterial != null && Time.time - lastSpawn >= interval)
                Spawn();

            for (int i = 0; i < pool.Length; i++)
            {
                if (!pool[i].Go.activeSelf)
                    continue;
                float age = (Time.time - pool[i].Born) / lifetime;
                if (age >= 1f)
                {
                    pool[i].Go.SetActive(false);
                    continue;
                }
                block.SetColor("_Color", new Color(colour.r, colour.g, colour.b, colour.a * (1f - age)));
                pool[i].Renderer.SetPropertyBlock(block);
            }
        }

        void Spawn()
        {
            lastSpawn = Time.time;
            ref Ghost ghost = ref pool[next];
            next = (next + 1) % pool.Length;
            skinned.BakeMesh(ghost.Mesh, true);
            OarLook.HideOthers(ghost.Mesh, skinned.sharedMaterials, apprentice.Equipment.OarStep);
            ghost.Go.transform.SetPositionAndRotation(skinned.transform.position, skinned.transform.rotation);
            ghost.Born = Time.time;
            ghost.Go.SetActive(true);
        }
    }
}
