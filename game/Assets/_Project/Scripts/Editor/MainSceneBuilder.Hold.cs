using MyGame;
using UnityEditor;
using UnityEngine;

// The Hold's lab (the game's opening, before the Hub; docs/brief.md): a flooded undercroft far beneath a great jetty,
// a forest of pilings in black water, where something has kept what it's taken a very long time (hold.py). Dark,
// eerie, desolate, dead: the Apprentice's lantern nearly the only light, thin shafts of grey daylight through cracks
// far above, drips out of the dark, a low mist on the water, ash drifting down; and eyes in the dark (the Shades,
// only watching for now) that shrink back from the lantern. Its route: where you wake, the Boardwalk, the
// Foundations, the Pens, the Mooring islet, the Basin's edge (the capstan in its middle, its chains up into the dark),
// the Climb up to the hatch and the grey light (the way out, to the Hub).
public static partial class MainSceneBuilder
{
    const string HoldLabPath = "Assets/_Project/Scenes/Lab_Hold.unity";

    // The Hold: no moon to speak of, the dark close and black, the colour all but gone out of it (dead, not stopped:
    // not the Hall's grey photograph, but a place the life went out of), the grain heavy, the edges closing in.
    static readonly AreaNight HoldNight = new AreaNight
    {
        volume = "HoldVolume", moon = Hex("#7c8aa0"), moonIntensity = 0.16f, moonAngles = new Vector3(70f, -20f, 0f), shadowStrength = 0.6f,
        sky = Hex("#1c2230"), equator = Hex("#12151c"), ground = Hex("#07080a"),
        fog = Hex("#06070a"), fogStart = 9f, fogEnd = 30f,
        saturation = -32f, contrast = 18f, exposure = 0.1f, vignette = 0.48f, grain = 0.45f, filter = Hex("#e6edf6"),
    };
    static readonly Color HoldDaylight = Hex("#b9c3cf"); // (the grey of the day, far above)

    [MenuItem("Assets/MyGame/Build Lab (The Hold)")]
    static void BuildHoldLabFromMenu()
    {
        if (!UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;
        BuildHoldLab();
    }

    public static void BuildHoldLabFromCommandLine() => BuildHoldLab();

    static void BuildHoldLab()
    {
        var scene = BeginScene(new Vector3(0f, 0f, 30f), HoldNight);
        var root = new GameObject("The Hold").transform;
        var hold = Place("Environment/hold_hold", Vector3.zero, 0f);
        hold.transform.SetParent(root, true);
        Vector3 At(float e, float n, float u = 0.5f) => new Vector3(e, u, n);

        // A few of the heaped lanterns loose along the way, as if dropped.
        foreach (var (e, n, yaw, lying, floor) in new[] { (0.6f, 1.2f, 30f, true, 0.5f), (0.7f, 9.5f, 200f, false, 0.44f), (7.4f, 21.5f, 110f, true, 0.5f), (-3.4f, 41.0f, 300f, false, 0.3f) })
        {
            var lantern = Place("Environment/hold_lantern_dead", At(e, n, floor + (lying ? 0.085f : 0f)), yaw);
            lantern.transform.SetParent(root, true);
            if (lying) // (on its side)
                lantern.transform.rotation = Quaternion.AngleAxis(84f, Quaternion.Euler(0f, yaw, 0f) * Vector3.right) * lantern.transform.rotation;
        }

        // Grey daylight through cracks far above: thin shafts falling on the route (a pool where each lands, dust in it).
        foreach (var (e, n, u, tall) in new[] { (0.0f, 0.3f, 0.5f, 13f), (0.0f, 6.0f, 0.5f, 13f), (-0.6f, 12.4f, 0.5f, 12f), (7.0f, 19.8f, 0.5f, 13f),
                                                (8.6f, 24.5f, 0.5f, 12f), (5.0f, 29.5f, 0.5f, 14f), (-1.0f, 35.0f, 0.5f, 12f), (-6.4f, 43.7f, 0.3f, 13f), (-8.6f, 51.0f, 0.3f, 12f) })
            LightShaft(root, At(e, n, u), Mathf.Min(tall, 9f), 0.4f);

        // Its light besides: next to none. A cold glow far off over the basin (the capstan's dark shape against it),
        // the grey of the day falling down the hatch at the Climb's top, and your lantern.
        var basinGlow = PointLight(root, At(8f, 52f, 9f), 26f, 0.9f, Hex("#5d6c80"));
        basinGlow.shadows = LightShadows.None;
        // The way out: the day pouring down through the hatch onto the top landing (the brightest thing in the Hold).
        var hatch = PointLight(root, At(-9f, 62.8f, 11.8f), 12f, 6f, HoldDaylight);
        hatch.shadows = LightShadows.Soft;
        LightShaft(root, At(-9f, 62.8f, 10f), 2.6f, 1.1f, brightness: 3.5f);
        // Behind the capstan, a cold glow on the water: its dark shape against it, seen from the ledge.
        var behind = PointLight(root, At(9f, 61f, 2.5f), 15f, 1.6f, Hex("#6d7f99"));
        behind.shadows = LightShadows.None;

        // The air: a low mist on the black water, ash (or dust: something) drifting down out of the dark, drips.
        HoldMist(root, At(2f, 28f, 0.15f), new Vector3(60f, 0.3f, 90f));
        HoldAsh(root, At(2f, 28f, 9f), new Vector3(46f, 1f, 80f));
        var drips = new GameObject("Drips").AddComponent<Drips>();
        drips.transform.SetParent(root, true);
        drips.transform.position = At(2f, 28f, 0f);
        drips.size = new Vector3(46f, 0f, 80f);
        drips.everySecond = 0.35f;
        drips.from = 9f;
        drips.dripSound = Clips("hold_drip");

        // The Shades' eyes in the dark, out among the pilings off the route, watching.
        foreach (var (e, n, u) in new[] { (-8.5f, 3.5f, 0.9f), (8.5f, 6.5f, 1.4f), (15.5f, 17.5f, 0.8f), (15.5f, 26.5f, 1.2f), (-4.5f, 26.5f, 0.7f), (-9.5f, 33.5f, 1.6f),
                                          (15.5f, 35.5f, 1.1f), (-15.5f, 44.5f, 0.9f), (-21.5f, 56.5f, 0.8f), (-2.5f, 66.5f, 1.3f), (-20.5f, 67.5f, 1.0f), (-11.5f, 69.5f, 1.8f),
                                          // (and nearer, at every stop: just beyond where they shrink back, in sight as you come)
                                          (-4.5f, 4.5f, 0.8f), (5.0f, 2.0f, 1.2f), (-4.8f, 10.0f, 0.9f), (4.6f, 13.0f, 1.4f), (3.0f, 23.0f, 1.0f),
                                          (-2.5f, 28.0f, 0.9f), (13.5f, 31.0f, 1.2f), (-5.5f, 32.0f, 1.0f), (3.5f, 38.5f, 0.7f), (-10.6f, 41.3f, 0.9f),
                                          (-1.5f, 46.5f, 0.6f), (-13.5f, 52.5f, 1.0f), (-4.5f, 59.0f, 0.8f) })
            ShadeEyes(root, At(e, n, u));

        // Sounds: the jetty creaking far above, now and then; the day at the hatch.
        var creak = new GameObject("The Jetty Overhead").AddComponent<AmbientOneShots>();
        creak.transform.SetParent(root, true);
        creak.transform.position = At(2f, 28f, 12f);
        creak.clips = Clips("hold_creak");
        creak.every = new Vector2(9f, 22f);
        creak.spread = 30f;
        creak.volume = 0.55f;
        var day = new GameObject("The Day Above (the hatch)", typeof(AudioSource)).GetComponent<AudioSource>();
        day.transform.SetParent(root, true);
        day.transform.position = At(-9f, 62.8f, 11.5f);
        day.clip = Clips("hold_hatch") is var hatchClips && hatchClips.Length > 0 ? hatchClips[0] : null;
        day.loop = true;
        day.playOnAwake = true;
        day.spatialBlend = 1f;
        day.rolloffMode = AudioRolloffMode.Linear;
        day.minDistance = 3f;
        day.maxDistance = 22f;
        day.volume = 0.45f; // (louder than the bed, as made: it only rises as you near the hatch)
        day.dopplerLevel = 0f;

        // The lab's posts on the Mooring islet; where you wake.
        MooringPostAt(At(-1f, 35.5f), 180f);
        var tester = TesterPostAt(At(0.6f, 34.2f), 225f);
        tester.spawner = LabTemplates();
        var start = new GameObject("Start Point").transform;
        start.position = At(0f, 0f);
        var entrance = new GameObject("Area Entrance").AddComponent<AreaEntrance>();
        entrance.transform.position = start.position + Vector3.up;
        entrance.areaName = "The Hold";
        var apprentice = Apprentice(start.position);
        var bow = apprentice.GetComponent<BowLantern>();
        bow.minRange = 6.5f;      // (down here, even your first light reaches a little further: the dark is all there is)
        bow.minIntensity = 2.6f;
        apprentice.GetComponent<Equipment>().startingCharms = new[] { Charm.TestersKnot.Id };
        var readout = new GameObject("Lab Readout").AddComponent<LabReadout>();
        readout.font = AssetDatabase.LoadAssetAtPath<Font>(Fonts + "IMFellEnglish-Regular.ttf");
        tester.readout = readout;

        FinishScene(scene, HoldLabPath, start, apprentice, "music_hold");
    }

    // A shaft of grey daylight falling from a crack `tall` above `floor`: crossed soft planes (bright where they come
    // through, fading as they fall), a dim pool of light where it lands, and dust turning slowly in it.
    static void LightShaft(Transform parent, Vector3 floor, float tall, float radius, float brightness = 1f)
    {
        var go = new GameObject("Light Shaft", typeof(MeshFilter), typeof(MeshRenderer));
        go.transform.SetParent(parent, true);
        go.transform.position = floor;
        var mesh = new Mesh { name = "Light Shaft" };
        var verts = new System.Collections.Generic.List<Vector3>();
        var colours = new System.Collections.Generic.List<Color>();
        var uvs = new System.Collections.Generic.List<Vector2>();
        var tris = new System.Collections.Generic.List<int>();
        const int planes = 6, rows = 8; // (many faint planes: a soft round column from any side, no edges to see)
        for (int p = 0; p < planes; p++)
        {
            float a = Mathf.PI * p / planes;
            Vector3 side = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
            int first = verts.Count;
            for (int r = 0; r <= rows; r++)
            {
                float v = (float)r / rows;                         // 0 at the floor, 1 at the crack
                float width = radius * Mathf.Lerp(1.35f, 0.55f, v); // (spreading as it falls)
                // Faint, strongest a little above the floor, gone well before it reaches up to the camera.
                // Faint, fading to nothing at both ends (no edges where it meets the floor, gone before the camera); a
                // short bright one (from an opening) strongest high up, where it comes through.
                float peak = brightness > 1f ? 0.7f : 0.35f;
                float alpha = brightness * 0.042f * Mathf.Sin(Mathf.PI * Mathf.Pow(v, Mathf.Log(0.5f) / Mathf.Log(peak)));
                Color c = new Color(HoldDaylight.r, HoldDaylight.g, HoldDaylight.b, alpha);
                verts.Add(-side * width + Vector3.up * tall * v);
                verts.Add(side * width + Vector3.up * tall * v);
                colours.Add(c);
                colours.Add(c);
                uvs.Add(new Vector2(0f, v));
                uvs.Add(new Vector2(1f, v));
            }
            for (int r = 0; r < rows; r++)
            {
                int i = first + r * 2;
                tris.AddRange(new[] { i, i + 2, i + 1, i + 1, i + 2, i + 3 });
            }
        }
        mesh.SetVertices(verts);
        mesh.SetColors(colours);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateBounds();
        go.GetComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = go.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = ParticleMaterial("M_LightShaft", ShaftTexture("T_LightShaft"));
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        go.AddComponent<NavMeshIgnore>();

        var pool = new GameObject("Light Shaft (its pool)").AddComponent<Light>();
        pool.transform.SetParent(go.transform, false);
        pool.transform.localPosition = Vector3.up * Mathf.Min(tall, 7f);
        pool.transform.rotation = Quaternion.LookRotation(Vector3.down);
        pool.type = LightType.Spot;
        pool.spotAngle = Mathf.Clamp(Mathf.Atan2(radius * 2.2f, Mathf.Min(tall, 7f)) * Mathf.Rad2Deg * 2f, 10f, 60f);
        pool.innerSpotAngle = pool.spotAngle * 0.4f;
        pool.range = Mathf.Min(tall, 7f) + 1.5f;
        pool.intensity = 3.2f * Mathf.Sqrt(brightness);
        pool.color = HoldDaylight;
        pool.shadows = LightShadows.None;

        var dust = new GameObject("Light Shaft (dust)").AddComponent<Motes>();
        dust.transform.SetParent(go.transform, false);
        dust.transform.localPosition = Vector3.up * 1.2f;
        dust.radius = radius * 0.9f;
        dust.perSecond = 1.4f;
        dust.colour = new Color(0.85f, 0.88f, 0.92f, 0.5f);
    }

    // A 32x8 alpha texture: soft at both edges across, full down its length (a beam's cross-section).
    static Texture2D ShaftTexture(string name)
    {
        string path = $"{Generated}/{name}.asset";
        var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (existing != null)
            return existing;
        const int w = 32, h = 8;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = name };
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float u = (x + 0.5f) / w * 2f - 1f;
                float a = Mathf.Clamp01(1f - u * u);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a * a));
            }
        tex.Apply();
        AssetDatabase.CreateAsset(tex, path);
        return tex;
    }

    // Mist lying low on the black water, barely moving.
    static void HoldMist(Transform parent, Vector3 centre, Vector3 size)
    {
        var go = new GameObject("Mist");
        go.transform.SetParent(parent, false);
        go.transform.position = centre;
        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.loop = true;
        main.prewarm = true;
        main.startLifetime = 24f;
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(5f, 9f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startColor = new Color(0.62f, 0.66f, 0.72f, 0.05f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 160;
        var emission = ps.emission;
        emission.rateOverTime = 6f;
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = size;
        var drift = ps.velocityOverLifetime;
        drift.enabled = true;
        drift.space = ParticleSystemSimulationSpace.World;
        drift.x = new ParticleSystem.MinMaxCurve(-0.04f, 0.04f);
        drift.y = new ParticleSystem.MinMaxCurve(0f, 0f);
        drift.z = new ParticleSystem.MinMaxCurve(0.02f, 0.06f);
        var spin = ps.rotationOverLifetime;
        spin.enabled = true;
        spin.z = new ParticleSystem.MinMaxCurve(-0.03f, 0.03f);
        var fade = ps.colorOverLifetime;
        fade.enabled = true;
        var g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                  new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.3f), new GradientAlphaKey(1f, 0.7f), new GradientAlphaKey(0f, 1f) });
        fade.color = g;
        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = LitParticleMaterial("M_Mist", RadialTexture("T_SoftDot", ring: false), 1f);
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.renderMode = ParticleSystemRenderMode.HorizontalBillboard;
        go.AddComponent<NavMeshIgnore>();
    }

    // Something drifting down out of the dark, very slowly: ash, or dust (a few at a time), catching your light.
    static void HoldAsh(Transform parent, Vector3 top, Vector3 size)
    {
        var go = new GameObject("Ash");
        go.transform.SetParent(parent, false);
        go.transform.position = top;
        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.loop = true;
        main.prewarm = true;
        main.startLifetime = 26f;
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.06f);
        main.startColor = new Color(0.78f, 0.78f, 0.8f, 0.7f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 900;
        var emission = ps.emission;
        emission.rateOverTime = 30f;
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = size;
        var drift = ps.velocityOverLifetime;
        drift.enabled = true;
        drift.space = ParticleSystemSimulationSpace.World;
        drift.x = new ParticleSystem.MinMaxCurve(-0.06f, 0.06f);
        drift.y = new ParticleSystem.MinMaxCurve(-0.42f, -0.28f);
        drift.z = new ParticleSystem.MinMaxCurve(-0.06f, 0.06f);
        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.12f;
        noise.frequency = 0.25f;
        var fade = ps.colorOverLifetime;
        fade.enabled = true;
        var g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                  new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.1f), new GradientAlphaKey(1f, 0.85f), new GradientAlphaKey(0f, 1f) });
        fade.color = g;
        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = LitParticleMaterial("M_Ash", RadialTexture("T_SoftDot", ring: false), 0.15f); // (lit: seen where the light is)
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        go.AddComponent<NavMeshIgnore>();
    }

    // A pair of pale eyes in the dark (a Shade, only watching).
    static void ShadeEyes(Transform parent, Vector3 at)
    {
        var go = new GameObject("Eyes in the Dark").AddComponent<Watcher>();
        go.transform.SetParent(parent, true);
        go.transform.position = at;
        var eye = GeneratedMaterial("M_ShadeEye", "MyGame/Toon");
        eye.SetColor("_BaseColor", new Color(0.86f, 0.88f, 0.84f));
        eye.EnableKeyword("_EMISSION");
        eye.SetColor("_EmissionColor", new Color(0.86f, 0.88f, 0.84f) * 2.2f);
        EditorUtility.SetDirty(eye);
        Transform Eye(string name)
        {
            var e = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            e.name = name;
            Object.DestroyImmediate(e.GetComponent<Collider>());
            e.transform.SetParent(go.transform, false);
            var r = e.GetComponent<MeshRenderer>();
            r.sharedMaterial = eye;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return e.transform;
        }
        go.eyeL = Eye("Eye (left)");
        go.eyeR = Eye("Eye (right)");
        go.whisperSound = Clips("hold_whisper");
    }
}
