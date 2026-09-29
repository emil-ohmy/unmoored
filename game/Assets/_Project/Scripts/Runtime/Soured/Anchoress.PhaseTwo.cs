using UnityEngine;

namespace MyGame
{
    // The Anchoress's second phase. At half health she rises: nothing gets through while she lifts the anchor to the
    // sky and plants it, a ring of water throws you back, and the chapel floor starts to break. From then on:
    //   Her Drops break the floor where they land: the section shudders (get off it) and gives way into the water.
    //     Anyone on it falls in, her too: she hauls herself out hurt and staggered, so baiting her onto a crumbling
    //     section is a way to hurt her. Her middle and the rim never break.
    //   Hook Yank: she plants the anchor and whirls the Ferryman's hook overhead on its line (cold blue, his colour),
    //     then throws it straight at you. Caught, you're dragged in and set down in front of her, and a quick Drop
    //     follows. Dash through it, or perfect-block it (the hook glances off and she's stunned); a normal block still
    //     drags you part of the way, with no follow-up.
    public sealed partial class Anchoress
    {
        public enum HookMode { AtWaist, Whirling, Flying, Caught, Returning }

        [Header("Phase two: at half health she rises, her Drops break the floor, and she throws the Ferryman's hook")]
        [Range(0f, 1f)] public float phaseTwoAt = 0.5f;
        public float riseDuration = 2.2f;
        [Range(0f, 1f)] public float riseSlamAt = 0.66f; // when the anchor's planted (the Rise clip)
        public float riseRingDamage = 8f;
        public float riseRingRadius = 7f;
        public float riseRingKnockback = 10f;
        public int riseBreaks = 2;
        [Range(0f, 0.3f)] public float surfaceDamage = 0.08f; // of her health, when she falls in and hauls herself out
        public ChapelFloor floor;
        public AudioClip[] riseSound;

        [Header("Hook Yank")]
        public SouredAttack yank = new SouredAttack
        {
            name = "Yank", windupClip = "YankWindup", strikeClip = "Yank", recoverClip = "YankRecover",
            telegraph = 1.1f, strike = 0.6f, recover = 0.9f, impactAt = 0f,
            lunge = 0f, maxLunge = 0f, trackUntil = 0.8f, damage = 10f, reach = 9.5f, arc = 180f, knockback = 0f, scripted = true,
        };
        public float yankMin = 3.5f, yankMax = 9f;
        public float yankCooldown = 6f;
        public float hookSpeed = 18f;
        public float hookHitRadius = 0.8f;
        public float yankLandDistance = 2.2f;  // where the pull sets you down, in front of her
        public float followUpTelegraph = 0.75f; // the Drop after a caught yank winds up quicker
        public GameObject hookModel;            // the hook's head in flight
        public Transform hookBone;              // the one at her waist, hidden while it's out
        public Transform hookHand;              // where its line leaves her hands
        public AudioClip[] hookCatchSound;

        const float HookReturnTime = 0.35f;
        static readonly Color HookGlow = new Color(0.62f, 0.77f, 1f); // the Ferryman's cold blue

        public bool PhaseTwo { get; private set; }
        public HookMode Hook { get; private set; }
        public Vector3 HookPosition => hookObject != null ? hookObject.position : transform.position;
        public int Yanked { get; private set; }   // times the hook caught the Apprentice
        public int Surfaced { get; private set; } // times she fell in and hauled herself out

        SouredAttack quickDrop;
        Transform hookObject;
        Quaternion hookModelRotation = Quaternion.identity;
        LinkChain hookLine;
        Vector3 hookAt, hookDir, returnFrom;
        float hookTime, hookTravelled, nextYank;
        bool riseSlammed, followUpPending;

        protected override bool Unhittable => CurrentState == State.Busy;

        bool IsDrop(SouredAttack attack) => attack != null && (attack == primary || attack == quickDrop);

        SouredAttack QuickDrop
        {
            get
            {
                if (quickDrop == null)
                {
                    quickDrop = primary.Copy();
                    quickDrop.telegraph = followUpTelegraph;
                }
                return quickDrop;
            }
        }

        bool YankReady(float distance) => PhaseTwo && distance >= yankMin && distance <= yankMax && Time.time >= nextYank;

        // After a caught yank, you land in front of her: the quick Drop, if you're still there.
        SouredAttack FollowUp(float distance, float angle)
        {
            if (!followUpPending)
                return null;
            followUpPending = false;
            return distance <= attackRange + 1f && angle <= backAngle ? QuickDrop : null;
        }

        void PhaseTwoEntered(State state)
        {
            if (state == State.Telegraph && CurrentAttack == yank)
            {
                Hook = HookMode.Whirling;
                hookTime = 0f;
                ShowHook(true);
            }
            else if (state == State.Strike && CurrentAttack == yank && Hook == HookMode.Whirling)
            {
                // Thrown from where it whirls, straight along her aim.
                Hook = HookMode.Flying;
                hookTime = hookTravelled = 0f;
                hookAt = HookPosition;
                hookDir = LockedDirection.sqrMagnitude > 0.01f ? LockedDirection : transform.forward;
            }
            else if (state == State.Dying)
            {
                Hook = HookMode.AtWaist;
                ShowHook(false);
            }
            else if (Hook == HookMode.Whirling || Hook == HookMode.Flying)
                ReturnHook(); // interrupted, or it flew its length: back it comes
            if (state == State.Recover && CurrentAttack == yank)
                nextYank = Time.time + yank.recover + yankCooldown;
        }

        void PhaseTwoTick(float dt)
        {
            if (!PhaseTwo && IsAlive && HealthFraction <= phaseTwoAt && Anchor == AnchorMode.Held
                && (CurrentState == State.Chase || CurrentState == State.Recover))
                Rise();
            if (CurrentState == State.Busy && PhaseTwo && !riseSlammed && StateTime >= riseDuration * riseSlamAt)
                RiseSlam();

            hookTime += dt;
            switch (Hook)
            {
                case HookMode.Flying:
                    float step = hookSpeed * dt;
                    hookAt += hookDir * step;
                    hookAt.y = Mathf.MoveTowards(hookAt.y, transform.position.y + 0.9f, 4f * dt); // down to chest height
                    hookTravelled += step;
                    if (target != null && target.IsAlive)
                    {
                        Vector3 to = target.transform.position - hookAt;
                        if (Flat(to).magnitude <= hookHitRadius && Mathf.Abs(to.y + 0.8f) < 1.4f)
                            Catch();
                    }
                    if (Hook == HookMode.Flying && hookTravelled >= yankMax + 0.5f)
                        ReturnHook();
                    break;
                case HookMode.Caught:
                    if (hookTime >= HookReturnTime)
                        ReturnHook();
                    break;
                case HookMode.Returning:
                    if (hookTime >= HookReturnTime)
                    {
                        Hook = HookMode.AtWaist;
                        ShowHook(false);
                    }
                    break;
            }
        }

        // The hook reaches the Apprentice: dragged in to land in front of her (knockback eases off at 10/s, so this
        // much sets you down about there).
        void Catch()
        {
            Vector3 toHer = Flat(transform.position - target.transform.position);
            Vector3 pull = toHer.normalized * (10f * Mathf.Max(0f, toHer.magnitude - yankLandDistance));
            var result = HitTarget(yank.damage, pull);
            if (result == Defence.Dodged)
                return; // through the i-frames: it flies on
            Sfx.Instance?.Attack(hookCatchSound, target.transform.position + Vector3.up, 1f, "yank_catch");
            if (result == Defence.Parried)
            {
                Stun(); // it glances off the oar; the line snaps taut in her hands
                return;
            }
            Yanked++;
            Hook = HookMode.Caught;
            hookTime = 0f;
            followUpPending = result == Defence.Taken;
        }

        void ReturnHook()
        {
            if (Hook == HookMode.AtWaist || Hook == HookMode.Returning)
                return;
            returnFrom = HookPosition;
            Hook = HookMode.Returning;
            hookTime = 0f;
        }

        // Half her health gone: she rises. Nothing gets through meanwhile.
        void Rise()
        {
            PhaseTwo = true;
            riseSlammed = followUpPending = false;
            nextYank = Time.time + riseDuration + 1f;
            BeBusy("Rise", riseDuration);
            Sfx.Instance?.Attack(riseSound, transform.position + Vector3.up * 2f, 1f, "rise", 0.8f);
        }

        // The anchor planted with all her weight: a ring of water throws everyone back, and the floor starts to break
        // (away from the Apprentice: a warning, not a trap).
        void RiseSlam()
        {
            riseSlammed = true;
            Vector3 at = transform.position + Flat(transform.forward).normalized * 1.1f;
            LastShockwave = Shockwave.Spread(at, target, riseRingRadius, ringSpeed, ringWidth, riseRingDamage, riseRingKnockback, ringTint);
            Effects.Instance?.Splash(at + Vector3.up * 0.1f);
            Sfx.Instance?.Attack(primary.impactSound, at, 1f, "rise_slam", 0.8f);
            CameraRig.Instance?.Shake(0.45f);
            floor?.BreakAwayFrom(target != null ? target.transform.position : at, riseBreaks);
        }

        // Fallen through the floor (or knocked in): a Boss doesn't drown. She hauls herself back out onto her middle,
        // hurt, and is open while she recovers.
        protected override bool Surface()
        {
            Surfaced++;
            Vector3 surface = new Vector3(transform.position.x, waterLevel + 0.1f, transform.position.z);
            Effects.Instance?.Splash(surface);
            Sfx.Instance?.Splash(surface);
            LoseHealth(maxHealth * surfaceDamage);
            SetMode(AnchorMode.Held);
            Hook = HookMode.AtWaist;
            ShowHook(false);
            PlaceAt(SpawnPosition);
            StaggerNow();
            return true;
        }

        void ResetPhaseTwo()
        {
            PhaseTwo = riseSlammed = followUpPending = false;
            Hook = HookMode.AtWaist;
            ShowHook(false);
        }

        // ------------------------------------------------------------ the hook, posed after the animation

        void PoseHook()
        {
            if (hookBone != null)
                hookBone.localScale = Hook == HookMode.AtWaist || !IsAlive ? Vector3.one : Vector3.zero;
            if (hookObject == null || !hookObject.gameObject.activeSelf)
            {
                hookLine?.Draw(Vector3.zero, Vector3.zero, 0f, 0f, false);
                return;
            }
            Vector3 hand = hookHand != null ? hookHand.position : transform.position + Vector3.up * 1.2f;
            Vector3 at, facing;
            switch (Hook)
            {
                case HookMode.Whirling:
                {
                    float a = hookTime * 13f, r = Mathf.Min(1f, hookTime * 3f) * 0.95f;
                    var round = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                    at = transform.position + Vector3.up * 2.9f + round * r;
                    facing = Vector3.Cross(Vector3.up, round);
                    break;
                }
                case HookMode.Flying:
                    at = hookAt;
                    facing = hookDir;
                    break;
                case HookMode.Caught:
                    at = target != null ? target.transform.position + Vector3.up * 0.8f : hookAt;
                    facing = Flat(at - transform.position);
                    break;
                default: // Returning
                    at = Vector3.Lerp(returnFrom, hand, UiKit.Smooth(0f, 1f, hookTime / HookReturnTime));
                    facing = Flat(at - transform.position);
                    break;
            }
            hookObject.SetPositionAndRotation(at, Quaternion.LookRotation(facing.sqrMagnitude > 1e-4f ? facing.normalized : transform.forward) * hookModelRotation);
            hookLine ??= new LinkChain("Hook Line", chainMaterial, 72, new Vector3(0.035f, 0.07f, 0.12f), 0.11f);
            hookLine.Draw(hand, at, Hook == HookMode.Whirling ? 0.05f : 0.12f, transform.position.y + 0.04f, true);
        }

        // The hook in flight: the Ferryman's, so it glows his cold blue (the thing you've come for).
        void ShowHook(bool shown)
        {
            if (hookObject == null)
            {
                if (!shown || hookModel == null)
                    return;
                var go = Instantiate(hookModel);
                go.name = "Ferryman's Hook (thrown)";
                hookModelRotation = go.transform.localRotation;
                go.transform.localScale = Vector3.one * 1.3f;
                foreach (var c in go.GetComponentsInChildren<Collider>())
                    Destroy(c);
                var glow = new MaterialPropertyBlock();
                glow.SetColor("_EmissionColor", HookGlow * 1.6f);
                foreach (var r in go.GetComponentsInChildren<Renderer>())
                    r.SetPropertyBlock(glow);
                var light = new GameObject("Hook Glow").AddComponent<Light>();
                light.transform.SetParent(go.transform, false);
                light.type = LightType.Point;
                light.color = HookGlow;
                light.range = 3.5f;
                light.intensity = 2f;
                light.shadows = LightShadows.None;
                hookObject = go.transform;
            }
            hookObject.gameObject.SetActive(shown);
        }
    }
}
