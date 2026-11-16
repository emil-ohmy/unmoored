using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MyGame
{
    // The game's opening, in the Hold: the Apprentice lies still in complete darkness. Then a lantern on the floor before
    // it kindles, slowly, sputtering, and a voice in the dark: "Apprentice. Take light." Bidden (the prompt: Take light),
    // you reach out and take it (Apprentice.Rise: the Wake clip): the hand closes on its ring and from then it hangs from
    // the hand, swaying, upright, its light going with it, as you come up onto your knees and hold it up before your face,
    // and rise, and hang it at your hip (there it's handed over to the hip's lantern: the same lantern in the same place).
    // The cave comes back around you as you rise, the nearest of its lights first; once you stand the Area is named. The
    // lights are switched off and on, not turned down (many have flickers of their own that set their intensity every
    // frame); the ambient and the fog are scaled to black.
    public sealed class TheOpening : MonoBehaviour
    {
        public Apprentice apprentice;
        public GameObject floorLantern;     // the lantern on the floor before the Apprentice (its origin its ring)
        public Light floorLight;            // its light (kindles out of the dark; goes with it in the hand)
        public Transform foreArm;           // the Apprentice's left forearm bone (ForeL): the hand that carries it
        public string line = "Apprentice. Take light.";
        public AudioClip voice;             // a whisper as the line comes
        public float lanternIntensity = 2.4f, lanternRange = 4.5f;

        [Header("Timing (seconds)")]
        public float darkFor = 4f;          // nothing at all, at first
        public float kindleFor = 5f;        // the lantern coming up, sputtering
        public float lineAt = 3f;           // into the kindling: the words
        public float bidAfter = 3f;         // after the words: the prompt
        public float lightsReturnOver = 6f; // once taken: the cave's lights back, nearest first, over the rise
        public float handOver = 0.5f;       // at the hip: the carried light fading into the hip's
        public float closeZoom = 0.62f;     // the camera, close on you while you lie

        [Header("Carried (hanging from the hand by its ring)")]
        public float hangLength = 0.24f;    // ring to glass
        public float swaySpring = 30f, swayDamping = 5f;

        public enum Stage { Dark, Kindling, Bidden, Rising, Done }
        public Stage Current { get; private set; } = Stage.Done;
        public bool Playing => Current != Stage.Done;
        public bool Carried { get; private set; } // (tests) in the hand

        readonly List<Light> darkened = new List<Light>();
        Color sky, equator, ground, fog;
        float ambientWas = 1f;
        Renderer[] lanternRenderers;
        MaterialPropertyBlock glowBlock;
        static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");
        static readonly Color LanternGlow = new Color(0.78f, 0.9f, 1f);
        Coroutine running;
        Vector3 ringInHand, bob, bobVelocity;
        float yaw;

        void Start()
        {
            if (apprentice == null)
                apprentice = FindAnyObjectByType<Apprentice>();
            running = StartCoroutine(Play());
        }

        IEnumerator Play()
        {
            Current = Stage.Dark;
            apprentice.LieStill();
            Darken();
            var bow = apprentice.GetComponent<BowLantern>();
            if (bow != null)
                bow.Dimmed = 0f;
            CameraRig.Instance?.SetZoom(closeZoom);
            SetLantern(0f);
            yield return new WaitForSeconds(darkFor);

            // The lantern kindles: a spark, a long sputter, then it holds.
            Current = Stage.Kindling;
            bool spoken = false;
            for (float t = 0f; t < kindleFor || !spoken; t += Time.deltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / kindleFor));
                float sputter = 1f - 0.45f * (1f - k) * Mathf.Abs(Mathf.Sin(t * 11f) * Mathf.Sin(t * 6.1f));
                SetLantern(k * sputter);
                if (!spoken && t >= lineAt)
                {
                    spoken = true;
                    FindHud()?.OpeningLine.Show(line);
                    if (voice != null)
                        AudioSource.PlayClipAtPoint(voice, apprentice.transform.position + Vector3.up, 0.8f);
                }
                yield return null;
            }
            SetLantern(1f);
            yield return new WaitForSeconds(Mathf.Max(0f, bidAfter - (kindleFor - lineAt)));

            // "Take light": waiting on you.
            Current = Stage.Bidden;
            apprentice.BidTakeLight();
            bool taken = false, stowed = false;
            void OnTaken() => taken = true;
            void OnStowed() => stowed = true;
            apprentice.LightTaken += OnTaken;
            apprentice.LightStowed += OnStowed;
            while (!taken)
            {
                SetLantern(1f + 0.06f * Mathf.Sin(Time.time * 2.1f)); // (it breathes a little, waiting; and as the hand comes)
                yield return null;
            }

            // Taken: in the hand now, by its ring (LateUpdate carries it); the words go; the cave comes back as you rise.
            Current = Stage.Rising;
            TakeInHand();
            FindHud()?.OpeningLine.Dismiss();
            var order = darkened.Where(l => l != null)
                .OrderBy(l => (l.transform.position - apprentice.transform.position).sqrMagnitude).ToList();
            int lit = 0;
            float handedOverAt = -1f;
            for (float t = 0f; t < lightsReturnOver || handedOverAt < 0f || Time.time - handedOverAt < handOver; t += Time.deltaTime)
            {
                float k = Mathf.Clamp01(t / lightsReturnOver);
                SetAmbient(Mathf.SmoothStep(0f, 1f, k));
                for (int want = Mathf.CeilToInt(order.Count * Mathf.SmoothStep(0f, 1f, k)); lit < want; lit++)
                    order[lit].enabled = true;
                if (stowed && handedOverAt < 0f)
                    handedOverAt = Time.time;
                if (handedOverAt >= 0f)
                {
                    // Hung at the hip: its light fades into the hip lantern's; then it's gone (the hip's is it).
                    float u = Mathf.Clamp01((Time.time - handedOverAt) / handOver);
                    if (bow != null)
                        bow.Dimmed = u;
                    if (floorLight != null)
                        floorLight.intensity = lanternIntensity * (1f - u);
                    if (u >= 1f && floorLantern.activeSelf)
                        floorLantern.SetActive(false);
                    if (u >= 1f)
                        Carried = false;
                }
                if (apprentice.CurrentState == Apprentice.State.Free)
                    CameraRig.Instance?.SetZoom(1f);
                yield return null;
            }
            apprentice.LightTaken -= OnTaken;
            apprentice.LightStowed -= OnStowed;
            Finish();
        }

        // The hand closes on the ring: from now the lantern hangs from it (where the ring is in the forearm's frame).
        void TakeInHand()
        {
            if (floorLantern == null || foreArm == null)
                return;
            ringInHand = foreArm.InverseTransformPoint(floorLantern.transform.position);
            yaw = floorLantern.transform.eulerAngles.y;
            bob = floorLantern.transform.position + Vector3.down * hangLength;
            bobVelocity = Vector3.zero;
            Carried = true;
        }

        // Carried: the ring in the hand, the lantern hanging from it upright, swaying after the hand's moves (a pendulum).
        void LateUpdate()
        {
            if (!Carried || floorLantern == null || foreArm == null)
                return;
            float dt = Mathf.Max(Time.deltaTime, 1e-4f);
            Vector3 ring = foreArm.TransformPoint(ringInHand);
            Vector3 rest = ring + Vector3.down * hangLength;
            bobVelocity += ((rest - bob) * swaySpring - bobVelocity * swayDamping) * dt;
            bob += bobVelocity * dt;
            Vector3 hang = bob - ring;
            if (hang.sqrMagnitude < 1e-6f)
                hang = Vector3.down;
            bob = ring + hang.normalized * hangLength;
            floorLantern.transform.SetPositionAndRotation(ring,
                Quaternion.FromToRotation(Vector3.down, hang.normalized) * Quaternion.Euler(0f, yaw, 0f));
        }

        // Over at once (the tests; anything that must start you standing): everything as it was, you up, the lantern held.
        public void Skip()
        {
            if (running != null)
                StopCoroutine(running);
            running = null;
            Carried = false;
            if (apprentice != null)
                apprentice.EndWaking();
            if (floorLantern != null)
                floorLantern.SetActive(false);
            FindHud()?.OpeningLine.Dismiss();
            Finish();
        }

        void Finish()
        {
            foreach (var l in darkened)
                if (l != null)
                    l.enabled = true;
            darkened.Clear();
            if (Current != Stage.Done)
                SetAmbient(1f);
            var bow = apprentice != null ? apprentice.GetComponent<BowLantern>() : null;
            if (bow != null)
                bow.Dimmed = 1f;
            if (floorLantern != null && floorLantern.activeSelf && apprentice != null && apprentice.LanternAtTheHip)
                floorLantern.SetActive(false);
            Carried = false;
            CameraRig.Instance?.SetZoom(1f);
            Current = Stage.Done;
        }

        // Every light in the scene off but the floor lantern's (the Apprentice's own is dimmed through its BowLantern);
        // the ambient and the fog to black.
        void Darken()
        {
            var bow = apprentice.GetComponent<BowLantern>();
            foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if (!l.enabled || l == floorLight || (bow != null && l == bow.lanternLight))
                    continue;
                l.enabled = false;
                darkened.Add(l);
            }
            sky = RenderSettings.ambientSkyColor;
            equator = RenderSettings.ambientEquatorColor;
            ground = RenderSettings.ambientGroundColor;
            fog = RenderSettings.fogColor;
            ambientWas = RenderSettings.ambientIntensity;
            SetAmbient(0f);
        }

        void SetAmbient(float k)
        {
            RenderSettings.ambientSkyColor = sky * k;
            RenderSettings.ambientEquatorColor = equator * k;
            RenderSettings.ambientGroundColor = ground * k;
            RenderSettings.fogColor = fog * k;
            RenderSettings.ambientIntensity = ambientWas * k;
        }

        void SetLantern(float k)
        {
            if (floorLight != null)
            {
                floorLight.intensity = lanternIntensity * k;
                floorLight.range = lanternRange * Mathf.Lerp(0.5f, 1f, Mathf.Clamp01(k));
            }
            if (floorLantern == null)
                return;
            lanternRenderers ??= floorLantern.GetComponentsInChildren<Renderer>();
            glowBlock ??= new MaterialPropertyBlock();
            glowBlock.SetColor(EmissionId, LanternGlow * (1.6f * k));
            foreach (var r in lanternRenderers)
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                    if (mats[i] != null && mats[i].name.Contains("Glow")) // (its glass, not its iron)
                        r.SetPropertyBlock(glowBlock, i);
            }
        }

        static Hud FindHud() => FindAnyObjectByType<Hud>();
    }
}
