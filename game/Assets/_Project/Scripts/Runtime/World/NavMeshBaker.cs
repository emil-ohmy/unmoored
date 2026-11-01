using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace MyGame
{
    // The ways the Soured can walk: built from the scene's own colliders when it loads (nothing baked to go stale when
    // the builder rebuilds a scene), for two sizes of Soured: slim (a Clinger, an archer: up a tower's narrow stair)
    // and broad (a Laden, Hold: too wide for it). No walking off a ledge, nothing over water. Rebuilt in the background
    // when the world changes shape (a floor breaking, a gate opening).
    public sealed class NavMeshBaker : MonoBehaviour
    {
        public static NavMeshBaker Instance { get; private set; }

        public float slimRadius = 0.4f, broadRadius = 0.6f; // (broad: a Laden's own width)
        public float agentHeight = 1.8f, agentClimb = 0.32f, agentSlope = 50f;
        public Vector3 extent = new Vector3(220f, 60f, 220f);

        public int SlimAgent => slim.agentTypeID;
        public int BroadAgent => broad.agentTypeID;
        public bool Ready { get; private set; }

        NavMeshBuildSettings slim, broad;
        NavMeshData slimData, broadData;
        NavMeshDataInstance slimInstance, broadInstance;
        AsyncOperation pending;
        bool dirty;
        Bounds bounds;

        void Awake()
        {
            Instance = this;
            slim = Settings(slimRadius);
            broad = Settings(broadRadius);
            bounds = new Bounds(transform.position, extent);
            var sources = Collect();
            slimData = NavMeshBuilder.BuildNavMeshData(slim, sources, bounds, Vector3.zero, Quaternion.identity);
            broadData = NavMeshBuilder.BuildNavMeshData(broad, sources, bounds, Vector3.zero, Quaternion.identity);
            slimInstance = NavMesh.AddNavMeshData(slimData);
            broadInstance = NavMesh.AddNavMeshData(broadData);
            Ready = true;
        }

        NavMeshBuildSettings Settings(float radius)
        {
            var s = NavMesh.CreateSettings(); // (an agent type of its own)
            s.agentRadius = radius;
            s.agentHeight = agentHeight;
            s.agentClimb = agentClimb;
            s.agentSlope = agentSlope;
            s.minRegionArea = 1f;
            s.overrideVoxelSize = true;
            s.voxelSize = radius / 4f; // (fine enough to find a way through a doorway just wide enough)
            return s;
        }

        List<NavMeshBuildSource> Collect()
        {
            var sources = new List<NavMeshBuildSource>();
            NavMeshBuilder.CollectSources(bounds, ~0, NavMeshCollectGeometry.PhysicsColliders, 0, new List<NavMeshBuildMarkup>(), sources);
            // Characters aren't ground, nor is anything you can walk through.
            sources.RemoveAll(s => s.component is CharacterController || s.component is Collider c && (c.isTrigger || !c.enabled)
                                   || s.component != null && s.component.GetComponentInParent<NavMeshIgnore>() != null); // (a trap: it moves)
            return sources;
        }

        // The world changed shape: build again, in the background.
        public void Rebuild()
        {
            if (pending != null && !pending.isDone)
            {
                dirty = true;
                return;
            }
            var sources = Collect();
            NavMeshBuilder.UpdateNavMeshDataAsync(slimData, slim, sources, bounds);
            pending = NavMeshBuilder.UpdateNavMeshDataAsync(broadData, broad, sources, bounds);
        }

        void Update()
        {
            if (dirty && pending != null && pending.isDone)
            {
                dirty = false;
                Rebuild();
            }
        }

        // Which ways a Soured of this size can walk.
        public NavMeshQueryFilter Filter(float radius) =>
            new NavMeshQueryFilter { agentTypeID = radius < (slimRadius + broadRadius) / 2f + 0.05f ? slim.agentTypeID : broad.agentTypeID, areaMask = NavMesh.AllAreas };

        void OnDestroy()
        {
            slimInstance.Remove();
            broadInstance.Remove();
            if (Instance == this)
                Instance = null;
        }
    }
}
