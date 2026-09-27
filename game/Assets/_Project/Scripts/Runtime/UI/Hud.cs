using UnityEngine;
using UnityEngine.UI;

namespace MyGame
{
    // Minimal HUD, built in code: HP and stamina bars, Healing charges, a Rest prompt and short event text.
    public sealed class Hud : MonoBehaviour
    {
        public Apprentice apprentice;

        RectTransform hpFill, staminaFill;
        Image[] charges;
        Text prompt, eventText;
        static readonly Color ChargeOn = new Color(1f, 0.66f, 0.3f), ChargeOff = new Color(0.25f, 0.22f, 0.24f, 0.8f);

        void Start()
        {
            if (apprentice == null)
                apprentice = FindAnyObjectByType<Apprentice>();
            var canvasGo = new GameObject("HUD Canvas", typeof(Canvas), typeof(CanvasScaler));
            canvasGo.transform.SetParent(transform, false);
            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            var root = canvasGo.transform;

            hpFill = Bar(root, new Vector2(40, -40), 360, 16, new Color(0.78f, 0.24f, 0.2f));
            staminaFill = Bar(root, new Vector2(40, -64), 300, 10, new Color(0.45f, 0.72f, 0.42f));
            charges = new Image[apprentice.maxHealingCharges];
            for (int i = 0; i < charges.Length; i++)
            {
                charges[i] = Box(root, "Charge", new Vector2(50 + i * 26, -94), new Vector2(14, 14), ChargeOn);
                charges[i].rectTransform.localRotation = Quaternion.Euler(0, 0, 45);
            }
            prompt = Label(root, "Prompt", new Vector2(0.5f, 0f), new Vector2(0, 90), 26);
            prompt.text = "Rest   [E / A]";
            eventText = Label(root, "Event", new Vector2(0.5f, 1f), new Vector2(0, -60), 24);
        }

        void Update()
        {
            if (apprentice == null)
                return;
            hpFill.anchorMax = new Vector2(apprentice.Health / apprentice.maxHealth, 1f);
            staminaFill.anchorMax = new Vector2(apprentice.Stamina / apprentice.maxStamina, 1f);
            for (int i = 0; i < charges.Length; i++)
                charges[i].color = i < apprentice.HealingCharges ? ChargeOn : ChargeOff;
            prompt.enabled = apprentice.NearbyPost != null;
            float age = Time.unscaledTime - apprentice.LastEventTime;
            eventText.text = apprentice.LastEvent;
            eventText.color = new Color(0.95f, 0.92f, 0.85f, Mathf.Clamp01(1.2f - age));
        }

        static RectTransform Bar(Transform parent, Vector2 position, float width, float height, Color colour)
        {
            var back = Box(parent, "Bar", position, new Vector2(width + 4, height + 4), new Color(0.08f, 0.07f, 0.1f, 0.75f));
            var fill = Box(back.transform, "Fill", Vector2.zero, Vector2.zero, colour).rectTransform;
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = Vector2.one;
            fill.offsetMin = new Vector2(2, 2);
            fill.offsetMax = new Vector2(-2, -2);
            fill.pivot = new Vector2(0, 0.5f);
            return fill;
        }

        static Image Box(Transform parent, string name, Vector2 position, Vector2 size, Color colour)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
            var image = go.GetComponent<Image>();
            image.color = colour;
            return image;
        }

        static Text Label(Transform parent, string name, Vector2 anchor, Vector2 position, int size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
            rt.anchoredPosition = position;
            rt.sizeDelta = new Vector2(800, 40);
            var text = go.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(0.95f, 0.92f, 0.85f);
            return text;
        }
    }
}
