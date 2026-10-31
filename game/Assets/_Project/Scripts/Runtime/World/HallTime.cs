using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace MyGame
{
    // The Hall of the Last Hour's time: stopped (as you find it), or running (its central engine set going). Stopped,
    // whatever isn't in time is drained to a cold grey (the Toon and Water shaders, from the globals set here: only the
    // things' shading, light and fog and all, only their glow keeping its colour; the Apprentice's materials are never
    // stilled), the air is ghastlier (a heavier vignette and grain), the dust hangs, stirring only round the Apprentice
    // (StillAir), and the music is the stopped one. Set going, the colour comes back in a wave out from the engine, the
    // light warms and the colour deepens (the moon, the sky's light, the fog, a richer grade) as it goes, the dust falls
    // where it passes, and the music wakes. Leaving the scene, the globals go back to nothing: no other Area is touched.
    public sealed class HallTime : MonoBehaviour
    {
        public static HallTime Instance { get; private set; }

        public Transform engine;                              // where the wave starts (the central engine; else here)
        public float waveSpeed = 12f;                         // metres a second
        public float waveWidth = 6f;                          // its soft edge
        public float reach = 140f;                            // past all of the Hall: the wave's done
        public float stopFade = 1.5f;                         // stopped again (the lab): back to grey over this long
        public Color tint = new Color(0.9f, 0.93f, 1f, 1f);   // the grey's cast: cold, silvery
        public float contrast = 1.25f;                        // the grey a little harder

        [Header("Running: the light warmed, the colour deepened (blended in as the wave goes out)")]
        public Color runningMoon = new Color(1f, 0.96f, 0.9f);  // (a touch warmer only: the colours read true, the lamps warm them)
        public float runningMoonIntensity = 0.82f;
        public Color runningSky = new Color(0.4f, 0.42f, 0.5f), runningEquator = new Color(0.31f, 0.29f, 0.31f), runningGround = new Color(0.1f, 0.09f, 0.09f);
        public Color runningFog = new Color(0.26f, 0.26f, 0.31f);
        public float runningSaturation = 10f, runningContrast = 12f, runningExposure = -0.2f;
        public Color runningFilter = new Color(1f, 0.97f, 0.92f);
        public float warmBy = 40f;                            // the wave this far out: the light all warmed

        [Header("Stopped: ghastlier")]
        public float stoppedVignette = 0.45f, stoppedGrain = 0.42f;

        public float Warmth { get; private set; }             // 0 stopped .. 1 the light all warmed
        public bool Running { get; private set; }
        public float WaveRadius { get; private set; }
        public float Stillness { get; private set; } = 1f;    // everywhere, 0..1 (the wave aside)
        public Vector3 WaveCentre => engine != null ? engine.position : transform.position;
        public event System.Action<bool> Changed;             // running or not

        Light moon;
        Color moonWas, skyWas, equatorWas, groundWas, fogWas;
        float moonIntensityWas;
        Volume runningVolume, stoppedVolume;

        static readonly int ContrastId = Shader.PropertyToID("_HallStillContrast");
        static readonly int StillId = Shader.PropertyToID("_HallStill"), WaveId = Shader.PropertyToID("_HallWave"),
                            WaveOnId = Shader.PropertyToID("_HallWaveOn"), WaveWidthId = Shader.PropertyToID("_HallWaveWidth"),
                            TintId = Shader.PropertyToID("_HallStillTint");

        void Awake()
        {
            Instance = this;
            Apply();
        }

        void Start()
        {
            moon = RenderSettings.sun;
            if (moon == null)
                foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None))
                    if (l.type == LightType.Directional)
                        moon = l;
            if (moon != null)
            {
                moonWas = moon.color;
                moonIntensityWas = moon.intensity;
            }
            skyWas = RenderSettings.ambientSkyColor;
            equatorWas = RenderSettings.ambientEquatorColor;
            groundWas = RenderSettings.ambientGroundColor;
            fogWas = RenderSettings.fogColor;
            runningVolume = MakeVolume("Time Running (the grade)", profile =>
            {
                var grade = profile.Add<ColorAdjustments>(true);
                grade.saturation.Override(runningSaturation);
                grade.contrast.Override(runningContrast);
                grade.postExposure.Override(runningExposure);
                grade.colorFilter.Override(runningFilter);
            });
            stoppedVolume = MakeVolume("Time Stopped (the air)", profile =>
            {
                profile.Add<Vignette>(true).intensity.Override(stoppedVignette);
                var grain = profile.Add<FilmGrain>(true);
                grain.type.Override(FilmGrainLookup.Medium2);
                grain.intensity.Override(stoppedGrain);
            });
            Apply();
        }

        Volume MakeVolume(string name, System.Action<VolumeProfile> fill)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var volume = go.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 10f; // (over the Area's own)
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            fill(profile);
            volume.sharedProfile = profile;
            return volume;
        }

        void OnEnable() => Instance = this;

        void OnDisable()
        {
            if (Instance == this)
                Instance = null;
            Shader.SetGlobalFloat(StillId, 0f);
            Shader.SetGlobalFloat(WaveOnId, 0f);
        }

        // The central engine set going: time again, out from it.
        public void Run()
        {
            if (Running)
                return;
            Running = true;
            WaveRadius = 0f;
            Stillness = 0f;
            MusicPlayer.Instance?.Wake(true);
            Changed?.Invoke(true);
        }

        // Held still again (the lab's: the game never stops it once it's going).
        public void Stop()
        {
            if (!Running)
                return;
            Running = false;
            Stillness = 0f;
            MusicPlayer.Instance?.Wake(false);
            Changed?.Invoke(false);
        }

        // Whether time is stopped at `point` (stopped everywhere; running, until the wave has passed it).
        public bool StoppedAt(Vector3 point)
        {
            if (!Running)
                return true;
            Vector3 d = point - WaveCentre;
            d.y = 0f;
            return d.magnitude > WaveRadius - waveWidth * 0.5f;
        }

        void Update()
        {
            if (Running)
                WaveRadius = Mathf.Min(reach, WaveRadius + waveSpeed * Time.deltaTime);
            else
                Stillness = Mathf.MoveTowards(Stillness, 1f, Time.deltaTime / Mathf.Max(0.01f, stopFade));
            Apply();
        }

        void Apply()
        {
            // The light and the grade: warming as the wave goes out (or back to cold and grey, stopped again).
            Warmth = Running ? Mathf.Clamp01(WaveRadius / Mathf.Max(1f, warmBy)) : 1f - Stillness;
            float k = Mathf.SmoothStep(0f, 1f, Warmth);
            if (moon != null)
            {
                moon.color = Color.Lerp(moonWas, runningMoon, k);
                moon.intensity = Mathf.Lerp(moonIntensityWas, runningMoonIntensity, k);
            }
            if (runningVolume != null)
            {
                RenderSettings.ambientSkyColor = Color.Lerp(skyWas, runningSky, k);
                RenderSettings.ambientEquatorColor = Color.Lerp(equatorWas, runningEquator, k);
                RenderSettings.ambientGroundColor = Color.Lerp(groundWas, runningGround, k);
                RenderSettings.fogColor = Color.Lerp(fogWas, runningFog, k);
                runningVolume.weight = k;
                stoppedVolume.weight = 1f - k;
            }
            Shader.SetGlobalFloat(ContrastId, contrast);
            Vector3 c = WaveCentre;
            Shader.SetGlobalFloat(StillId, Running ? 0f : Stillness);
            Shader.SetGlobalFloat(WaveOnId, Running && WaveRadius < reach ? 1f : 0f);
            Shader.SetGlobalVector(WaveId, new Vector4(c.x, c.y, c.z, WaveRadius));
            Shader.SetGlobalFloat(WaveWidthId, waveWidth);
            Shader.SetGlobalColor(TintId, tint);
        }

        // A colour as time stopped shows it: its brightness only, in the cold grey.
        public Color Stilled(Color colour)
        {
            float l = colour.r * 0.3f + colour.g * 0.59f + colour.b * 0.11f;
            return new Color(l * tint.r, l * tint.g, l * tint.b, colour.a);
        }
    }
}
