using System.Collections.Generic;
using UnityEngine;

namespace MyGame
{
    // A pocket of time in the stopped Hall: while its machine runs (set going a while by the Stroke), time comes back in
    // a ring round it, `radius` across its floor (and a storey's height up and down, no more): the colour returns there
    // (the shaders, through HallTime), and what's held in the stillness can be touched there (the Widow Hale can be
    // hurt). It swells as the machine spins up and shrinks as it winds down. Its edge drawn faintly on the floor, so you
    // can see where time reaches.
    public sealed class TimePocket : MonoBehaviour
    {
        public const int Most = 4; // (the shaders' arrays)
        public const float Height = 4f; // up and down from its centre: time reaches no further

        public Clockwork clockwork;
        public float radius = 3.6f;

        static readonly List<TimePocket> pockets = new List<TimePocket>();
        public static IReadOnlyList<TimePocket> All => pockets;

        // How far time reaches now (0: none; its machine still).
        public float Reach => clockwork != null && clockwork.Run > 0f ? radius * UiKit.Smooth(0f, 1f, clockwork.Run) : 0f;
        public Vector3 Centre => transform.position;

        void OnEnable() => pockets.Add(this);
        void OnDisable() => pockets.Remove(this);

        // Whether `point` is in time here (within its reach, with a little margin inside its soft edge).
        public bool Holds(Vector3 point, float margin = 0.2f)
        {
            float reach = Reach;
            if (reach <= margin)
                return false;
            Vector3 d = point - Centre;
            if (Mathf.Abs(d.y) > Height)
                return false;
            d.y = 0f;
            return d.magnitude < reach - margin;
        }

        // Whether `point` is in any pocket of time now.
        public static bool InAny(Vector3 point, float margin = 0.2f)
        {
            foreach (var p in pockets)
                if (p != null && p.Holds(point, margin))
                    return true;
            return false;
        }
    }
}
