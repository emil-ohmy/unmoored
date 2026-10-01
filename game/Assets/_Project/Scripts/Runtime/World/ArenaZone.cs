using System.Collections.Generic;
using UnityEngine;

namespace MyGame
{
    // A Boss's arena, as the other Soured know it: none of them cross into it. Whatever follows the Apprentice there
    // stops at its edge and gives them up (they go back to their posts); none notices them while they're inside.
    // Only those that belong there (that stand in it from the start: the Boss, the scarecrows) move in it.
    public sealed class ArenaZone : MonoBehaviour
    {
        public Vector3 size = new Vector3(20f, 8f, 20f); // a box, centred on this transform, axis-aligned

        static readonly List<ArenaZone> zones = new List<ArenaZone>();

        void OnEnable() => zones.Add(this);
        void OnDisable() => zones.Remove(this);

        public bool Contains(Vector3 at) => new Bounds(transform.position, size).Contains(at);

        // Whether `at` is in an arena `soured` doesn't belong to (where it's from: `home`).
        public static bool KeepsOut(Vector3 home, Vector3 at)
        {
            foreach (var z in zones)
                if (z.Contains(at) && !z.Contains(home))
                    return true;
            return false;
        }
    }
}
