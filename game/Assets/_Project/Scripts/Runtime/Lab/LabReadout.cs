using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MyGame
{
    // The lab's readout, top right (for testing only: the game itself shows no numbers): the Soured last struck (its
    // health and stagger meter), the last blow's damage, blows landed and blocked, the damage of the last five seconds
    // and per second, and the Apprentice's stamina.
    public sealed class LabReadout : MonoBehaviour
    {
        public Font font;
        public float window = 5f;

        public Soured Last { get; private set; }
        public float LastDamage { get; private set; }
        public int Landed { get; private set; }
        public int Blocked { get; private set; }
        public float RecentDamage { get; private set; }
        public string Text => text != null ? text.text : "";

        readonly Queue<(float time, float damage)> recent = new Queue<(float, float)>();
        Apprentice apprentice;
        Text text;

        void Awake()
        {
            var canvasGo = new GameObject("Lab Readout Canvas", typeof(Canvas), typeof(CanvasScaler));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            var topRight = new Vector2(1f, 1f);
            UiKit.Image(canvasGo.transform, "Band", UiKit.Band, Color.black.WithAlpha(0.55f), topRight, new Vector2(-330, -110), new Vector2(700, 230));
            text = UiKit.Text(canvasGo.transform, "Readout", font, 24, UiKit.Bone.WithAlpha(0.9f), TextAnchor.UpperRight, topRight, new Vector2(-40, -30));
            text.rectTransform.pivot = topRight;
            text.rectTransform.sizeDelta = new Vector2(680, 220);
            Soured.Struck += OnStruck;
        }

        void Start() => apprentice = FindAnyObjectByType<Apprentice>();

        void OnDestroy() => Soured.Struck -= OnStruck;

        void OnStruck(Soured soured, HitResult result, float damage)
        {
            Last = soured;
            if (result == HitResult.Blocked)
            {
                Blocked++;
                return;
            }
            Landed++;
            LastDamage = damage;
            recent.Enqueue((Time.time, damage));
        }

        public void ResetCounts()
        {
            Landed = Blocked = 0;
            LastDamage = 0f;
            recent.Clear();
        }

        void Update()
        {
            while (recent.Count > 0 && Time.time - recent.Peek().time > window)
                recent.Dequeue();
            float sum = 0f;
            foreach (var r in recent)
                sum += r.damage;
            RecentDamage = sum;
            var lines = new List<string> { "THE LAB  (nothing here is kept)" };
            if (Last != null)
                lines.Add($"{Last.name}:  health {Mathf.Max(0f, Last.Health):0} / {Last.maxHealth:0}   stagger {Last.Stagger:0} / {Last.staggerCapacity:0}");
            else
                lines.Add("Strike something.");
            lines.Add($"last blow {LastDamage:0}   landed {Landed}   blocked {Blocked}");
            lines.Add($"last {window:0} s: {sum:0} damage ({sum / window:0.0} a second)");
            if (apprentice != null)
                lines.Add($"your stamina {apprentice.Stamina:0} / {apprentice.maxStamina:0}   health {apprentice.Health:0} / {apprentice.maxHealth:0}");
            text.text = string.Join("\n", lines);
        }
    }
}
