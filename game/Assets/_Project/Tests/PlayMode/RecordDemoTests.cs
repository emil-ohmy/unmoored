using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MyGame.Tests
{
    // Not a check: records a scripted demo session (a small bot plays the Drowned Village) as JPEG frames at a fixed
    // frame rate, plus a log of every sound (clip, time, position) and the listener's path. tools/record-demo.sh
    // mixes the audio offline from that log and muxes an MP4 (real-time audio can't keep in step with frame capture).
    // Runs only when RECORD is set:  RECORD=<dir> Unity -batchmode -runTests -testFilter RecordDemoTests ...
    public class RecordDemoTests
    {
        const int Fps = 30, Width = 1280, Height = 720;

        [Serializable] class Frame { public float t, x, y, z, music; }
        [Serializable] class Shot { public float t, x, y, z, volume, pitch, blend, min, max; public string clip; }
        [Serializable] class Loop { public float x, y, z, volume, offset, blend, min, max; public string clip; }
        [Serializable] class Log { public int fps; public string music; public List<Frame> frames = new List<Frame>(); public List<Shot> shots = new List<Shot>(); public List<Loop> loops = new List<Loop>(); }

        string dir;
        RenderTexture rt;
        Texture2D readback;
        Log log;
        Transform listener;
        AudioSource music;
        ScriptedInput input;
        Apprentice apprentice;

        [UnityTest]
        public IEnumerator Record_DemoSession()
        {
            dir = Environment.GetEnvironmentVariable("RECORD");
            if (string.IsNullOrEmpty(dir))
                Assert.Ignore("set RECORD to record");
            Directory.CreateDirectory(Path.Combine(dir, "frames"));
            foreach (var old in Directory.GetFiles(Path.Combine(dir, "frames"), "*.jpg"))
                File.Delete(old);

            yield return SceneManager.LoadSceneAsync("Main");
            yield return null;
            Time.captureFramerate = Fps; // game time advances exactly 1/Fps per frame, however long a frame takes
            apprentice = UnityEngine.Object.FindAnyObjectByType<Apprentice>();
            apprentice.Input = input = new ScriptedInput();
            var laden = UnityEngine.Object.FindObjectsByType<Laden>(FindObjectsSortMode.None);
            var target = laden.OrderByDescending(l => l.transform.position.x).First(); // the one with its back turned
            var post = UnityEngine.Object.FindAnyObjectByType<MooringPost>();

            // Render the game camera (HUD included, laid out for this size) into a texture we read every frame.
            var cam = Camera.main;
            rt = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB) { antiAliasing = 4 };
            cam.targetTexture = rt;
            readback = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            foreach (var canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = cam;
                canvas.planeDistance = 1f;
            }

            yield return null;
            log = new Log { fps = Fps };
            listener = UnityEngine.Object.FindAnyObjectByType<AudioListener>().transform;
            music = UnityEngine.Object.FindAnyObjectByType<MusicPlayer>().GetComponent<AudioSource>();
            log.music = AssetPath(music.clip);
            foreach (var torch in UnityEngine.Object.FindObjectsByType<TorchFlame>(FindObjectsSortMode.None))
            {
                var s = torch.GetComponent<AudioSource>();
                var p = torch.transform.position;
                log.loops.Add(new Loop { clip = AssetPath(s.clip), x = p.x, y = p.y, z = p.z, volume = s.volume, offset = s.time,
                                         blend = s.spatialBlend, min = s.minDistance, max = s.maxDistance });
            }
            float t0 = Time.time;
            void OnPlayed(AudioClip clip, AudioSource s)
            {
                var p = s.transform.position;
                log.shots.Add(new Shot { t = Time.time - t0, clip = AssetPath(clip), x = p.x, y = p.y, z = p.z, volume = s.volume,
                                         pitch = s.pitch, blend = s.spatialBlend, min = s.minDistance, max = s.maxDistance });
            }
            Sfx.Played += OnPlayed;
            try
            {
                // 1. On the landing, in the dark: Rest at the Mooring post; the lantern lights and the bow lantern grows.
                for (int i = 0; i < 45; i++) { Snap(t0); yield return null; }
                input.PressInteract();
                for (int i = 0; i < 60; i++) { Snap(t0); yield return null; }

                // 2. Walk the boardwalk towards the torchlit quay.
                for (int i = 0; i < 300 && apprentice.transform.position.z < -5.5f; i++)
                {
                    input.Move = Vector2.up;
                    Snap(t0);
                    yield return null;
                }

                // 3. Sneak up behind the Laden that has its back turned, and strike before it notices.
                for (int i = 0; i < 240 && target.CurrentState == Soured.State.Idle; i++)
                {
                    Vector3 behind = target.transform.position - target.transform.forward * 1.9f - apprentice.transform.position;
                    behind.y = 0f;
                    input.Move = behind.magnitude > 0.3f ? new Vector2(behind.x, behind.z).normalized * Mathf.Clamp01(behind.magnitude) : Vector2.zero;
                    if (behind.magnitude < 0.35f)
                    {
                        input.Move = Vector2.zero;
                        input.PressLight();
                    }
                    Snap(t0);
                    yield return null;
                }
                input.Move = Vector2.zero;
                input.PressLock();
                for (int i = 0; i < 20; i++) { Snap(t0); yield return null; }

                // 4. The fight: keep a respectful distance while its guard is up, test the guard once, dash out of
                //    every slam, punish every recovery.
                bool testedGuard = false, dashedThisSwing = false;
                int punishSwings = 0;
                for (int i = 0; i < Fps * 45 && target.IsAlive && apprentice.IsAlive; i++)
                {
                    Vector3 to = target.transform.position - apprentice.transform.position;
                    to.y = 0f;
                    float d = to.magnitude;
                    Vector2 toward = new Vector2(to.x, to.z).normalized;
                    Vector2 around = new Vector2(-toward.y, toward.x);
                    input.Move = Vector2.zero;

                    if (apprentice.LockTarget == null && target.IsAlive)
                        input.PressLock();
                    if (target.CurrentState == Soured.State.Telegraph)
                    {
                        if (!dashedThisSwing && d < 3.2f && target.StateTime > target.telegraphDuration * 0.55f)
                        {
                            input.Move = around; // sideways out of the slam's line
                            input.PressDash();
                            dashedThisSwing = true;
                        }
                    }
                    else if (target.CurrentState == Soured.State.Recover || target.CurrentState == Soured.State.Staggered)
                    {
                        dashedThisSwing = false;
                        if (d > 1.9f)
                            input.Move = toward;
                        else if (apprentice.CurrentState == Apprentice.State.Free && i % 8 == 0)
                        {
                            if (target.CurrentState == Soured.State.Staggered)
                                input.PressHeavy();
                            else
                                input.PressLight();
                            punishSwings++;
                        }
                    }
                    else
                    {
                        dashedThisSwing = false;
                        if (!testedGuard && target.Guarding && d < 2.5f)
                        {
                            input.PressLight(); // clang: the heap blocks
                            testedGuard = true;
                        }
                        else if (apprentice.Health < 45f && apprentice.HealingCharges > 0 && d > 3f)
                            input.PressHeal();
                        else if (d < 2.7f)
                            input.Move = -toward * 0.6f + around * 0.4f;
                        else if (d > 3.6f)
                            input.Move = toward * 0.8f;
                        else
                            input.Move = around * 0.5f; // circle it
                    }
                    Snap(t0);
                    yield return null;
                }

                // 5. Let it settle (if the Apprentice fell, show it waking at the Mooring post).
                input.Move = Vector2.zero;
                int settle = apprentice.IsAlive ? 60 : Mathf.CeilToInt((apprentice.respawnDelay + 2.5f) * Fps);
                for (int i = 0; i < settle; i++) { Snap(t0); yield return null; }
            }
            finally
            {
                Sfx.Played -= OnPlayed;
                Time.captureFramerate = 0;
                cam.targetTexture = null;
                File.WriteAllText(Path.Combine(dir, "recording.json"), JsonUtility.ToJson(log));
                Debug.Log($"[Record] {log.frames.Count} frames, {log.shots.Count} sounds -> {dir}");
            }
        }

        // Save the frame just rendered and where the listener was.
        void Snap(float t0)
        {
            var previous = RenderTexture.active;
            RenderTexture.active = rt;
            readback.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
            readback.Apply();
            RenderTexture.active = previous;
            File.WriteAllBytes(Path.Combine(dir, "frames", $"{log.frames.Count:D5}.jpg"), readback.EncodeToJPG(90));
            var p = listener.position;
            log.frames.Add(new Frame { t = Time.time - t0, x = p.x, y = p.y, z = p.z, music = music.volume });
        }

        static string AssetPath(AudioClip clip)
        {
#if UNITY_EDITOR
            return clip != null ? UnityEditor.AssetDatabase.GetAssetPath(clip) : "";
#else
            return "";
#endif
        }
    }
}
