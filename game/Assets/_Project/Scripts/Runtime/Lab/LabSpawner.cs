using System;
using System.Collections.Generic;
using UnityEngine;

namespace MyGame
{
    // The lab's spawner: a copy of any kind of Soured, set down a few metres in front of the Apprentice (facing them),
    // as many as asked, aggressive (it knows you're there) or idle (a dummy that never fights back). The kinds are
    // templates kept switched off in the scene (MainSceneBuilder.Lab), so every copy is built exactly as in the world.
    public sealed class LabSpawner : MonoBehaviour
    {
        [Serializable]
        public sealed class Kind
        {
            public string name;
            public GameObject template; // inactive; its dropped lantern (if any) inactive too
        }

        public Kind[] kinds = new Kind[0];
        public float ahead = 4f, spacing = 1.8f;

        public static readonly int[] Counts = { 1, 2, 3, 5 };
        public int Count { get; set; } = 1;
        public bool Aggressive { get; set; } = true;
        public IReadOnlyList<Soured> Spawned => spawned;

        readonly List<Soured> spawned = new List<Soured>();

        public void NextCount() => Count = Counts[(Array.IndexOf(Counts, Count) + 1) % Counts.Length];

        public void Spawn(int kind, Apprentice near)
        {
            var template = kinds[kind].template;
            Vector3 forward = Flat(near.transform.forward).normalized;
            if (forward.sqrMagnitude < 0.01f)
                forward = Vector3.forward;
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            for (int i = 0; i < Count; i++)
            {
                Vector3 spot = near.transform.position + forward * ahead + right * ((i - (Count - 1) * 0.5f) * spacing);
                spot = Ground(spot, near.transform.position);
                var go = Instantiate(template, spot, template.transform.rotation);
                go.name = kinds[kind].name + (Count > 1 ? $" {spawned.Count + 1}" : "");
                // Face the Apprentice (turning about the vertical only: a rigged model's root keeps its own rotation).
                float yaw = Vector3.SignedAngle(Flat(go.transform.forward), -forward, Vector3.up);
                go.transform.rotation = Quaternion.Euler(0f, yaw, 0f) * go.transform.rotation;
                var soured = go.GetComponent<Soured>();
                // Its dropped lantern lives outside it: a copy of its own, carrying this one's light.
                if (soured.droppedLantern != null)
                {
                    var lantern = Instantiate(soured.droppedLantern);
                    lantern.carried = go.GetComponentInChildren<Light>(true);
                    lantern.gameObject.SetActive(true);
                    soured.droppedLantern = lantern;
                }
                soured.passive = !Aggressive;
                if (Aggressive)
                    soured.aggroRadius = Mathf.Max(soured.aggroRadius, ahead + 6f); // it knows you're there
                go.SetActive(true);
                spawned.Add(soured);
            }
        }

        public void ClearSpawned()
        {
            foreach (var s in spawned)
            {
                if (s == null)
                    continue;
                if (s.droppedLantern != null)
                    Destroy(s.droppedLantern.gameObject);
                Destroy(s.gameObject);
            }
            spawned.Clear();
        }

        // Standing ground where asked, or back towards the Apprentice until there is some (never in the water).
        static Vector3 Ground(Vector3 spot, Vector3 from)
        {
            for (float t = 0f; t <= 1f; t += 0.1f)
            {
                Vector3 p = Vector3.Lerp(spot, from, t);
                if (Physics.Raycast(p + Vector3.up * 3f, Vector3.down, out var hit, 6f, ~0, QueryTriggerInteraction.Ignore)
                    && !(hit.collider is CharacterController) && hit.point.y > 0f)
                    return hit.point;
            }
            return from;
        }

        static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);
    }
}
