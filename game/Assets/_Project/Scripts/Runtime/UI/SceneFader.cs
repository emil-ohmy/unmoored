using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MyGame
{
    // Between scenes (the title screen and a Journey): the screen fades to black, "Loading" and a small ember glowing
    // while the next scene loads (held a moment even if it loads at once: it reads as a crossing, not a flicker), then
    // it fades back in once the scene has settled (a save applied).
    public sealed class SceneFader : MonoBehaviour
    {
        public const float FadeOut = 0.5f, MinimumLoad = 1.1f, FadeIn = 0.9f;

        public static bool Busy => current != null;
        static SceneFader current;

        Image black, ember;
        Text label;
        CanvasGroup loading;

        public static void Load(string scene)
        {
            if (current != null)
                return;
            var go = new GameObject("Scene Fader");
            DontDestroyOnLoad(go);
            current = go.AddComponent<SceneFader>();
            current.Build();
            current.StartCoroutine(current.Run(scene));
        }

        void Build()
        {
            var canvasGo = new GameObject("Fader Canvas", typeof(Canvas), typeof(CanvasScaler));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200; // over everything
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            black = UiKit.Image(canvasGo.transform, "Black", null, Color.black.WithAlpha(0f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            UiKit.Stretch(black.rectTransform, -4f);
            var corner = UiKit.Rect(canvasGo.transform, "Loading", new Vector2(1f, 0f), new Vector2(-90, 70), new Vector2(300, 60));
            loading = corner.gameObject.AddComponent<CanvasGroup>();
            loading.alpha = 0f;
            ember = UiKit.Image(corner, "Ember", UiKit.SoftDot, UiKit.Ember, new Vector2(1f, 0.5f), new Vector2(-10, 0), new Vector2(34, 34));
            var font = FindAnyObjectByType<TitleMenu>()?.titleFont ?? FindAnyObjectByType<Hud>()?.titleFont;
            label = UiKit.Text(corner, "Label", font, 30, UiKit.Bone, TextAnchor.MiddleRight, new Vector2(1f, 0.5f), new Vector2(-48, 0), 3f);
            label.rectTransform.pivot = new Vector2(1f, 0.5f);
            label.rectTransform.sizeDelta = new Vector2(260, 50);
            label.text = "Loading";
        }

        IEnumerator Run(string scene)
        {
            for (float t = 0; t < FadeOut; t += UiKit.DeltaTime)
            {
                black.color = Color.black.WithAlpha(UiKit.Smooth(0f, FadeOut, t));
                yield return null;
            }
            black.color = Color.black;
            float started = UiKit.Now;
            var op = SceneManager.LoadSceneAsync(scene);
            op.allowSceneActivation = false;
            while (op.progress < 0.9f || UiKit.Now - started < MinimumLoad)
            {
                Loading(UiKit.Now - started);
                yield return null;
            }
            op.allowSceneActivation = true;
            while (!op.isDone)
                yield return null;
            for (int f = 0; f < 3; f++) // (let the new scene settle: a save applied, the camera placed)
            {
                Loading(UiKit.Now - started);
                yield return null;
            }
            for (float t = 0; t < FadeIn; t += UiKit.DeltaTime)
            {
                black.color = Color.black.WithAlpha(1f - UiKit.Smooth(0f, FadeIn, t));
                loading.alpha = 1f - UiKit.Smooth(0f, 0.3f, t);
                yield return null;
            }
            current = null;
            Destroy(gameObject);
        }

        // The ember breathing while it loads.
        void Loading(float age)
        {
            loading.alpha = UiKit.Smooth(0f, 0.3f, age);
            float breathe = 0.7f + 0.3f * Mathf.Sin(age * 4f);
            ember.color = UiKit.Ember.WithAlpha(breathe);
            ember.rectTransform.localScale = Vector3.one * (0.85f + 0.2f * breathe);
        }

        void OnDestroy()
        {
            if (current == this)
                current = null;
        }
    }
}
