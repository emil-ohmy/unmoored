using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MyGame.Tests
{
    // Not a check: plays a short scripted sequence and saves screenshots of it (animations included), for
    // reviewing the look without opening the editor. Runs only when the SHOTS environment variable is set:
    //   SHOTS=builds/shots Unity -batchmode -runTests -testPlatform PlayMode -testFilter PlayCaptureTests ...
    public class PlayCaptureTests
    {
        [UnityTest]
        public IEnumerator Capture_ApproachWindupDashSwing()
        {
            string outDir = Environment.GetEnvironmentVariable("SHOTS");
            if (string.IsNullOrEmpty(outDir))
                Assert.Ignore("set SHOTS to capture");
            Directory.CreateDirectory(outDir);
            Application.targetFrameRate = 60;
            yield return SceneManager.LoadSceneAsync("Main");
            yield return null;
            var apprentice = UnityEngine.Object.FindAnyObjectByType<Apprentice>();
            var input = new ScriptedInput();
            apprentice.Input = input;
            var laden = UnityEngine.Object.FindObjectsByType<Laden>(FindObjectsSortMode.None);
            var target = laden[0];
            float aggro = target.aggroRadius, range = target.attackRange;
            target.aggroRadius = 0f; // hold it unaware for the first frame (open: heap low)
            target.attackRange = 0f; // and don't let it attack until after the blocked swing
            yield return new WaitForSeconds(0.5f);
            Capture(outDir, "play-0-landing");
            input.Move = Vector2.up; // walk in along the boardwalk
            for (float t = 0; t < 6f && apprentice.transform.position.z < -12f; t += Time.deltaTime)
                yield return null;
            Capture(outDir, "play-0b-boardwalk");
            for (float t = 0; t < 6f && apprentice.transform.position.z < -4.5f; t += Time.deltaTime)
                yield return null;
            input.Move = Vector2.zero;
            yield return new WaitForSeconds(0.3f);
            Capture(outDir, "play-1-start");
            target.aggroRadius = aggro;
            yield return new WaitForSeconds(0.8f);
            Capture(outDir, "play-1b-approach-open");

            // Walk towards the Laden; it notices and lifts its guard. Swing into the guard: sparks and a shove.
            bool capturedGuard = false, swung = false;
            for (float t = 0; t < 6f && !swung; t += Time.deltaTime)
            {
                Vector3 to = target.transform.position - apprentice.transform.position;
                to.y = 0f;
                input.Move = to.magnitude > 2.2f ? new Vector2(to.x, to.z).normalized : Vector2.zero;
                if (!capturedGuard && target.Guarding)
                {
                    Capture(outDir, "play-2-guard-up");
                    capturedGuard = true;
                }
                if (target.Guarding && to.magnitude < 2.4f)
                {
                    input.PressLight();
                    swung = true;
                }
                yield return null;
            }
            input.Move = Vector2.zero;
            yield return new WaitForSecondsRealtime(0.16f); // lands mid hit-stop, sparks flying
            Capture(outDir, "play-2b-blocked");
            target.attackRange = range;
            for (float t = 0; t < 10f && target.CurrentState != Soured.State.Telegraph; t += Time.deltaTime)
            {
                Vector3 to = target.transform.position - apprentice.transform.position;
                input.Move = to.magnitude > 1.8f ? new Vector2(to.x, to.z).normalized : Vector2.zero;
                yield return null;
            }
            input.Move = Vector2.zero;
            if (target.CurrentAttack == null)
                Assert.Inconclusive("the Laden never wound up; no wind-up or dash shots this time");
            yield return new WaitForSeconds(target.CurrentAttack.telegraph * 0.8f);
            Capture(outDir, "play-3-laden-windup");

            // Dash sideways out of the slam.
            Vector3 side = Vector3.Cross(Vector3.up, target.transform.position - apprentice.transform.position).normalized;
            input.Move = new Vector2(side.x, side.z);
            input.PressDash();
            yield return new WaitForSeconds(0.15f);
            Capture(outDir, "play-4-dash");
            input.Move = Vector2.zero;
            yield return new WaitForSeconds(0.35f);
            Capture(outDir, "play-5-laden-recover");

            // Punish: turn to it and swing while its heap is on the ground.
            Vector3 back = target.transform.position - apprentice.transform.position;
            input.Move = new Vector2(back.x, back.z).normalized * 0.1f;
            input.PressLight();
            yield return new WaitForSeconds(0.2f);
            Capture(outDir, "play-6-swing");
            input.Move = Vector2.zero;

            // Lock on: the target's health bar (with the damage trail) floats above it. The HUD normally draws
            // straight to the screen; route it through the camera so the capture includes it.
            foreach (var canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = Camera.main;
                canvas.planeDistance = 1f;
            }
            input.PressLock();
            yield return new WaitForSeconds(0.25f);
            Capture(outDir, "play-7-lock-on", Screen.width, Screen.height); // HUD layout follows the screen size
            yield return new WaitForSeconds(0.4f);
        }

        // Close-ups of the death dissolve (with the fare streaming into the lantern) and of a torch flame moving.
        [UnityTest]
        public IEnumerator Capture_DeathAndFlames()
        {
            string outDir = Environment.GetEnvironmentVariable("SHOTS");
            if (string.IsNullOrEmpty(outDir))
                Assert.Ignore("set SHOTS to capture");
            Directory.CreateDirectory(outDir);
            yield return SceneManager.LoadSceneAsync("Main");
            yield return null;
            Time.captureFramerate = 30;
            var apprentice = UnityEngine.Object.FindAnyObjectByType<Apprentice>();
            apprentice.Input = new ScriptedInput();
            var laden = UnityEngine.Object.FindAnyObjectByType<Laden>();
            apprentice.Teleport(laden.transform.position - laden.transform.forward * 3f + Vector3.up * 0.1f);
            yield return new WaitForSeconds(0.5f);
            var cam = Camera.main;
            var rig = cam.GetComponent<CameraRig>();
            rig.enabled = false;
            var pitch = Quaternion.Euler(50f, 0f, 0f);
            Vector3 mid = (laden.transform.position + apprentice.transform.position) / 2f;
            cam.transform.SetPositionAndRotation(mid + Vector3.forward * 0.8f + pitch * new Vector3(0f, 0f, -9f), pitch);
            Capture(outDir, "death-warmup"); // the first render after moving the camera can come out blank
            laden.TakeHit(new Hit { Damage = 1000f, From = apprentice.transform.position });
            for (int i = 0; i < 8; i++)
            {
                for (int f = 0; f < 6; f++)
                    yield return null; // 0.2 s at 30 fps
                Capture(outDir, $"death-{i}");
            }
            for (int f = 0; f < 45; f++)
                yield return null;
            Capture(outDir, "death-lantern"); // the dropped lantern, glowing where the Laden fell

            var torch = UnityEngine.Object.FindAnyObjectByType<TorchFlame>().transform;
            cam.transform.SetPositionAndRotation(torch.position + Quaternion.Euler(20f, 0f, 0f) * new Vector3(0f, 0f, -2.2f), Quaternion.Euler(20f, 0f, 0f));
            for (int i = 0; i < 6; i++)
            {
                for (int f = 0; f < 4; f++)
                    yield return null;
                Capture(outDir, $"torch-{i}");
            }
            Time.captureFramerate = 0;
        }

        // The screen messages and HUD, rendered with the UI (the camera draws into a texture, canvases in camera space).
        [UnityTest]
        public IEnumerator Capture_Ui()
        {
            string outDir = Environment.GetEnvironmentVariable("SHOTS");
            if (string.IsNullOrEmpty(outDir))
                Assert.Ignore("set SHOTS to capture");
            Directory.CreateDirectory(outDir);
            yield return SceneManager.LoadSceneAsync("Main");
            yield return null;
            Time.captureFramerate = 30;
            var apprentice = UnityEngine.Object.FindAnyObjectByType<Apprentice>();
            var input = new ScriptedInput();
            apprentice.Input = input;
            var cam = Camera.main;
            var rt = new RenderTexture(1600, 900, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB) { antiAliasing = 4 };
            cam.targetTexture = rt;
            foreach (var canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = cam;
                canvas.planeDistance = 1f;
            }
            IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }
            void Shot(string name)
            {
                var previous = RenderTexture.active;
                RenderTexture.active = rt;
                var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                tex.Apply();
                RenderTexture.active = previous;
                File.WriteAllBytes(Path.Combine(outDir, name + ".png"), tex.EncodeToPNG());
            }
            try
            {
                yield return Frames(20);
                Shot("ui-1-prompt");
                input.PressInteract();
                yield return Frames(20);
                Shot("ui-2-lantern-lit");
                yield return Frames(40);
                Shot("ui-3-lantern-lit-late");
                apprentice.TakeHit(30f, apprentice.transform.position + Vector3.forward);
                yield return Frames(4);
                Shot("ui-4-hurt-trail");
                yield return Frames(30);
                input.PressHeal();
                yield return Frames(40);
                Shot("ui-5-charge-spent");

                apprentice.Teleport(new Vector3(2.4f, 0.55f, -11.5f));
                for (int i = 0; i < 90; i++)
                {
                    input.Move = i < 25 ? Vector2.up : Vector2.zero;
                    yield return null;
                    if (i == 45)
                        Shot("ui-6-area-title");
                }
                yield return Frames(60);
                Shot("ui-7-area-title-late");

                var laden = UnityEngine.Object.FindAnyObjectByType<Laden>();
                apprentice.Teleport(laden.transform.position - laden.transform.forward * 3.5f + Vector3.up * 0.1f);
                laden.TakeHit(new Hit { Damage = 30f, From = apprentice.transform.position });
                input.PressLock();
                yield return Frames(20);
                Shot("ui-8-lock");

                apprentice.GainObols(160);
                yield return Frames(12);
                Shot("ui-8b-obols");
                Vector3 fell = apprentice.transform.position;
                apprentice.TakeHit(1000f, apprentice.transform.position + Vector3.forward);
                foreach (var (frames, name) in new[] { (15, "a"), (21, "b"), (24, "c"), (27, "d"), (12, "e"), (15, "f"), (24, "g") })
                {
                    yield return Frames(frames);
                    Shot($"ui-9-death-{name}");
                }
                laden.gameObject.SetActive(false); // keep the Shade shots calm
                apprentice.Teleport(fell + Vector3.back * 2.5f);
                yield return Frames(20);
                Shot("ui-10-shade");
                for (int i = 0; i < 40; i++)
                {
                    input.Move = i < 14 ? Vector2.up : Vector2.zero;
                    yield return null;
                    if (i == 22)
                        Shot("ui-11-claim");
                }
                yield return Frames(20);
                Shot("ui-12-claimed");
            }
            finally
            {
                Time.captureFramerate = 0;
                cam.targetTexture = null;
            }
        }

        static void Capture(string outDir, string name, int width = 1600, int height = 900)
        {
            var cam = Camera.main;
            var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB) { antiAliasing = 4 };
            var request = new RenderPipeline.StandardRequest { destination = rt };
            RenderPipeline.SubmitRenderRequest(cam, request);
            var previous = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();
            RenderTexture.active = previous;
            rt.Release();
            File.WriteAllBytes(Path.Combine(outDir, name + ".png"), tex.EncodeToPNG());
        }
    }
}
