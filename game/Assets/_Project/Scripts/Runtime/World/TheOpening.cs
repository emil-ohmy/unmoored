using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MyGame
{
    // The game's opening, in the Hold: the Apprentice lies still in complete darkness. Then a lantern on the floor before
    // it kindles, and a voice in the dark: "Apprentice. Take light." Bidden (the prompt: Take light), you reach out and
    // take it (Apprentice.Rise: the Wake clip), and rise; the cave comes back around you as you do, the nearest of its
    // lights first, and once you stand the Area is named. The lights are switched off and on, not turned down (many
    // have flickers of their own that set their intensity every frame); the ambient and the fog are scaled to black.
    public sealed class TheOpening : MonoBehaviour
    {
        public Apprentice apprentice;
        public GameObject floorLantern;     // the lantern lying on the floor before the Apprentice (taken at Wake's f28)
        public Light floorLight;            // its light (kindles out of the dark)
        public string line = "Apprentice. Take light.";
        public AudioClip voice;             // a whisper as the line comes
        public float lanternIntensity = 2.4f, lanternRange = 4.5f;

        [Header("Timing (seconds)")]
        public float darkFor = 3f;          // nothing at all, at first
        public float kindleFor = 3.5f;      // the lantern coming up, sputtering
        public float lineAt = 2f;           // into the kindling: the words
        public float bidAfter = 2.5f;       // after the words: the prompt
        public float lightsReturnOver = 3.5f; // once taken: the cave's lights back, nearest first
        public float closeZoom = 0.62f;     // the camera, close on you while you lie

        public enum Stage { Dark, Kindling, Bidden, Rising, Done }
        public Stage Current { get; private set; } = Stage.Done;
        public bool Playing => Current != Stage.Done;

        readonly List<Light> darkened = new List<Light>();
        Color sky, equator, ground, fog;
        float ambientWas = 1f;
        Renderer[] lanternRenderers;
        MaterialPropertyBlock glowBlock;
        static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");
        static readonly Color LanternGlow = new Color(0.78f, 0.9f, 1f);
        Coroutine running;

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

            // The lantern kindles: a spark, a sputter, then it holds.
            Current = Stage.Kindling;
            bool spoken = false;
            for (float t = 0f; t < kindleFor || !spoken; t += Time.deltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / kindleFor));
                float sputter = 1f - 0.35f * (1f - k) * Mathf.Abs(Mathf.Sin(t * 13f) * Mathf.Sin(t * 7.3f));
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
            bool taken = false;
            void OnTaken() => taken = true;
            apprentice.LightTaken += OnTaken;
            while (!taken)
            {
                SetLantern(1f + 0.06f * Mathf.Sin(Time.time * 2.1f)); // (it breathes a little, waiting)
                yield return null;
            }
            apprentice.LightTaken -= OnTaken;

            // Taken: the floor's lantern is at the hip now; the words go; the cave comes back as you rise.
            Current = Stage.Rising;
            floorLantern.SetActive(false);
            FindHud()?.OpeningLine.Dismiss();
            float rest = apprentice.wakeDuration - apprentice.wakeTakeAt;
            var order = darkened.Where(l => l != null)
                .OrderBy(l => (l.transform.position - apprentice.transform.position).sqrMagnitude).ToList();
            int lit = 0;
            for (float t = 0f; t < lightsReturnOver; t += Time.deltaTime)
            {
                float k = Mathf.Clamp01(t / lightsReturnOver);
                if (bow != null)
                    bow.Dimmed = Mathf.Clamp01(t / 0.8f);
                SetAmbient(Mathf.SmoothStep(0f, 1f, k));
                for (int want = Mathf.CeilToInt(order.Count * k); lit < want; lit++)
                    order[lit].enabled = true;
                if (t >= rest - 0.4f)
                    CameraRig.Instance?.SetZoom(1f);
                yield return null;
            }
            Finish();
        }

        // Over at once (the tests; anything that must start you standing): everything as it was, you up, the lantern held.
        public void Skip()
        {
            if (running != null)
                StopCoroutine(running);
            running = null;
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
