using System.Collections.Generic;
using UnityEngine;

namespace MyGame
{
    // The Hold, That Would Not Let Go: the Hold's (easy, first) Boss (docs/brief.md). At the way out, the cave itself won't
    // let you go: a great face in the chamber's north wall, high up, and two enormous hands on long arms either side of it.
    // Rooted: a fight of patterns.
    //   Dormant till you come into its chamber; then it wakes (the face swells out, the hands tear free).
    //   The hands: one rears up (the tell) and slams down where you stand (its arm turned at the shoulder to aim); after a
    //     slam it lies stunned on the floor a while: strike it then (it hurts the whole). Or one is drawn out low and swept
    //     across the floor (vault it, or dodge). Wound a hand enough and it withdraws into the wall a while; with both
    //     withdrawn, the face sags down into reach: strike it (it takes harm doubly), until it heaves itself up and the
    //     hands tear out again.
    //   At half, the face joins (a roar first): it draws breath and the cave's breath gusts you back; close under it, it
    //     bites down at you; the hands quicker.
    //   Beaten, it lets go: the face sags to the floor, its mouth opening on the way out (the tunnel behind it, blocked till
    //     then), and the hands sink back into the stone.
    // Its clips play on layers at once: the body (0: Dormant, Wake, Idle, Roar, Death), the face (1), each arm (2, 3); a
    // layer above Idle overrides only the bones its clip keys.
    public sealed class TheHoldWall : MonoBehaviour, IBoss, IBossParts
    {
        public string title = "The Hold";
        public string epithet = "That Would Not Let Go";
        public string farewell = "LET GO";
        public string farewellLine = "It held on as long as it could.";
        public AudioClip[] music;

        [Header("Its parts and its place")]
        public HoldPart handL, handR, face;
        public Transform shoulderL, shoulderR, boneHandL, boneHandR, boneFace;
        public Vector3 arenaCentre;
        public float arenaRadius = 8.5f;
        public Collider wayOut;               // the tunnel behind its face, blocked till it's beaten
        public SwingTrail[] trails;           // its hands' swipes, each for its own blows (SlamL, SweepR...)

        [Header("Its numbers (first Boss: forgiving)")]
        public float maxHealth = 320f;        // (about two dozen blows of the oar: half of them doubled on the face)
        public float handWound = 56f;         // a hand struck this much (four blows: one stun's worth): it withdraws
        public float withdrawnFor = 12f;
        public float stunnedFor = 3f;
        public float saggedFor = 5f;
        public float faceVulnerability = 2f;
        public float slamDraw = 1.2f, slamTime = 0.4f, slamRecover = 1.0f, slamImpactAt = 0.55f;
        public float sweepDraw = 0.9f, sweepTime = 0.6f, sweepRecover = 0.8f;
        public float breathDraw = 1.2f, breathTime = 1.5f, breathRecover = 0.8f;
        public float biteDraw = 0.8f, biteTime = 0.5f, biteRecover = 0.8f;
        public float restBetween = 1.4f;      // between one attack and the next (phase two: less)
        public float lateQuicker = 0.75f;     // phase two: tells and rests this much as long
        public float slamDamage = 22f, slamRadius = 1.9f, sweepDamage = 14f, biteDamage = 26f, breathPush = 9f;
        public float inFront = 2f;            // its parts' places on the floor at least this far out from the wall
        public float aimLimit = 55f;          // how far an arm turns at the shoulder to aim a slam
        public float pullBack = 1.6f;        // the camera drawn back while it fights (its face is high in the wall)...
        public float lean = 0.58f;             // ...and leaning towards the wall (the floor before it), to have its face in view

        [Header("Sounds")]
        public AudioClip[] wakeSound, slamDrawSound, slamSound, sweepSound, withdrawSound, returnSound, breathSound, biteSound,
                           hurtSound, roarSound, deathSound;

        public string Title => title;
        public string Epithet => epithet;
        public bool IsAlive => health > 0f;
        public float HealthFraction => Mathf.Clamp01(health / maxHealth);
        public float[] PhaseMarks => new[] { 0.5f };
        public int Phase => HealthFraction <= 0.5f ? 2 : 1;
        public AudioClip[] Music => music;
        public string Farewell => farewell;
        public string FarewellLine => farewellLine;
        public IReadOnlyList<(string name, float fraction)> Parts { get { parts.Clear(); parts.Add((title, HealthFraction)); return parts; } }
        public bool Owns(Soured soured) => soured == handL || soured == handR || soured == face;

        public enum Mood { Dormant, Waking, Fighting, Dying, Gone }
        public Mood State { get; private set; } = Mood.Dormant;
        public bool Withdrawn(bool left) => (left ? withdrawnL : withdrawnR) > Time.time;
        public bool Stunned(bool left) => (left ? handL : handR).Open;
        public bool Sagged => face != null && face.Open;
        public int Slams { get; private set; }  // (tests)
        public int Sweeps { get; private set; } // (tests)

        static readonly HashSet<string> FaceClips = new HashSet<string>
            { "BreathDraw", "Breath", "BreathRecover", "BiteDraw", "Bite", "BiteRecover", "Sag", "Sagged", "Rise" };
        readonly List<(string, float)> parts = new List<(string, float)>();
        readonly Dictionary<string, string> names = new Dictionary<string, string>();
        Animation anim;
        Apprentice target;
        float health, nextAttack, withdrawnL, withdrawnR, woundL, woundR;
        float aimL, aimR;             // degrees the arm's turned at the shoulder (to aim a slam)
        bool busyL, busyR, busyFace, roared, defeated, framed;

        void Awake()
        {
            health = maxHealth;
            anim = GetComponentInChildren<Animation>();
            foreach (AnimationState s in anim)
            {
                int bar = s.name.LastIndexOf('|');
                string clip = bar >= 0 ? s.name.Substring(bar + 1) : s.name;
                names[clip] = s.name;
                // Each clip only on its own bones (the exporter bakes every bone into every clip): the face's on the
                // face, an arm's on its shoulder; the body's (Dormant, Wake, Idle, Roar, Death) on everything.
                if (FaceClips.Contains(clip) && boneFace != null)
                    s.AddMixingTransform(boneFace, true);
                else if (clip.EndsWith("L") && clip != "Idle" && shoulderL != null && !FaceClips.Contains(clip))
                    s.AddMixingTransform(shoulderL, true);
                else if (clip.EndsWith("R") && clip != "Roar" && shoulderR != null && !FaceClips.Contains(clip))
                    s.AddMixingTransform(shoulderR, true);
            }
            Soured.Struck += OnStruck;
        }

        void OnDestroy() => Soured.Struck -= OnStruck;

        void Start()
        {
            target = FindAnyObjectByType<Apprentice>();
            Layer("Dormant", 0, loop: true);
            if (face != null)
                face.Reformed += Sleep; // (a Rest)
        }

        // ---- its clips, on layers

        AnimationState Layer(string clip, int layer, bool loop = false, float duration = 0f, float fade = 0.25f)
        {
            if (!names.TryGetValue(clip, out var full))
                return null;
            var s = anim[full];
            s.layer = layer;
            s.wrapMode = loop ? WrapMode.Loop : WrapMode.ClampForever;
            s.speed = !loop && duration > 0f ? s.length / duration : 1f;
            s.time = 0f;
            anim.CrossFade(full, fade, PlayMode.StopSameLayer);
            return s;
        }

        // A layer let go: its bones back to the body's clip.
        void Release(int layer, float fade = 0.3f)
        {
            foreach (AnimationState s in anim)
                if (s.layer == layer && s.enabled)
                    anim.Blend(s.name, 0f, fade);
        }

        // ---- every frame

        void Update()
        {
            if (target == null || State == Mood.Gone)
                return;
            if (State == Mood.Dormant)
            {
                if (target.IsAlive && In(target.transform.position))
                    StartCoroutine(Wake());
                return;
            }
            bool on = State == Mood.Fighting && target.IsAlive && In(target.transform.position, 4f);
            if (on)
                BossFight.Begin(this);
            else if (State == Mood.Fighting)
                BossFight.End(this);
            if (on != framed && State != Mood.Dying)
                Frame(on);
            if (State != Mood.Fighting)
                return;

            // Its hands back out of the wall, in time; the face up again when they are.
            if (!busyL && withdrawnL > 0f && Time.time >= withdrawnL)
                StartCoroutine(Return(true));
            if (!busyR && withdrawnR > 0f && Time.time >= withdrawnR)
                StartCoroutine(Return(false));
            if (!busyFace && withdrawnL > Time.time && withdrawnR > Time.time && !Sagged)
                StartCoroutine(Sag());

            // The phase change: it roars, and the face joins.
            if (Phase == 2 && !roared && !busyFace && !busyL && !busyR)
            {
                roared = true;
                StartCoroutine(Roar());
                return;
            }

            if (Time.time >= nextAttack && target.IsAlive && On(target.transform.position))
                Choose();
        }

        void LateUpdate()
        {
            // Each arm turned at its shoulder (after the clip has posed it), to aim a slam; the parts on the floor under
            // their bones.
            if (shoulderL != null)
                shoulderL.rotation = Quaternion.AngleAxis(aimL, Vector3.up) * shoulderL.rotation;
            if (shoulderR != null)
                shoulderR.rotation = Quaternion.AngleAxis(aimR, Vector3.up) * shoulderR.rotation;
            // Lock-on can choose a hand while it's out of the wall, the face while it fights (struck only when in reach).
            bool fighting = State == Mood.Fighting;
            if (handL != null) handL.Out = fighting && withdrawnL <= Time.time && !withdrawingL;
            if (handR != null) handR.Out = fighting && withdrawnR <= Time.time && !withdrawingR;
            if (face != null) face.Out = fighting;
            if (handL != null && boneHandL != null) handL.Follow(OnFloor(boneHandL.position));
            if (handR != null && boneHandR != null) handR.Follow(OnFloor(boneHandR.position));
            if (face != null && boneFace != null) face.Follow(OnFloor(boneFace.position));
        }

        // A part's place: on the floor under its bone, but never back in the wall (where lock-on and the camera would lose it).
        Vector3 OnFloor(Vector3 bone)
        {
            Vector3 p = new Vector3(bone.x, arenaCentre.y, bone.z), out_ = Flat(transform.forward).normalized;
            float d = Vector3.Dot(p - transform.position, out_);
            return d < inFront ? p + out_ * (inFront - d) : p;
        }

        bool In(Vector3 p, float more = 0f) => Flat(p - arenaCentre).magnitude < arenaRadius + more && Mathf.Abs(p.y - arenaCentre.y) < 2.5f;
        bool On(Vector3 p) => In(p);
        float Quick => Phase >= 2 ? lateQuicker : 1f;

        // ---- what it does next

        void Choose()
        {
            Vector3 to = target.transform.position;
            float toWall = Flat(to - transform.position).magnitude;
            var options = new List<System.Func<System.Collections.IEnumerator>>();
            bool left = Flat(to - shoulderL.position).magnitude < Flat(to - shoulderR.position).magnitude;
            bool canL = !busyL && withdrawnL < Time.time && !handL.Open, canR = !busyR && withdrawnR < Time.time && !handR.Open;
            if ((left ? canL : canR))
                options.Add(() => Slam(left));
            else if (canL || canR)
                options.Add(() => Slam(canL));
            // (with one hand back in the wall the other only slams: it lies stunned after, so you always get your chance at it)
            bool oneGone = withdrawnL > Time.time || withdrawnR > Time.time;
            if ((canL || canR) && toWall > 3f && !oneGone)
                options.Add(() => Sweep(canR ? false : true));
            if (Phase >= 2 && !busyFace && !Sagged)
            {
                if (toWall < 4.5f)
                    options.Add(Bite);
                else
                    options.Add(Breath);
            }
            if (options.Count == 0)
                return;
            nextAttack = float.MaxValue; // (set again as the attack ends)
            StartCoroutine(options[Random.Range(0, options.Count)]());
        }

        System.Collections.IEnumerator Wake()
        {
            State = Mood.Waking;
            BossFight.Begin(this);
            Frame(true);
            Sfx.Instance?.Attack(wakeSound, transform.position + Vector3.up * 4f, 1f, "the_hold_wake");
            CameraRig.Instance?.Shake(0.25f);
            Layer("Wake", 0, duration: 4f, fade: 0.1f);
            yield return new WaitForSeconds(4f);
            Layer("Idle", 0, loop: true, fade: 0.3f);
            State = Mood.Fighting;
            nextAttack = Time.time + 1f;
        }

        System.Collections.IEnumerator Slam(bool left)
        {
            string side = left ? "L" : "R";
            SetBusy(left, true);
            int layer = left ? 2 : 3;
            Sfx.Instance?.Attack(slamDrawSound, (left ? shoulderL : shoulderR).position, 1f, "the_hold_slam_draw");
            Layer("SlamDraw" + side, layer, duration: slamDraw * Quick, fade: 0.2f);
            // Aimed at you as it rears (it follows you most of the draw, then commits).
            float draw = slamDraw * Quick;
            for (float t = 0f; t < draw; t += Time.deltaTime)
            {
                if (t < draw * 0.75f)
                    Aim(left, target.transform.position);
                yield return null;
            }
            Layer("Slam" + side, layer, duration: slamTime, fade: 0.05f);
            Trail("Slam" + side, true);
            yield return new WaitForSeconds(slamTime * slamImpactAt);
            Trail("Slam" + side, false);
            // Down: where the hand lands, a blow (a dodge slips it; a block takes it, heavily).
            Vector3 at = Flat((left ? boneHandL : boneHandR).position) + Vector3.up * arenaCentre.y;
            Slams++;
            Sfx.Instance?.Attack(slamSound, at, 1f, "the_hold_slam");
            CameraRig.Instance?.Shake(0.3f);
            Effects.Instance?.Rubble(at, 1.6f);
            if (target.IsAlive && Flat(target.transform.position - at).magnitude < slamRadius && !target.Airborne)
                target.TakeHit(slamDamage, Flat(target.transform.position - at).normalized * 6f, left ? handL : handR);
            yield return new WaitForSeconds(slamTime * (1f - slamImpactAt));
            // It lies there, stunned: strike it.
            Layer("SlamStunned" + side, layer, loop: true, fade: 0.1f);
            (left ? handL : handR).Open = true;
            float until = Time.time + stunnedFor;
            while (Time.time < until && !(left ? withdrawingL : withdrawingR))
                yield return null;
            (left ? handL : handR).Open = false;
            if (left ? withdrawingL : withdrawingR)
                yield break; // (wounded enough: it's going back into the wall: Withdraw has it)
            Layer("SlamRecover" + side, layer, duration: slamRecover, fade: 0.15f);
            yield return new WaitForSeconds(slamRecover);
            Release(layer);
            yield return EaseAim(left);
            SetBusy(left, false);
            Rest();
        }

        System.Collections.IEnumerator Sweep(bool left)
        {
            string side = left ? "L" : "R";
            SetBusy(left, true);
            int layer = left ? 2 : 3;
            Layer("SweepDraw" + side, layer, duration: sweepDraw * Quick, fade: 0.2f);
            yield return new WaitForSeconds(sweepDraw * Quick);
            Layer("Sweep" + side, layer, duration: sweepTime, fade: 0.05f);
            Sfx.Instance?.Attack(sweepSound, (left ? boneHandL : boneHandR).position, 1f, "the_hold_sweep");
            Trail("Sweep" + side, true);
            // Low along the floor: wherever the hand passes you (not over you in the air), a blow.
            bool struck = false;
            for (float t = 0f; t < sweepTime; t += Time.deltaTime)
            {
                Vector3 hand = (left ? boneHandL : boneHandR).position;
                if (!struck && target.IsAlive && !target.Airborne && Flat(target.transform.position - hand).magnitude < 1.6f
                    && hand.y < arenaCentre.y + 1.2f)
                {
                    struck = true;
                    target.TakeHit(sweepDamage, Flat(target.transform.position - transform.position).normalized * 5f, left ? handL : handR);
                }
                yield return null;
            }
            Trail("Sweep" + side, false);
            Sweeps++;
            Layer("SweepRecover" + side, layer, duration: sweepRecover, fade: 0.1f);
            yield return new WaitForSeconds(sweepRecover);
            Release(layer);
            SetBusy(left, false);
            Rest();
        }

        System.Collections.IEnumerator Breath()
        {
            busyFace = true;
            Layer("BreathDraw", 1, duration: breathDraw * Quick, fade: 0.2f);
            yield return new WaitForSeconds(breathDraw * Quick);
            Layer("Breath", 1, duration: breathTime, fade: 0.05f);
            Sfx.Instance?.Attack(breathSound, boneFace.position, 1f, "the_hold_breath");
            // The cave's breath: a gust pushing you back across the chamber (no harm: it's the push that matters), as far
            // as its far side (never out of it).
            for (float t = 0f; t < breathTime * 0.8f; t += Time.deltaTime)
            {
                if (target.IsAlive && In(target.transform.position, -1.5f) && !target.Airborne)
                    target.Wade(0.55f, Flat(target.transform.position - transform.position).normalized * breathPush); // (slowed, and pushed back)
                yield return null;
            }
            yield return new WaitForSeconds(breathTime * 0.2f);
            Layer("BreathRecover", 1, duration: breathRecover, fade: 0.1f);
            yield return new WaitForSeconds(breathRecover);
            Release(1);
            busyFace = false;
            Rest();
        }

        System.Collections.IEnumerator Bite()
        {
            busyFace = true;
            Layer("BiteDraw", 1, duration: biteDraw * Quick, fade: 0.15f);
            yield return new WaitForSeconds(biteDraw * Quick);
            Layer("Bite", 1, duration: biteTime, fade: 0.05f);
            yield return new WaitForSeconds(biteTime * 0.5f - 0.07f);
            Sfx.Instance?.Attack(biteSound, boneFace.position, 1f, "the_hold_bite"); // (its clack 0.07 s in: on the snap)
            yield return new WaitForSeconds(0.07f);
            Vector3 at = Flat(boneFace.position) + Vector3.up * arenaCentre.y;
            if (target.IsAlive && Flat(target.transform.position - at).magnitude < 2.2f)
                target.TakeHit(biteDamage, Flat(target.transform.position - transform.position).normalized * 7f, face);
            yield return new WaitForSeconds(biteTime * 0.5f);
            Layer("BiteRecover", 1, duration: biteRecover, fade: 0.1f);
            yield return new WaitForSeconds(biteRecover);
            Release(1);
            busyFace = false;
            Rest();
        }

        System.Collections.IEnumerator Roar()
        {
            busyFace = busyL = busyR = true;
            Sfx.Instance?.Attack(roarSound, boneFace.position, 1f, "the_hold_roar");
            CameraRig.Instance?.Shake(0.35f);
            Layer("Roar", 0, duration: 1.6f, fade: 0.15f);
            yield return new WaitForSeconds(1.6f);
            Layer("Idle", 0, loop: true, fade: 0.3f);
            busyFace = busyL = busyR = false;
            Rest();
        }

        // ---- wounds

        bool withdrawingL, withdrawingR;

        void OnStruck(Soured s, HitResult result, float dealt)
        {
            if (!IsAlive || result != HitResult.Landed || dealt <= 0f || !Owns(s))
                return;
            float harm = s == face ? dealt * faceVulnerability : dealt;
            health = Mathf.Max(0f, health - harm);
            Sfx.Instance?.Attack(hurtSound, s.transform.position + Vector3.up, 0.9f, "the_hold_hurt");
            if (health <= 0f)
            {
                StopAllCoroutines(); // (whatever it was doing: it lets go)
                StartCoroutine(Die());
                return;
            }
            if (s == handL && (woundL += dealt) >= handWound)
                StartCoroutine(Withdraw(true));
            else if (s == handR && (woundR += dealt) >= handWound)
                StartCoroutine(Withdraw(false));
        }

        System.Collections.IEnumerator Withdraw(bool left)
        {
            if (left ? withdrawingL : withdrawingR)
                yield break;
            if (left) withdrawingL = true; else withdrawingR = true;
            yield return null; // (the slam lets go of it)
            SetBusy(left, true);
            int layer = left ? 2 : 3;
            (left ? handL : handR).Open = false;
            Sfx.Instance?.Attack(withdrawSound, (left ? shoulderL : shoulderR).position, 1f, "the_hold_withdraw");
            Layer("Withdraw" + (left ? "L" : "R"), layer, duration: 1f, fade: 0.15f);
            yield return EaseAim(left);
            yield return new WaitForSeconds(0.8f);
            if (left) { withdrawnL = Time.time + withdrawnFor; woundL = 0f; withdrawingL = false; }
            else { withdrawnR = Time.time + withdrawnFor; woundR = 0f; withdrawingR = false; }
            SetBusy(left, false);
            Rest();
        }

        System.Collections.IEnumerator Return(bool left)
        {
            SetBusy(left, true);
            if (left) withdrawnL = 0f; else withdrawnR = 0f;
            int layer = left ? 2 : 3;
            Sfx.Instance?.Attack(returnSound, (left ? shoulderL : shoulderR).position, 1f, "the_hold_return");
            Layer("Return" + (left ? "L" : "R"), layer, duration: 1.2f, fade: 0.1f);
            yield return new WaitForSeconds(1.2f);
            Release(layer);
            SetBusy(left, false);
        }

        // Both hands in the wall: the face sags down into reach, spent; up again when a hand comes back (or in time).
        System.Collections.IEnumerator Sag()
        {
            busyFace = true;
            Layer("Sag", 1, duration: 1.2f, fade: 0.2f);
            yield return new WaitForSeconds(1.2f);
            Layer("Sagged", 1, loop: true, fade: 0.1f);
            face.Open = true;
            float until = Time.time + saggedFor;
            while (Time.time < until && IsAlive)
                yield return null;
            face.Open = false;
            if (!IsAlive)
                yield break;
            Layer("Rise", 1, duration: 1.2f, fade: 0.1f);
            yield return new WaitForSeconds(1.2f);
            Release(1);
            busyFace = false;
            // (and the hands tear out again, at once)
            if (withdrawnL > 0f) withdrawnL = Time.time;
            if (withdrawnR > 0f) withdrawnR = Time.time;
            Rest();
        }

        System.Collections.IEnumerator Die()
        {
            State = Mood.Dying;
            handL.Open = handR.Open = face.Open = false;
            for (int layer = 1; layer <= 3; layer++)
                Release(layer, 0.4f);
            Layer("Death", 0, duration: 5f, fade: 0.3f);
            Sfx.Instance?.Attack(deathSound, boneFace.position, 1f, "the_hold_death");
            aimL = aimR = 0f;
            if (!defeated)
            {
                defeated = true;
                BossFight.Defeat(this);
            }
            yield return new WaitForSeconds(5f);
            if (wayOut != null)
                wayOut.enabled = false; // (its mouth open on the way out)
            BossFight.End(this);
            Frame(false);
            State = Mood.Gone;
        }

        // ---- helpers

        void Aim(bool left, Vector3 at)
        {
            var shoulder = left ? shoulderL : shoulderR;
            Vector3 straight = transform.forward; // (out of the wall: the way it faces)
            Vector3 to = Flat(at - shoulder.position);
            float want = to.sqrMagnitude > 0.01f ? Mathf.Clamp(Vector3.SignedAngle(Flat(straight).normalized, to.normalized, Vector3.up), -aimLimit, aimLimit) : 0f;
            if (left) aimL = Mathf.MoveTowardsAngle(aimL, want, 90f * Time.deltaTime);
            else aimR = Mathf.MoveTowardsAngle(aimR, want, 90f * Time.deltaTime);
        }

        System.Collections.IEnumerator EaseAim(bool left)
        {
            for (float t = 0f; t < 0.6f; t += Time.deltaTime)
            {
                if (left) aimL = Mathf.MoveTowardsAngle(aimL, 0f, 120f * Time.deltaTime);
                else aimR = Mathf.MoveTowardsAngle(aimR, 0f, 120f * Time.deltaTime);
                yield return null;
            }
        }

        void SetBusy(bool left, bool on) { if (left) busyL = on; else busyR = on; }

        void Trail(string attack, bool on)
        {
            foreach (var trail in trails ?? new SwingTrail[0])
                if (trail != null && trail.For(attack))
                    trail.Emitting = on;
        }

        public int SwipeSamples { get { int n = 0; foreach (var t in trails ?? new SwingTrail[0]) n += t.Samples; return n; } } // (tests)

        void Rest() => nextAttack = Mathf.Min(nextAttack, Time.time) + restBetween * Quick + Random.Range(0f, 0.6f);

        static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);

        // At a Rest (or the Apprentice gone): asleep in the wall again, whole.
        public void Sleep() // (not "Reset": Unity calls that when the component's added in the editor)
        {
            if (State == Mood.Gone || State == Mood.Dying)
                return;
            StopAllCoroutines();
            health = maxHealth;
            woundL = woundR = 0f;
            withdrawnL = withdrawnR = 0f;
            withdrawingL = withdrawingR = false;
            busyL = busyR = busyFace = roared = false;
            aimL = aimR = 0f;
            handL.Open = handR.Open = face.Open = false;
            for (int layer = 1; layer <= 3; layer++)
                Release(layer, 0.1f);
            Layer("Dormant", 0, loop: true, fade: 0.3f);
            State = Mood.Dormant;
            BossFight.End(this);
            if (framed)
                Frame(false);
        }

        // The camera drawn back and leaning towards the wall while it fights (its face high in it), home again after.
        Transform focus;
        void Frame(bool on)
        {
            framed = on;
            if (focus == null)
            {
                focus = new GameObject("The Hold (where the camera leans)").transform;
                focus.position = new Vector3(transform.position.x, arenaCentre.y, transform.position.z) + Flat(transform.forward).normalized * 2.5f;
            }
            CameraRig.Instance?.SetZoom(on ? pullBack : 1f);
            CameraRig.Instance?.SetFightFocus(on ? focus : null, on ? lean : 0f);
        }
    }
}
