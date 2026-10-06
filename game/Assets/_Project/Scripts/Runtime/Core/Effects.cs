using UnityEngine;

namespace MyGame
{
    // One-shot visual effects, built in code from particle systems. Materials come from the scene builder.
    public sealed class Effects : MonoBehaviour
    {
        public static Effects Instance { get; private set; }

        public Material sparkMaterial; // MyGame/Particle with a soft dot
        public Material flashMaterial; // MyGame/Particle with a ring
        public Material flameMaterial; // MyGame/Particle with a tongue of flame (burning: pitch, a Soured alight, a tarred oar)
        public Material strawMaterial; // MyGame/ParticleLit (straw is lit, not light: a scarecrow bursting)

        ParticleSystem sparks, flash, fare, glints, wisps, streaks;
        ParticleSystem.Particle[] fareBuffer = new ParticleSystem.Particle[256];
        ParticleSystem flames, straw, smoke;
        Transform fareTarget;
        BowLantern fareLantern;

        public int LiveBlockSparks => sparks != null ? sparks.particleCount : 0;
        public int LiveFare => fare != null ? fare.particleCount : 0;

        void Awake()
        {
            Instance = this;
            sparks = Build("Block Sparks", sparkMaterial, stretch: true);
            // Streaks of light (energy drawn in): stretched like the sparks, but weightless, longer, many.
            streaks = Build("Energy Streaks", sparkMaterial, stretch: true);
            var streakMain = streaks.main;
            streakMain.gravityModifier = 0f;
            streakMain.maxParticles = 500;
            var streakRenderer = streaks.GetComponent<ParticleSystemRenderer>();
            streakRenderer.velocityScale = 0.09f;
            streakRenderer.lengthScale = 1f;
            flash = Build("Block Flash", flashMaterial, stretch: false);
            fare = BuildFare();
            glints = BuildGlints();
            wisps = BuildWisps();
            flames = BuildFlames();
            straw = BuildStraw();
            smoke = BuildSmoke();
        }

        // Where a defeated Soured's fare flies to: the Apprentice's lantern.
        public void SetFareTarget(Transform target, BowLantern lantern)
        {
            fareTarget = target;
            fareLantern = lantern;
        }

        // One mote of a defeated Soured's fare: it bursts out, then spirals into the Apprentice's lantern.
        public void EmitFare(Vector3 at)
        {
            fare.Emit(new ParticleSystem.EmitParams
            {
                position = at,
                velocity = Random.insideUnitSphere * 1.2f + Vector3.up * 1.5f, // a small puff, then the pull takes over
                startLifetime = 5f,
                startSize = Random.Range(0.08f, 0.15f),
                startColor = Color.Lerp(new Color(0.75f, 1f, 0.5f), new Color(1f, 0.85f, 0.4f), Random.value),
                applyShapeToPosition = false,
            }, 1);
        }

        void LateUpdate()
        {
            if (fare == null || fare.particleCount == 0 || fareTarget == null)
                return;
            int count = fare.GetParticles(fareBuffer);
            float dt = Time.deltaTime;
            int arrived = 0;
            for (int i = 0; i < count; i++)
            {
                var p = fareBuffer[i];
                Vector3 to = fareTarget.position - p.position;
                float distance = to.magnitude;
                if (distance < 0.3f)
                {
                    p.remainingLifetime = 0f;
                    arrived++;
                }
                else
                {
                    float age = p.startLifetime - p.remainingLifetime;
                    // Sucked in: an accelerating pull with a slight spiral, so the fare arrives as one stream in ~1 s.
                    Vector3 seek = to / distance * (8f + age * 18f);
                    Vector3 swirl = Vector3.Cross(to, Vector3.up).normalized * Mathf.Clamp(distance * 0.8f, 0f, 2f);
                    p.velocity = Vector3.Lerp(p.velocity, seek + swirl, Mathf.Clamp01(dt * (5f + age * 10f)));
                }
                fareBuffer[i] = p;
            }
            fare.SetParticles(fareBuffer, count);
            if (arrived > 0)
                fareLantern?.Pulse(0.4f * arrived);
        }

        ParticleSystem BuildFare()
        {
            var ps = Build("Fare Motes", sparkMaterial, stretch: false);
            var main = ps.main;
            main.maxParticles = 256;
            var size = ps.sizeOverLifetime;
            size.enabled = false;
            var colour = ps.colorOverLifetime;
            colour.enabled = false;
            var trail = ps.trails;
            trail.enabled = false;
            return ps;
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        // A perfect block: a big golden ring and a burst of bright sparks.
        public void ParryFlash(Vector3 position, Vector3 normal)
        {
            var rotation = Quaternion.LookRotation(normal.sqrMagnitude > 0.001f ? normal : Vector3.up);
            sparks.transform.SetPositionAndRotation(position, rotation);
            flash.transform.position = position;
            flash.Emit(new ParticleSystem.EmitParams { startSize = 1.5f, startColor = new Color(1f, 0.85f, 0.45f, 1f), applyShapeToPosition = false }, 1);
            sparks.Emit(new ParticleSystem.EmitParams { startColor = new Color(1f, 0.95f, 0.7f, 1f) }, 45);
        }

        // A Soured notices the Apprentice: its eyes flare sickly green.
        public void AlertFlare(Vector3 position)
        {
            flash.transform.position = position;
            var p = new ParticleSystem.EmitParams { startSize = 0.6f, startColor = new Color(0.7f, 1f, 0.45f, 1f), applyShapeToPosition = false };
            flash.Emit(p, 1);
        }

        // A key item held up: a big cold ring bursting out from it.
        public void KeyItemRing(Vector3 position)
        {
            flash.transform.position = position;
            flash.Emit(new ParticleSystem.EmitParams { startSize = 3.2f, startLifetime = 0.7f, startColor = new Color(0.62f, 0.77f, 1f, 0.9f), applyShapeToPosition = false }, 1);
            sparks.transform.SetPositionAndRotation(position, Quaternion.LookRotation(Vector3.up));
            for (int i = 0; i < 30; i++)
                sparks.Emit(new ParticleSystem.EmitParams { startColor = new Color(0.8f, 0.9f, 1f, 1f), startLifetime = Random.Range(0.4f, 0.8f) }, 1);
        }

        // A mote of the bow lantern's light drifting away as it goes out: pale, cold, rising slowly and fading.
        public void Wisp(Vector3 at) => Wisp(at, new Color(0.72f, 0.86f, 1f, 1f));

        public void Wisp(Vector3 at, Color colour)
        {
            wisps.Emit(new ParticleSystem.EmitParams
            {
                position = at + Random.insideUnitSphere * 0.12f,
                velocity = new Vector3(Random.Range(-0.25f, 0.25f), Random.Range(0.5f, 1.1f), Random.Range(-0.25f, 0.25f)),
                startLifetime = Random.Range(1.2f, 2.2f),
                startSize = Random.Range(0.06f, 0.14f),
                startColor = colour,
                applyShapeToPosition = false,
            }, 1);
        }

        // A mote drifting somewhere (a soul's light going on ahead of it).
        public void Wisp(Vector3 at, Color colour, Vector3 velocity, float lifetime)
        {
            wisps.Emit(new ParticleSystem.EmitParams
            {
                position = at, velocity = velocity, startLifetime = lifetime, startSize = Random.Range(0.07f, 0.15f),
                startColor = colour, applyShapeToPosition = false,
            }, 1);
        }

        // The oar's blade dipping in: a few droplets and a small pale ring (every stroke, so small).
        public void OarDip(Vector3 position)
        {
            sparks.transform.SetPositionAndRotation(position, Quaternion.LookRotation(Vector3.up));
            for (int i = 0; i < 7; i++)
                sparks.Emit(new ParticleSystem.EmitParams
                {
                    startColor = Color.Lerp(new Color(0.55f, 0.68f, 0.82f, 0.8f), new Color(0.85f, 0.92f, 1f, 0.8f), Random.value),
                    startSize = Random.Range(0.04f, 0.08f),
                    startLifetime = Random.Range(0.2f, 0.4f),
                }, 1);
            flash.transform.position = position;
            flash.Emit(new ParticleSystem.EmitParams { startSize = 0.7f, startLifetime = 0.4f, startColor = new Color(0.6f, 0.75f, 0.9f, 0.35f), applyShapeToPosition = false }, 1);
        }

        public int LiveWisps => wisps != null ? wisps.particleCount : 0;

        // A lick of flame (burning pitch, a Soured alight, a tarred oar): bright, rising fast, dwindling to nothing.
        public void Flame(Vector3 at, Vector3 velocity, float lifetime, float size)
        {
            if (flames == null)
                return;
            flames.Emit(new ParticleSystem.EmitParams
            {
                position = at, velocity = velocity, startLifetime = lifetime, startSize3D = new Vector3(size, size * 1.6f, size), // (a tongue: taller than wide)
                startColor = Color.Lerp(new Color(1f, 0.8f, 0.55f), Color.white, Random.value),
                applyShapeToPosition = false,
            }, 1);
        }

        public int LiveFlames => flames != null ? flames.particleCount : 0;

        // Straw thrown out of a scarecrow's coat (one bursting; the Watchman's, spilling): stalks tumbling, falling.
        public void Straw(Vector3 at, int count)
        {
            if (straw == null)
                return;
            for (int i = 0; i < count; i++)
                straw.Emit(new ParticleSystem.EmitParams
                {
                    position = at + Random.insideUnitSphere * 0.3f,
                    velocity = new Vector3(Random.Range(-2.5f, 2.5f), Random.Range(1f, 4.5f), Random.Range(-2.5f, 2.5f)),
                    startColor = Color.Lerp(new Color(0.62f, 0.5f, 0.28f), new Color(0.86f, 0.74f, 0.46f), Random.value),
                    startSize = Random.Range(0.05f, 0.09f),
                    startLifetime = Random.Range(0.9f, 1.6f),
                    applyShapeToPosition = false,
                }, 1);
        }

        // Dead leaves on a gust (Father Moth's wingbeat): blown along `velocity`, tumbling (the straw's system: lit, not light).
        public void Leaves(Vector3 at, Vector3 velocity, int count)
        {
            if (straw == null)
                return;
            for (int i = 0; i < count; i++)
                straw.Emit(new ParticleSystem.EmitParams
                {
                    position = at + Random.insideUnitSphere * 0.4f,
                    velocity = velocity * Random.Range(0.7f, 1.2f) + Random.insideUnitSphere * 1.2f + Vector3.up * Random.Range(0.5f, 2f),
                    startColor = Color.Lerp(new Color(0.42f, 0.22f, 0.1f), new Color(0.72f, 0.42f, 0.16f), Random.value),
                    startSize = Random.Range(0.1f, 0.18f),
                    startLifetime = Random.Range(0.8f, 1.4f),
                    applyShapeToPosition = false,
                }, 1);
        }

        public int LiveStraw => straw != null ? straw.particleCount : 0;

        // A thread of smoke (something smouldering: the Watchman, hidden again): a faint grey puff, rising and
        // spreading as it thins out. Lit, not light: barely there in the dark unless you're near.
        public void Smoke(Vector3 at, float alpha = 0.45f)
        {
            if (smoke == null)
                return;
            smoke.Emit(new ParticleSystem.EmitParams
            {
                position = at + Random.insideUnitSphere * 0.05f,
                velocity = new Vector3(Random.Range(-0.08f, 0.08f), Random.Range(0.35f, 0.6f), Random.Range(-0.08f, 0.08f)),
                startColor = new Color(0.62f, 0.6f, 0.58f, alpha),
                startSize = Random.Range(0.16f, 0.26f),
                startLifetime = Random.Range(1.8f, 2.6f),
                rotation = Random.Range(0f, 360f),
                applyShapeToPosition = false,
            }, 1);
        }

        public int LiveSmoke => smoke != null ? smoke.particleCount : 0;

        ParticleSystem BuildSmoke()
        {
            var ps = Build("Smoke", strawMaterial != null ? strawMaterial : sparkMaterial, stretch: false);
            var main = ps.main;
            main.maxParticles = 200;
            main.gravityModifier = 0f;
            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 3.2f)); // (spreading)
            var colour = ps.colorOverLifetime;
            colour.enabled = true;
            var fade = new Gradient();
            fade.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                         new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(0f, 1f) });
            colour.color = fade;
            var trail = ps.trails;
            trail.enabled = false;
            return ps;
        }

        ParticleSystem BuildStraw()
        {
            var ps = Build("Straw", strawMaterial != null ? strawMaterial : sparkMaterial, stretch: true);
            var main = ps.main;
            main.maxParticles = 300;
            main.gravityModifier = 0.6f;
            var drag = ps.limitVelocityOverLifetime; // (light: the air slows it)
            drag.enabled = true;
            drag.drag = 1.5f;
            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.velocityScale = 0.06f;
            renderer.lengthScale = 4f; // (stalks, not sparks)
            return ps;
        }

        ParticleSystem BuildFlames()
        {
            var ps = Build("Flames", flameMaterial != null ? flameMaterial : sparkMaterial, stretch: false);
            var main = ps.main;
            main.maxParticles = 700;
            main.gravityModifier = 0f;
            main.startRotation = new ParticleSystem.MinMaxCurve(-0.25f, 0.25f); // (a little lean either way)
            main.startSize3D = true;
            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0.1f));
            var trail = ps.trails;
            trail.enabled = false;
            var colour = ps.colorOverLifetime;
            colour.enabled = true;
            var fade = new Gradient();
            // Burning out: a pale yellow core, then orange, then a deep red as it dies, fading.
            fade.SetKeys(new[] { new GradientColorKey(new Color(1f, 0.95f, 0.75f), 0f), new GradientColorKey(new Color(1f, 0.62f, 0.25f), 0.35f),
                                 new GradientColorKey(new Color(0.75f, 0.18f, 0.08f), 1f) },
                         new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.08f), new GradientAlphaKey(0.75f, 0.5f), new GradientAlphaKey(0f, 1f) });
            colour.color = fade;
            return ps;
        }

        ParticleSystem BuildWisps()
        {
            var ps = Build("Lantern Wisps", sparkMaterial, stretch: false);
            var main = ps.main;
            main.maxParticles = 128;
            main.gravityModifier = 0f;
            var size = ps.sizeOverLifetime;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.4f)); // dwindling
            var trail = ps.trails;
            trail.enabled = false;
            var colour = ps.colorOverLifetime;
            colour.enabled = true;
            var fade = new Gradient();
            fade.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                         new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.15f), new GradientAlphaKey(0f, 1f) });
            colour.color = fade;
            return ps;
        }

        // Stone breaking: a burst of grey dust and grit thrown up (a floor section giving way).
        public void Rubble(Vector3 at, float spread)
        {
            sparks.transform.SetPositionAndRotation(at, Quaternion.LookRotation(Vector3.up));
            for (int i = 0; i < 40; i++)
                sparks.Emit(new ParticleSystem.EmitParams
                {
                    position = at + new Vector3(Random.Range(-spread, spread), 0f, Random.Range(-spread, spread)),
                    velocity = new Vector3(Random.Range(-1.5f, 1.5f), Random.Range(2f, 5f), Random.Range(-1.5f, 1.5f)),
                    startColor = Color.Lerp(new Color(0.55f, 0.52f, 0.48f, 0.9f), new Color(0.8f, 0.76f, 0.7f, 0.9f), Random.value),
                    startSize = Random.Range(0.05f, 0.14f),
                    startLifetime = Random.Range(0.4f, 0.9f),
                    applyShapeToPosition = false,
                }, 1);
        }

        // Iron dragged over stone: a few sparks kicked up behind it.
        public void Scrape(Vector3 position, Vector3 behind)
        {
            sparks.transform.SetPositionAndRotation(position, Quaternion.LookRotation((behind.normalized + Vector3.up * 0.7f).normalized));
            sparks.Emit(new ParticleSystem.EmitParams { startColor = new Color(1f, 0.82f, 0.55f, 1f), startLifetime = Random.Range(0.15f, 0.35f) }, 3);
        }

        // Stone grinding: a pinch of grey grit and dust kicked up (a floor section about to give way).
        public void Grit(Vector3 position)
        {
            sparks.transform.SetPositionAndRotation(position, Quaternion.LookRotation(Vector3.up));
            for (int i = 0; i < 3; i++)
                sparks.Emit(new ParticleSystem.EmitParams
                {
                    position = position,
                    velocity = new Vector3(Random.Range(-0.6f, 0.6f), Random.Range(0.8f, 2.2f), Random.Range(-0.6f, 0.6f)),
                    startColor = Color.Lerp(new Color(0.42f, 0.4f, 0.37f, 0.9f), new Color(0.62f, 0.58f, 0.52f, 0.9f), Random.value),
                    startSize = Random.Range(0.04f, 0.09f),
                    startLifetime = Random.Range(0.25f, 0.5f),
                    applyShapeToPosition = false,
                }, 1);
        }

        // A Healing charge takes hold: a warm ring spreading at the Apprentice's feet.
        public void HealRing(Vector3 position)
        {
            flash.transform.position = position;
            var p = new ParticleSystem.EmitParams { startSize = 2.2f, startLifetime = 0.45f, startColor = new Color(1f, 0.55f, 0.24f, 0.85f), applyShapeToPosition = false };
            flash.Emit(p, 1);
        }

        // Something falls into the water: droplets thrown up and a pale ring on the surface.
        public void Splash(Vector3 position)
        {
            sparks.transform.SetPositionAndRotation(position, Quaternion.LookRotation(Vector3.up));
            for (int i = 0; i < 34; i++)
                sparks.Emit(new ParticleSystem.EmitParams
                {
                    startColor = Color.Lerp(new Color(0.55f, 0.68f, 0.82f, 0.9f), new Color(0.85f, 0.92f, 1f, 0.9f), Random.value),
                    startSize = Random.Range(0.06f, 0.13f),
                    startLifetime = Random.Range(0.35f, 0.7f),
                }, 1);
            flash.transform.position = position;
            flash.Emit(new ParticleSystem.EmitParams { startSize = 1.8f, startLifetime = 0.5f, startColor = new Color(0.6f, 0.75f, 0.9f, 0.7f), applyShapeToPosition = false }, 1);
        }

        // Wet iron catching the moonlight: a small four-pointed twinkle that swells and eases away (hook post rings).
        // The star shape is what sets it apart from the water's own soft highlights.
        public void MoonGlint(Vector3 position)
        {
            glints.Emit(new ParticleSystem.EmitParams
            {
                position = position, velocity = Vector3.zero, startSize = 0.42f, startLifetime = 0.6f,
                rotation = Random.Range(-12f, 12f), startColor = new Color(0.9f, 0.95f, 1f, 1f), applyShapeToPosition = false,
            }, 1);
        }

        public int LiveGlints => glints != null ? glints.particleCount : 0;

        ParticleSystem BuildGlints()
        {
            var go = new GameObject("Moon Glints");
            go.transform.SetParent(transform, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 16;
            var emission = ps.emission;
            emission.enabled = false;
            var shape = ps.shape;
            shape.enabled = false;
            // A smooth swell and ease away: size and brightness together.
            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                new Keyframe(0f, 0.3f, 0f, 0f), new Keyframe(0.4f, 1f, 0f, 0f), new Keyframe(1f, 0.2f, 0f, 0f)));
            var colour = ps.colorOverLifetime;
            colour.enabled = true;
            var fade = new Gradient();
            fade.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                         new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.4f), new GradientAlphaKey(0f, 1f) });
            colour.color = fade;
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            if (sparkMaterial != null)
            {
                var glint = new Material(sparkMaterial) { name = "Glint", mainTexture = StarTexture() };
                glint.SetFloat("_Intensity", 5f); // bright enough for the core to bloom a touch, like a real glint
                renderer.sharedMaterial = glint;
            }
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return ps;
        }

        // A four-pointed twinkle: a bright core and thin rays fading out along the axes.
        static Texture2D StarTexture()
        {
            const int n = 64;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { name = "T_Glint", wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color32[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float u = (x + 0.5f) / n * 2f - 1f, v = (y + 0.5f) / n * 2f - 1f;
                    float core = Mathf.Exp(-(u * u + v * v) * 22f);
                    float rays = Mathf.Exp(-Mathf.Abs(v) * 24f) * Mathf.Pow(1f - Mathf.Abs(u), 2.5f)
                               + Mathf.Exp(-Mathf.Abs(u) * 24f) * Mathf.Pow(1f - Mathf.Abs(v), 2.5f);
                    byte a = (byte)(255 * Mathf.Clamp01(core + rays * 0.8f));
                    pixels[y * n + x] = new Color32(255, 255, 255, a);
                }
            tex.SetPixels32(pixels);
            tex.Apply(false, true);
            return tex;
        }

        // The moment a guard comes up: a small, quick glint on the shield.
        public void GuardGlint(Vector3 position)
        {
            flash.transform.position = position;
            var p = new ParticleSystem.EmitParams { startSize = 0.35f, startColor = new Color(1f, 0.95f, 0.8f, 0.8f), applyShapeToPosition = false };
            flash.Emit(p, 1);
        }

        // A blocked hit: a spray of sparks off the surface plus a quick expanding ring.
        // A streak of light, stretched along its way (the sparks' stretched particles): energy drawn in (Father Moth's
        // charge).
        public void Streak(Vector3 at, Vector3 velocity, Color colour, float lifetime, float size = 0.12f)
        {
            if (streaks == null)
                return;
            streaks.Emit(new ParticleSystem.EmitParams
            {
                position = at,
                velocity = velocity,
                startColor = colour,
                startSize = size,
                startLifetime = lifetime,
                applyShapeToPosition = false,
            }, 1);
        }

        public void BlockSpark(Vector3 position, Vector3 normal)
        {
            var rotation = Quaternion.LookRotation(normal.sqrMagnitude > 0.001f ? normal : Vector3.up);
            sparks.transform.SetPositionAndRotation(position, rotation);
            flash.transform.SetPositionAndRotation(position, rotation);
            sparks.Emit(26);
            flash.Emit(1);
        }

        ParticleSystem Build(string name, Material material, bool stretch)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 128;
            var emission = ps.emission;
            emission.enabled = false;
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            if (stretch)
            {
                main.startLifetime = new ParticleSystem.MinMaxCurve(0.15f, 0.32f);
                main.startSpeed = new ParticleSystem.MinMaxCurve(4f, 10f);
                main.startSize = new ParticleSystem.MinMaxCurve(0.07f, 0.14f);
                main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.95f, 0.75f), new Color(1f, 0.7f, 0.35f));
                main.gravityModifier = 1.2f;
                var shape = ps.shape;
                shape.shapeType = ParticleSystemShapeType.Cone;
                shape.angle = 55f;
                shape.radius = 0.05f;
                renderer.renderMode = ParticleSystemRenderMode.Stretch;
                renderer.velocityScale = 0.04f;
                renderer.lengthScale = 1.5f;
            }
            else
            {
                main.startLifetime = 0.2f;
                main.startSpeed = 0f;
                main.startSize = 0.7f;
                main.startColor = new Color(1f, 0.85f, 0.55f);
                var shape = ps.shape;
                shape.enabled = false;
                var size = ps.sizeOverLifetime;
                size.enabled = true;
                size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.5f, 1f, 2.8f));
                var colour = ps.colorOverLifetime;
                colour.enabled = true;
                var fade = new Gradient();
                fade.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                             new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
                colour.color = fade;
                renderer.renderMode = ParticleSystemRenderMode.Billboard;
            }
            return ps;
        }
    }
}
