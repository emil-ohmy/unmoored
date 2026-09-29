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

        // The basics: a heavy blocked by the heap (frame by frame), the heal, and the water by the torches.
        [UnityTest]
        public IEnumerator Capture_BlockedHeavy_Heal_Water()
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
            var laden = UnityEngine.Object.FindAnyObjectByType<Laden>();
            laden.chaseSpeed = laden.guardSpeed = 0f;
            laden.attackRange = 0f;
            laden.sweepChanceInFront = 0f;
            // From the usual side: the camera behind the Apprentice, looking at the heap (the Laden turns to face it).
            laden.transform.rotation = Quaternion.LookRotation(Vector3.back); // facing the Apprentice, so it sees it and guards
            Vector3 front = laden.transform.position + Vector3.back * 2.7f;
            apprentice.Teleport(new Vector3(front.x, 0.55f, front.z));
            apprentice.transform.rotation = Quaternion.LookRotation(Vector3.forward);
            var cam = Camera.main;
            cam.GetComponent<CameraRig>().enabled = false;
            var pitch = Quaternion.Euler(50f, 0f, 0f);
            Vector3 mid = (laden.transform.position + apprentice.transform.position) / 2f;
            cam.transform.SetPositionAndRotation(mid + pitch * new Vector3(0f, 0f, -8f), pitch);
            for (int i = 0; i < 45 && !laden.Guarding; i++)
                yield return null;
            Capture(outDir, "polish-warmup");
            input.PressHeavy();
            for (int i = 1; i <= 24; i++)
            {
                yield return null;
                if (i >= 12)
                    Capture(outDir, $"polish-heavy-{i:D2}"); // every frame from 0.4 s: the slam, contact, the bounce
            }

            laden.gameObject.SetActive(false);
            yield return new WaitForSeconds(1f);
            cam.transform.SetPositionAndRotation(apprentice.transform.position + pitch * new Vector3(0f, 0.6f, -6f), pitch);
            input.PressHeal();
            foreach (var (frames, name) in new[] { (6, "a"), (8, "b"), (8, "c"), (4, "d"), (8, "e") })
            {
                for (int f = 0; f < frames; f++)
                    yield return null;
                Capture(outDir, $"polish-heal-{name}");
            }

            // Water: the boardwalk with its lanterns, and a torch by the quay's edge.
            cam.GetComponent<CameraRig>().enabled = true;
            apprentice.Teleport(new Vector3(2.4f, 0.55f, -14f));
            for (int f = 0; f < 20; f++)
                yield return null;
            for (int i = 0; i < 3; i++)
            {
                for (int f = 0; f < 5; f++)
                    yield return null;
                Capture(outDir, $"polish-water-{i}");
            }
            apprentice.Teleport(new Vector3(2.4f, 0.55f, -5.5f));
            for (int f = 0; f < 20; f++)
                yield return null;
            Capture(outDir, "polish-water-quay");
            Time.captureFramerate = 0;
        }

        // The Laden archer: the draw (its arrow glowing on the bow), the glint as the aim is set, the arrow in flight,
        // and a perfect block nullifying one.
        [UnityTest]
        public IEnumerator Capture_Archer()
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
            UnityEngine.Object.FindAnyObjectByType<Laden>().gameObject.SetActive(false);
            var archer = UnityEngine.Object.FindAnyObjectByType<LadenArcher>();
            archer.retreatSpeed = 0f;
            archer.volleyChance = 1f; // the first attack a volley, then single shots for the deflection
            Vector3 front = archer.transform.forward;
            front.y = 0f;
            Vector3 spot = archer.transform.position + front.normalized * 6.5f;
            apprentice.Teleport(new Vector3(spot.x, 0.55f, spot.z));
            apprentice.transform.rotation = Quaternion.LookRotation(-front);
            var cam = Camera.main;
            cam.GetComponent<CameraRig>().enabled = false;
            var pitch = Quaternion.Euler(50f, 0f, 0f);
            Vector3 mid = (archer.transform.position + apprentice.transform.position) / 2f;
            cam.transform.SetPositionAndRotation(mid + pitch * new Vector3(0f, 0f, -11f), pitch);
            Capture(outDir, "archer-warmup");
            for (int i = 0; i < 96; i++)
            {
                yield return null;
                if (i % 3 == 0)
                    Capture(outDir, $"archer-{i:D2}");
            }
            archer.volleyChance = 0f;
            apprentice.RestoreFully();
            // A perfect block: raise it as the next arrow arrives.
            for (int i = 0; i < 150; i++)
            {
                bool close = false;
                foreach (var arrow in UnityEngine.Object.FindObjectsByType<Arrow>(FindObjectsSortMode.None))
                    close |= !arrow.Stuck && Vector3.Distance(arrow.transform.position, apprentice.transform.position + Vector3.up) < 1.4f;
                if (close)
                {
                    input.BlockHeld = true;
                    for (int f = 0; f < 16; f++)
                    {
                        yield return null;
                        if (f % 2 == 0)
                            Capture(outDir, $"archer-parry-{f:D2}");
                    }
                    input.BlockHeld = false;
                    break;
                }
                yield return null;
            }
            Time.captureFramerate = 0;
        }

        // The loop from the game camera: arriving in the yard and at the crossroads, and the gate, barred and opened.
        [UnityTest]
        public IEnumerator Capture_Loop()
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
            foreach (var soured in UnityEngine.Object.FindObjectsByType<Soured>(FindObjectsSortMode.None))
                soured.aggroRadius = 0f; // stand still for the pictures
            Capture(outDir, "loop-warmup"); // the first render after loading can come out blank
            foreach (var (spot, name) in new[] { (new Vector3(0f, 0.55f, 0f), "loop-quay-centre"), (new Vector3(-24.6f, 0.55f, 0f), "loop-yard-centre"),
                                                 (new Vector3(-15.5f, 0.55f, -2f), "loop-yard"), (new Vector3(-25.5f, 0.55f, -16f), "loop-crossroads"),
                                                 (new Vector3(-3.5f, 0.55f, -25.6f), "loop-gate-barred") })
            {
                apprentice.Teleport(spot);
                for (int f = 0; f < 20; f++)
                    yield return null;
                Capture(outDir, name);
            }
            apprentice.Teleport(new Vector3(-1.4f, 0.55f, -25.6f));
            for (int f = 0; f < 10; f++)
                yield return null;
            input.PressInteract();
            for (int f = 0; f < 45; f++)
                yield return null;
            Capture(outDir, "loop-gate-open");
            Time.captureFramerate = 0;
        }

        // The Hook, with the UI: the islet seen from the yard, the post marked, the tether mid-pull, the charge found.
        [UnityTest]
        public IEnumerator Capture_Hook()
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
            foreach (var soured in UnityEngine.Object.FindObjectsByType<Soured>(FindObjectsSortMode.None))
                soured.gameObject.SetActive(false);
            var cam = Camera.main;
            var rt = new RenderTexture(1600, 900, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB) { antiAliasing = 4 };
            cam.targetTexture = rt;
            foreach (var canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = cam;
                canvas.planeDistance = 1f;
            }
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
                apprentice.Teleport(new Vector3(-29f, 0.55f, 2.4f));
                apprentice.transform.rotation = Quaternion.LookRotation(Vector3.forward);
                for (int f = 0; f < 25; f++)
                    yield return null;
                Shot("hook-1-islet-unreachable");
                apprentice.GrantHook();
                for (int f = 0; f < 30; f++)
                {
                    input.Move = f < 6 ? Vector2.up : Vector2.zero;
                    yield return null;
                }
                Shot("hook-2-marked");
                input.PressHook();
                for (int f = 0; f < 20; f++)
                {
                    yield return null;
                    if (f == 3 || f == 9 || f == 14)
                        Shot($"hook-3-pull-{f:D2}");
                }
                for (int f = 0; f < 15; f++)
                    yield return null;
                input.PressInteract();
                for (int f = 0; f < 25; f++)
                    yield return null;
                Shot("hook-4-charge-found");
            }
            finally
            {
                Time.captureFramerate = 0;
                cam.targetTexture = null;
            }
        }

        // A lit, fog-free map of the whole slice from straight above: for checking the layout (not how it looks in play).
        [UnityTest]
        public IEnumerator Capture_Map()
        {
            string outDir = Environment.GetEnvironmentVariable("SHOTS");
            if (string.IsNullOrEmpty(outDir))
                Assert.Ignore("set SHOTS to capture");
            Directory.CreateDirectory(outDir);
            yield return SceneManager.LoadSceneAsync("Main");
            yield return null;
            var cam = Camera.main;
            cam.GetComponent<CameraRig>().enabled = false;
            RenderSettings.fog = false;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.55f, 0.55f, 0.6f);
            cam.orthographic = true;
            cam.orthographicSize = 22f;
            cam.farClipPlane = 300f;
            cam.transform.SetPositionAndRotation(new Vector3(-13f, 80f, -12f), Quaternion.Euler(90f, 0f, 0f));
            yield return null;
            Capture(outDir, "map-warmup", 1600, 1600);
            yield return null;
            Capture(outDir, "map", 1600, 1600);
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

                // The merchant soul, its wares, and fitting the Charm at the post.
                var merchant = UnityEngine.Object.FindAnyObjectByType<Merchant>();
                var post = UnityEngine.Object.FindAnyObjectByType<MooringPost>();
                Vector3 towardsPost = post.RespawnPoint - merchant.transform.position;
                towardsPost.y = 0f;
                apprentice.Teleport(merchant.transform.position + towardsPost.normalized * 1.1f);
                yield return Frames(20);
                Shot("ui-3a-trade-prompt");
                apprentice.GainObols(150);
                input.PressInteract();
                yield return Frames(12);
                Shot("ui-3b-shop");
                input.PressInteract();
                yield return Frames(8);
                Shot("ui-3c-bought");
                input.PressDash();
                apprentice.Teleport(post.RespawnPoint);
                yield return Frames(10);
                input.PressInteract();
                yield return Frames(12);
                input.PressInteract();
                yield return Frames(8);
                Shot("ui-3d-post-fitted");
                input.PressDash();
                yield return Frames(10);
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
