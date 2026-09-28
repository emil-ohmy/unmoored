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
            var target = laden[0].transform.position.x > laden[1].transform.position.x ? laden[0] : laden[1];
            (target == laden[0] ? laden[1] : laden[0]).gameObject.SetActive(false);
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
            for (float t = 0; t < 4f && target.CurrentState != Soured.State.Telegraph; t += Time.deltaTime)
            {
                Vector3 to = target.transform.position - apprentice.transform.position;
                input.Move = to.magnitude > 1.8f ? new Vector2(to.x, to.z).normalized : Vector2.zero;
                yield return null;
            }
            input.Move = Vector2.zero;
            yield return new WaitForSeconds(target.telegraphDuration * 0.8f);
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
            yield return new WaitForSeconds(0.4f);
        }

        static void Capture(string outDir, string name)
        {
            var cam = Camera.main;
            var rt = new RenderTexture(1600, 900, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB) { antiAliasing = 4 };
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
