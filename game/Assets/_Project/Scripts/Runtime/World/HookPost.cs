using System.Collections.Generic;
using UnityEngine;

namespace MyGame
{
    // A hook post: an old pile with an iron ring. With the Hook, the Apprentice can pull itself to one within reach
    // (over water too), landing on its platform. The nearest roughly ahead is the one aimed at.
    public sealed class HookPost : MonoBehaviour
    {
        static readonly List<HookPost> all = new List<HookPost>();

        public Transform ring;    // what the tether catches
        public Transform landing; // where the pull sets the Apprentice down

        public Vector3 RingPosition => ring != null ? ring.position : transform.position + Vector3.up * 1.6f;
        public Vector3 LandingPosition => landing != null ? landing.position : transform.position;

        void OnEnable() => all.Add(this);
        void OnDisable() => all.Remove(this);

        // The post to pull to from here: within range, not the one you're standing at, roughly ahead (within
        // `halfAngle` of `facing`), the nearest to straight ahead and close winning, with nothing solid in between.
        public static HookPost Best(Vector3 from, Vector3 facing, float range, float halfAngle)
        {
            HookPost best = null;
            float bestScore = float.MaxValue;
            facing.y = 0f;
            foreach (var post in all)
            {
                Vector3 to = post.LandingPosition - from;
                to.y = 0f;
                float d = to.magnitude;
                if (d > range || d < 1.5f)
                    continue;
                float angle = Vector3.Angle(facing, to);
                if (angle > halfAngle || !ClearLine(from + Vector3.up * 1.0f, post.RingPosition))
                    continue;
                float score = d + angle * 0.05f;
                if (score < bestScore)
                {
                    best = post;
                    bestScore = score;
                }
            }
            return best;
        }

        static bool ClearLine(Vector3 from, Vector3 to)
        {
            Vector3 d = to - from;
            foreach (var hit in Physics.RaycastAll(from, d.normalized, d.magnitude - 0.4f, ~0, QueryTriggerInteraction.Ignore))
                if (!(hit.collider is CharacterController) && hit.collider.GetComponentInParent<HookPost>() == null)
                    return false;
            return true;
        }
    }
}
