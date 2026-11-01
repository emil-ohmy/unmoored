using UnityEngine;

namespace MyGame
{
    // The Laden archer: a Laden that let most of its heap go and kept a bow. It keeps its distance (4-10 m), backing
    // away if you close in (never off an edge), and shoots: a 0.9 s draw that follows you, a green glint as it stops
    // following (the aim is set: move!), then the arrow, straight at where you were. Sometimes a volley instead: a
    // longer draw with three arrows fanned on the bow, then three in quick succession (each turning a little after
    // you), and a long recovery. Cornered, it jabs with the bow. No guard and less health than a Laden: the one to
    // rush. A perfectly blocked arrow flies back and stuns it.
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
        public SouredAttack volley = new SouredAttack
        {
            name = "Volley", windupClip = "Draw", strikeClip = "Release", recoverClip = "ShotRecover",
            telegraph = 1.3f, strike = 0.6f, recover = 1.0f, impactAt = 0.1f,
            lunge = 0f, maxLunge = 0f, trackUntil = 0.75f, damage = 14f, reach = 12f, arc = 180f, knockback = 4f,
            ranged = true, projectileSpeed = 13f, volley = 3, volleyInterval = 0.2f, volleyTrack = 6f,
        };
        [Range(0f, 1f)] public float volleyChance = 0.35f; // of a shot from 5 m or more
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
        Transform nocked; // the arrow(s) on the string while it draws: the tell you can see
        Light charge;     // their glow, swelling with the draw (brighter for a volley)
        int nockedCount;

        public int ShotsFired { get; private set; }
        public int NockedCount => nocked != null ? nocked.GetComponentsInChildren<MeshRenderer>().Length / 2 : 0;

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
                return distance >= 5f && Random.value < volleyChance ? volley : shot;
            return null;
        }

        protected override void OnEntered(State state)
        {
            if (state == State.Chase && !sighted)
            {
                sighted = true;
                nextShot = Time.time + firstShotDelay;
            }
            bool ranged = CurrentAttack == shot || CurrentAttack == volley;
            if (state == State.Recover && ranged)
                nextShot = Time.time + CurrentAttack.recover + shotCooldown;
            if (state == State.Telegraph && ranged)
                Nock(CurrentAttack.volley);
            else if (state != State.Strike)
                Unnock(); // an interrupted draw loses its arrow
            if (state == State.Idle)
                sighted = false;
        }

        // The aim is set: a green glint at the arrow's tip. Move now.
        protected override void OnCommitted(SouredAttack attack)
        {
            if (attack.ranged)
                Effects.Instance?.AlertFlare(BowPoint() + transform.forward * 0.3f);
        }

        protected override void Shoot(SouredAttack attack, Vector3 direction, int index)
        {
            ShotsFired++;
            if (index + 1 >= attack.volley)
                Unnock();
            else if (nocked != null && nocked.childCount > 0)
                Destroy(nocked.GetChild(nocked.childCount - 1).gameObject); // one fewer on the bow
            if (index > 0)
                clips.PlayOnce(attack.strikeClip, attack.volleyInterval * 0.9f); // the snap of each release
            // Along its aim (sidestepping still escapes it), rising or falling to where the Apprentice is: up on a
            // walkway, down in a yard. Loosed from the bow (held out to its side as it draws) to the point on its aim
            // line as far off as you are, so the side the bow is on doesn't carry it past you.
            Vector3 from = BowPoint(), chest = target.transform.position + Vector3.up * 0.7f;
            Vector3 flat = new Vector3(chest.x - transform.position.x, 0f, chest.z - transform.position.z);
            Vector3 aimed = transform.position + new Vector3(direction.x, 0f, direction.z).normalized * Mathf.Max(0.5f, flat.magnitude);
            aimed.y = chest.y;
            Vector3 aim = aimed - from;
            Arrow.Fire(from, aim, attack.projectileSpeed, attack.damage, attack.knockback, target, this, wood, tipGlow, trailMaterial);
        }

        void LateUpdate()
        {
            if (nocked == null)
                return;
            // The arrow rides the bow as it comes up: it glows brighter as the draw goes on.
            nocked.SetPositionAndRotation(BowPoint(), transform.rotation);
            float draw = CurrentState == State.Telegraph && CurrentAttack != null ? Mathf.Clamp01(StateTime / CurrentAttack.telegraph) : 1f;
            nocked.localScale = Vector3.one * Mathf.Lerp(0.6f, 1f, draw);
            if (charge != null)
                charge.intensity = draw * draw * (nockedCount > 1 ? 2.6f : 1.1f);
        }

        // Where the arrow sits: on the bow (its bone's head is the grip), wherever the clips put it; without one, where
        // a drawn bow would be.
        Vector3 BowPoint()
        {
            if (bow == null && !lookedForBow)
            {
                lookedForBow = true;
                foreach (var t in GetComponentsInChildren<Transform>())
                    if (t.name == "Bow")
                        bow = t;
            }
            return bow != null ? bow.position : transform.position + transform.forward * 0.62f + Vector3.up * 1.03f;
        }

        Transform bow;
        bool lookedForBow;

        // The arrow(s) on the string while it draws: one, or three fanned out for a volley (the tell you can see).
        void Nock(int count)
        {
            Unnock();
            if (wood == null || tipGlow == null)
                return;
            nocked = new GameObject("Nocked Arrows").transform;
            nockedCount = count;
            charge = new GameObject("Draw Glow").AddComponent<Light>();
            charge.transform.SetParent(nocked, false);
            charge.transform.localPosition = new Vector3(0f, 0.1f, 0.2f);
            charge.type = LightType.Point;
            charge.color = new Color(0.62f, 0.91f, 0.38f);
            charge.range = count > 1 ? 3.2f : 2.4f;
            charge.intensity = 0f;
            charge.shadows = LightShadows.None;
            for (int i = 0; i < count; i++)
            {
                var arrow = new GameObject("Nocked Arrow").transform;
                arrow.SetParent(nocked, false);
                float fan = count > 1 ? (i - (count - 1) * 0.5f) * 24f : 0f;
                arrow.localRotation = Quaternion.Euler(0f, fan, 0f);
                arrow.localPosition = new Vector3(0f, i * 0.03f, 0f);
                Piece(arrow, wood, new Vector3(0f, 0f, -0.1f), new Vector3(0.025f, 0.025f, 0.7f));
                Piece(arrow, tipGlow, new Vector3(0f, 0f, 0.28f), new Vector3(0.06f, 0.06f, 0.1f));
            }
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
