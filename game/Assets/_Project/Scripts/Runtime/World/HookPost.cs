using System.Collections.Generic;
using UnityEngine;

namespace MyGame
{
    // A hook post: an old pile with an iron ring. With the Hook, the Apprentice can pull itself to one within reach
    // (over water too), landing on its platform. The nearest roughly ahead, on other ground, is the one aimed at.
    public sealed class HookPost : MonoBehaviour
    {
        static readonly List<HookPost> all = new List<HookPost>();

        public Transform ring;    // what the tether catches
        public Transform landing; // where the pull sets the Apprentice down

        public Vector3 RingPosition => ring != null ? ring.position : transform.position + Vector3.up * 1.6f;
        public Vector3 LandingPosition => landing != null ? landing.position : transform.position;

        void OnEnable() => all.Add(this);
        void OnDisable() => all.Remove(this);

        // The post to pull to from here: within range, on other ground than you stand on (never a post on your own
        // platform), roughly ahead (within `halfAngle` of `facing`), the nearest to straight ahead and close winning,
        // with nothing solid in between.
        public static HookPost Best(Vector3 from, Vector3 facing, float range, float halfAngle)
        {
            HookPost best = null;
            float bestScore = float.MaxValue;
            facing.y = 0f;
            var standingOn = GroundUnder(from);
            foreach (var post in all)
            {
                if (standingOn != null && post.LandingGround == standingOn)
                    continue;
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

        Collider landingGround;
        bool landingGroundFound;

        // What the landing spot stands on (found once: platforms don't move).
        Collider LandingGround
        {
            get
            {
                if (!landingGroundFound)
                {
                    landingGround = GroundUnder(LandingPosition);
                    landingGroundFound = true;
                }
                return landingGround;
            }
        }

        static Collider GroundUnder(Vector3 at)
        {
            Collider ground = null;
            float nearest = float.MaxValue;
            foreach (var hit in Physics.RaycastAll(at + Vector3.up * 0.5f, Vector3.down, 2f, ~0, QueryTriggerInteraction.Ignore))
                if (!(hit.collider is CharacterController) && hit.collider.GetComponentInParent<HookPost>() == null && hit.distance < nearest)
                {
                    nearest = hit.distance;
                    ground = hit.collider;
                }
            return ground;
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
