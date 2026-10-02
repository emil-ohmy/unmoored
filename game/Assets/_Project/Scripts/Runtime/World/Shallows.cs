using System.Collections.Generic;
using UnityEngine;

namespace MyGame
{
    // Shallow water you can wade in (the Glasshouse's channels): no drowning in it, and it puts out the flames of
    // anyone alight who steps in.
    public sealed class Shallows : MonoBehaviour
    {
        public Vector3 size = new Vector3(4f, 1f, 1f); // a box, centred on this transform, axis-aligned

        static readonly List<Shallows> all = new List<Shallows>();

        void OnEnable() => all.Add(this);
        void OnDisable() => all.Remove(this);

        public static bool In(Vector3 at)
        {
            foreach (var s in all)
                if (new Bounds(s.transform.position, s.size).Contains(at))
                    return true;
            return false;
        }
    }
}
