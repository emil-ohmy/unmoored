using UnityEngine;

namespace MyGame
{
    // The Laden archer: a Laden that let most of its heap go and kept a bow. It keeps its distance (4-10 m), backing
    // away if you close in (never off an edge), and shoots: a 0.9 s draw that follows you, a green glint as it stops
    // following (the aim is set: move!), then the arrow, straight at where you were. Cornered, it jabs with the bow.
    // No guard and less health than a Laden: the one to rush.
    public sealed class LadenArcher : Soured
    {
        [Header("Archer: the shot")]
        public SouredAttack shot = new SouredAttack
        {
            name = "Shot", windupClip = "Draw", strikeClip = "Release", recoverClip = "ShotRecover",
            telegraph = 0.9f, strike = 0.25f, recover = 0.6f, impactAt = 0.2f,
            lunge = 0f, maxLunge = 0f, trackUntil = 0.67f, damage = 18f, reach = 12f, arc = 180f, knockback = 5f,
            ranged = true, projectileSpeed = 13f,
        };
        public float shootRange = 11f;
        public float shotCooldown = 1.4f;
        public float firstShotDelay = 0.5f; // after it notices you

        [Header("Archer: keeping its distance")]
        public float preferredMin = 4f;
        public float preferredMax = 10f;
        public float retreatSpeed = 1.8f;

        [Header("Arrows")]
        public Material trailMaterial;

        Material wood, tipGlow;
        float nextShot;
        bool sighted, hadSight;
        int sightFrame = -1;
        Transform nocked; // the arrow on the string while it draws: the tell you can see

        public int ShotsFired { get; private set; }

        protected override void Awake()
        {
            base.Awake();
            foreach (var renderer in GetComponentsInChildren<Renderer>())
                foreach (var m in renderer.sharedMaterials)
                {
                    if (m == null)
                        continue;
                    if (m.name.StartsWith("BowWood"))
                        wood = m;
                    else if (m.name.StartsWith("SouredEye_Glow"))
                        tipGlow = m;
                }
        }

        protected override string ChaseClip(float distance) => Mathf.Abs(ChaseSpeed(distance)) > 0.01f ? "Walk" : IdleClip;

        protected override float ChaseSpeed(float distance)
        {
            if (!HasLineOfSight() || distance > preferredMax)
                return chaseSpeed;      // find a clear shot
            return distance < preferredMin ? -retreatSpeed : 0f; // too close: back away (KeepFooting keeps it off edges)
        }

        protected override SouredAttack ChooseAttack(Vector3 toTarget, float distance)
        {
            if (distance <= attackRange)
                return primary; // up close: the bow-jab
            if (distance <= shootRange && Time.time >= nextShot && HasLineOfSight())
                return shot;
            return null;
        }

        protected override void OnEntered(State state)
        {
            if (state == State.Chase && !sighted)
            {
                sighted = true;
                nextShot = Time.time + firstShotDelay;
            }
            if (state == State.Recover && CurrentAttack == shot)
                nextShot = Time.time + shot.recover + shotCooldown;
            if (state == State.Telegraph && CurrentAttack == shot)
                Nock();
            else if (state != State.Strike)
                Unnock(); // an interrupted draw loses its arrow
            if (state == State.Idle)
                sighted = false;
        }

        // The aim is set: a green glint at the arrow's tip. Move now.
        protected override void OnCommitted(SouredAttack attack)
        {
            if (attack == shot)
                Effects.Instance?.AlertFlare(BowPoint() + transform.forward * 0.3f);
        }

        protected override void Shoot(SouredAttack attack, Vector3 direction)
        {
            Unnock();
            ShotsFired++;
            Arrow.Fire(BowPoint(), direction, attack.projectileSpeed, attack.damage, attack.knockback, target, wood, tipGlow, trailMaterial);
        }

        void LateUpdate()
        {
            if (nocked == null)
                return;
            // The arrow rides the bow as it comes up: it glows brighter as the draw goes on.
            nocked.SetPositionAndRotation(BowPoint(), transform.rotation);
            float draw = CurrentState == State.Telegraph ? Mathf.Clamp01(StateTime / shot.telegraph) : 1f;
            nocked.localScale = Vector3.one * Mathf.Lerp(0.6f, 1f, draw);
        }

        Vector3 BowPoint() => transform.position + transform.forward * 0.62f + Vector3.up * 1.03f;

        void Nock()
        {
            Unnock();
            if (wood == null || tipGlow == null)
                return;
            var go = new GameObject("Nocked Arrow");
            nocked = go.transform;
            Piece(nocked, wood, new Vector3(0f, 0f, -0.1f), new Vector3(0.025f, 0.025f, 0.7f));
            Piece(nocked, tipGlow, new Vector3(0f, 0f, 0.28f), new Vector3(0.06f, 0.06f, 0.1f));
        }

        void Unnock()
        {
            if (nocked != null)
                Destroy(nocked.gameObject);
            nocked = null;
        }

        static void Piece(Transform parent, Material material, Vector3 position, Vector3 scale)
        {
            var part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(part.GetComponent<Collider>());
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localScale = scale;
            part.GetComponent<MeshRenderer>().sharedMaterial = material;
        }

        // A clear line from the bow to the Apprentice (walls and crates block it; characters don't). Once a frame.
        bool HasLineOfSight()
        {
            if (sightFrame == Time.frameCount)
                return hadSight;
            sightFrame = Time.frameCount;
            hadSight = false;
            if (target == null)
                return false;
            Vector3 from = BowPoint(), to = target.transform.position + Vector3.up * 0.7f;
            Vector3 d = to - from;
            hadSight = true;
            foreach (var hit in Physics.RaycastAll(from, d.normalized, d.magnitude, ~0, QueryTriggerInteraction.Ignore))
                if (!(hit.collider is CharacterController))
                {
                    hadSight = false;
                    break;
                }
            return hadSight;
        }

        void OnDestroy() => Unnock();
    }
}
