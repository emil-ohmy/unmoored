using UnityEngine;

namespace MyGame
{
    // The Orchard's harvest moon, on its moonlight (the scene's directional light). While Father Moth kept its
    // reflection in his ribs it could never set. As he falls and the Drowned Moon pours out of him it dims a little;
    // once it's taken (the Apprentice has the Stroke) it begins at last to set: lower, cooler, darker, its ambient and
    // its fog darkening with it, for good. With the Stroke already had (a save), it has set. It owns the night's light
    // where it is: a Boss darkening the night (Father Moth's phase 4) does it through Darkness.
    [RequireComponent(typeof(Light))]
    public sealed class SettingMoon : MonoBehaviour
    {
        public static SettingMoon Instance { get; private set; }

        public Color setColour = new Color(0.66f, 0.74f, 0.88f);
        public float setIntensity = 0.55f;
        public float setPitch = 11f;          // lower in the sky: longer shadows
        public float setAmbient = 0.62f;      // the ambient and the fog, darkened by this much
        public float dimmed = 0.82f;          // as it pours out of him
        public float setTime = 14f;

        public float Dimming { get; private set; }  // 0..1
        public float Setting { get; private set; }  // 0..1: set
        public bool Dimmed => Dimming > 0f;

        // How much of the night's light is left (1: all of it); a Boss sets it.
        public float Darkness
        {
            get => darkness;
            set { darkness = value; applied = false; }
        }

        Light moon;
        Apprentice apprentice;
        Color colour, fog, sky, equator, ground;
        float intensity, darkness = 1f;
        Quaternion rotation;
        bool dimming, applied;

        void Awake()
        {
            Instance = this;
            moon = GetComponent<Light>();
            colour = moon.color;
            intensity = moon.intensity;
            rotation = transform.rotation;
            sky = RenderSettings.ambientSkyColor;
            equator = RenderSettings.ambientEquatorColor;
            ground = RenderSettings.ambientGroundColor;
            fog = RenderSettings.fogColor;
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        // The Drowned Moon pouring out of him: the sky's moon dims a little.
        public void Dim() => dimming = true;

        void Update()
        {
            if (apprentice == null)
                apprentice = FindFirstObjectByType<Apprentice>();
            bool set = apprentice != null && apprentice.HasStroke;
            float dimTo = dimming || set ? 1f : 0f, setTo = set ? 1f : 0f;
            if (set && Setting <= 0f && Time.timeSinceLevelLoad < 1f)
                Dimming = Setting = 1f; // (a save: it set long ago)
            else if (Dimming == dimTo && Setting == setTo && applied)
                return;
            Dimming = Mathf.MoveTowards(Dimming, dimTo, Time.deltaTime / 2.5f);
            Setting = Mathf.MoveTowards(Setting, setTo, Time.deltaTime / setTime);
            applied = true;
            Apply();
        }

        void Apply()
        {
            float s = UiKit.Smooth(0f, 1f, Setting);
            moon.color = Color.Lerp(colour, setColour, s);
            moon.intensity = Mathf.Lerp(intensity * Mathf.Lerp(1f, dimmed, Dimming), setIntensity, s) * darkness;
            var e = rotation.eulerAngles;
            transform.rotation = Quaternion.Euler(Mathf.Lerp(e.x, setPitch, s), e.y, e.z);
            float dark = Mathf.Lerp(1f, setAmbient, s);
            RenderSettings.ambientSkyColor = sky * dark * darkness;
            RenderSettings.ambientEquatorColor = equator * dark * darkness;
            RenderSettings.ambientGroundColor = ground * dark * darkness;
            RenderSettings.fogColor = fog * dark;
        }
    }
}
