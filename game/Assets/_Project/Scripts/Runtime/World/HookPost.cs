using System.Collections.Generic;
using UnityEngine;

namespace MyGame
{
    // A hook post: an old pile with an iron ring. With the Hook, the Apprentice can pull itself to one within reach
    // (over water too), landing on its platform. The nearest roughly ahead, on other ground, is the one aimed at.
    // Before (and after) the Hook it never glows; its wet iron ring just catches the moonlight in a tiny glint now and
    // then while you're near and it's on screen, so it gets noticed and remembered.
    public sealed class HookPost : MonoBehaviour
    {
        static readonly List<HookPost> all = new List<HookPost>();

        public Transform ring;    // what the tether catches
        public Transform landing; // where the pull sets the Apprentice down

        public Vector3 RingPosition => ring != null ? ring.position : transform.position + Vector3.up * 1.6f;
        public Vector3 LandingPosition => landing != null ? landing.position : transform.position;

        [Header("Moon glint")]
        public float glintRange = 11f;
        public Vector2 glintEvery = new Vector2(1.5f, 3.5f); // seconds between glints, while near and on screen

        public int Glints { get; private set; }

        Transform walker;
        float waited, nextGlint;

        void OnEnable() => all.Add(this);
        void OnDisable() => all.Remove(this);

        void Start()
        {
            var apprentice = FindAnyObjectByType<Apprentice>();
            walker = apprentice != null ? apprentice.transform : null;
            nextGlint = Random.Range(glintEvery.x * 0.4f, glintEvery.y * 0.5f); // the first one comes soon
        }

        // Now and then, while you're near it and it's on screen.
        void Update()
        {
            if (walker == null || Vector3.Distance(walker.position, RingPosition) > glintRange)
                return;
            waited += Time.deltaTime;
            if (waited < nextGlint || !OnScreen())
                return;
            waited = 0f;
            nextGlint = Random.Range(glintEvery.x, glintEvery.y);
            Glints++;
            Effects.Instance?.MoonGlint(GlintPosition);
        }

        // On the ring's top, where the moon would catch it.
        public Vector3 GlintPosition => RingPosition + Vector3.up * 0.28f;

        // The glint itself must be in view (near a tall post its ring is often above the top of the screen).
        bool OnScreen()
        {
            var cam = Camera.main;
            if (cam == null)
                return false;
            Vector3 v = cam.WorldToViewportPoint(GlintPosition);
            return v.z > 0f && v.x > 0.04f && v.x < 0.96f && v.y > 0.04f && v.y < 0.96f;
        }

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
