using MyGame;
using UnityEditor;
using UnityEngine;

// The Orchard of Last Autumn's lab (Scenes/Lab_Orchard.unity): its architecture and its feeling, first. Three terraces
// stepping up a hillside out of the water (each 1.4 m above the last: drops you can take, one way; dry-stone steps
// up), rows of apple and pear trees, the press house (a loft over its floor), an orchard wall with a gateway along the
// top, the things left where they were the night the harvest didn't come, lanterns on crooks with moths round them,
// leaves always drifting down, mist lying low in the rows, and the harvest moon (OrchardNight). The Tester's Post
// sets the Drowned City's Soured down here until the Orchard has its own.
public static partial class MainSceneBuilder
{
    const string OrchardLabPath = "Assets/_Project/Scenes/Lab_Orchard.unity";

    // The terraces' tops (each model's origin is at the water, the centre of its south face).
    const float GroundTop = 0.5f, MiddleTop = 1.9f, UpperTop = 3.3f;
    const float GroundZ = -8f, MiddleZ = 2f, UpperZ = 10f, OrchardBackZ = 18f, OrchardHalfWidth = 16f;

    [MenuItem("Assets/MyGame/Build Lab (Orchard)")]
    static void BuildOrchardLabFromMenu()
    {
        if (!UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;
        BuildOrchardLab();
    }

    public static void BuildOrchardLabFromCommandLine() => BuildOrchardLab();

    static void BuildOrchardLab()
    {
        var scene = BeginScene(new Vector3(0f, 0f, 4f), OrchardNight);
        Terraces();
        OrchardRows();
        PressHouse(new Vector3(10.5f, MiddleTop, 6.1f));
        UpperTerrace();
        OrchardLanterns();
        OrchardAir();

        // The posts at the bottom terrace's west end, where you come in.
        MooringPostAt(new Vector3(-13.5f, GroundTop, -6.2f), 0f);
        var tester = TesterPostAt(new Vector3(-11.2f, GroundTop, -6.6f), 15f);
        tester.spawner = LabTemplates();
        var start = new GameObject("Start Point").transform;
        start.position = new Vector3(-12f, GroundTop, -3.8f);
        var entrance = new GameObject("Area Entrance").AddComponent<AreaEntrance>();
        entrance.transform.position = start.position + Vector3.up;
        entrance.areaName = "The Orchard of Last Autumn (Lab)";
        var apprentice = Apprentice(start.position);
        apprentice.GetComponent<Equipment>().startingCharms = new[] { Charm.TestersKnot.Id };
        var readout = new GameObject("Lab Readout").AddComponent<LabReadout>();
        readout.font = AssetDatabase.LoadAssetAtPath<Font>(Fonts + "IMFellEnglish-Regular.ttf");
        tester.readout = readout;

        // Dry as old leaves: everything here burns harder, and longer.
        var traits = new GameObject("Area Traits").AddComponent<AreaTraits>();
        traits.fireHarm = 1.75f;
        traits.burnLength = 1.5f;

        FinishScene(scene, OrchardLabPath, start, apprentice, "music_drowned_dark"); // (no music of its own yet)
    }

    // Three terraces up out of the water, steps between them, and walls along the high ends (too tall to drop from).
    static void Terraces()
    {
        Place("Environment/terrace_ground", new Vector3(0f, 0f, GroundZ), 0f);
        Place("Environment/terrace_middle", new Vector3(0f, 0f, MiddleZ), 0f);
        Place("Environment/terrace_upper", new Vector3(0f, 0f, UpperZ), 0f);
        var ramps = new GameObject("Step Ramps").transform;
        void Steps(float x, float fromTop, float faceZ)
        {
            var foot = new Vector3(x, fromTop, faceZ - 7 * 0.333f);
            Place("Environment/orchard_steps", foot, 0f);
            Ramp(ramps, foot, new Vector3(x, fromTop + 1.4f, faceZ), 2f);
        }
        Steps(-9f, GroundTop, MiddleZ);
        Steps(3f, MiddleTop, UpperZ);
        // The middle and upper terraces' ends are too tall above the water to step off: walled.
        foreach (float x in new[] { -OrchardHalfWidth + 0.3f, OrchardHalfWidth - 0.3f })
        {
            for (float z = MiddleZ + 0.4f; z < OrchardBackZ - 0.4f; z += 4f)
                Place("Environment/orchard_wall_long", new Vector3(x, z < UpperZ ? MiddleTop : UpperTop, z), -90f);
            Place("Environment/orchard_wall_end", new Vector3(x, MiddleTop, MiddleZ + 0.3f), 0f);
        }
    }

    // A tree, with only its trunk solid (the crown overhead stops nothing: arrows, the Hook). It doesn't open up for the
    // camera as buildings do: a crown over the Apprentice hides little, and they can be seen through the leaves' gaps.
    static GameObject OrchardTree(string variant, Vector3 at, float yaw)
    {
        var go = Place("Environment/orchard_tree_" + variant, at, yaw);
        foreach (var c in go.GetComponentsInChildren<Collider>())
            Object.DestroyImmediate(c);
        var trunk = go.AddComponent<CapsuleCollider>();
        trunk.center = new Vector3(0f, variant == "flooded" ? 0.5f : 1.0f, 0f);
        trunk.radius = 0.22f;
        trunk.height = 2.2f;
        return go;
    }

    static GameObject OrchardProp(string variant, Vector3 at, float yaw) => Place("Environment/orchard_props_" + variant, at, yaw);

    // The rows: apple trees on the bottom and middle terraces (in lines, a few old), the flooded ones standing in the
    // water below; baskets and crates under them, fallen fruit in the leaves.
    static void OrchardRows()
    {
        var rng = new System.Random(7);
        string Apple() => rng.NextDouble() < 0.25 ? "apple_old" : "apple";
        float Yaw() => (float)rng.NextDouble() * 360f;
        foreach (float x in new[] { -6f, -1.5f, 3f, 7.5f, 12f })
            OrchardTree(Apple(), new Vector3(x, GroundTop, -2.2f), Yaw());
        foreach (float x in new[] { -3.5f, 5.5f, 12.5f })
            OrchardTree("flooded", new Vector3(x, 0f, GroundZ - 1.8f), Yaw());
        foreach (float x in new[] { -13f, -5f, -1f })
            OrchardTree(Apple(), new Vector3(x, MiddleTop, 4.6f), Yaw());
        foreach (float x in new[] { -11f, -7f, -3f, 1f })
            OrchardTree(x == -7f ? "pear" : Apple(), new Vector3(x, MiddleTop, 8.2f), Yaw());

        // Baskets and crates waiting under them.
        OrchardProp("basket", new Vector3(3.9f, GroundTop, -3.4f), 20f);
        OrchardProp("basket", new Vector3(-6.8f, GroundTop, -3.1f), -40f);
        OrchardProp("crate", new Vector3(8.6f, GroundTop, -3.3f), 12f);
        OrchardProp("crate", new Vector3(9.4f, GroundTop, -3.6f), -6f);
        OrchardProp("basket", new Vector3(-4.2f, MiddleTop, 3.5f), 60f);
        OrchardProp("crate", new Vector3(-12.2f, MiddleTop, 3.4f), 3f);
        foreach (var p in new[] { new Vector3(-1.2f, GroundTop, -1.4f), new Vector3(7.8f, GroundTop, -1.2f), new Vector3(-12.6f, MiddleTop, 5.6f),
                                  new Vector3(-7.4f, MiddleTop, 9.1f), new Vector3(0.6f, MiddleTop, 7.4f) })
            OrchardProp("apples", p, Yaw());
    }

    // The press house: its floor, its loft and its roof each fade with the storeys above the Apprentice inside; the
    // press and its barrels on the floor; its stair's ramp (for the Soured's ways).
    static void PressHouse(Vector3 at)
    {
        var root = new GameObject("Press House");
        root.transform.position = at;
        var storeys = root.AddComponent<Storeys>();
        storeys.footprint = new Vector2(4.5f, 3.5f);
        GameObject Part(string model)
        {
            var go = Place("Environment/" + model, at, 0f);
            go.transform.SetParent(root.transform, true);
            return go;
        }
        var ground = Part("press_house_ground");
        var loft = Part("press_house_loft");
        var roof = Part("press_house_roof");
        storeys.storeys = new[]
        {
            new Storeys.Storey { floor = at.y, parts = new[] { ground } },
            new Storeys.Storey { floor = at.y + 2.4f, parts = new[] { loft } },
            new Storeys.Storey { floor = at.y + 99f, parts = new[] { roof } }, // (never stood on: gone whenever you're inside)
        };
        void Inside(GameObject prop, GameObject storey) => prop.transform.SetParent(storey.transform, true);
        Inside(OrchardProp("press", at + new Vector3(-2.2f, 0f, -1.9f), 0f), ground);
        Inside(Place("Environment/barrel", at + new Vector3(-3.6f, 0f, 2.6f), 0f), ground);
        Inside(Place("Environment/barrel", at + new Vector3(-2.8f, 0f, 2.8f), 40f), ground);
        Inside(OrchardProp("crate", at + new Vector3(0.6f, 0f, 2.6f), 5f), ground);
        Inside(OrchardProp("basket", at + new Vector3(-0.4f, 2.4f, 2.6f), 30f), loft);
        Inside(OrchardProp("crate", at + new Vector3(1.6f, 2.4f, 2.7f), -10f), loft);
        var light = PointLight(root.transform, at + new Vector3(-1f, 1.9f, 0.5f), 6f, 1.8f, LanternLight); // (a lamp left lit inside)
        Flicker(light, 0.1f, 4f, 0.01f);
        light.transform.SetParent(ground.transform, true);
        Ramp(new GameObject("Press House Stair Ramp").transform, at + new Vector3(3.68f, 0f, -3.0f), at + new Vector3(3.68f, 2.4f, 1.0f), 1.0f);
    }

    // The top terrace: the old trees, the well, the hives, the scarecrow keeping watch over nothing, and the orchard
    // wall along its back, its gateway (where the Gardener's glasshouse will be).
    static void UpperTerrace()
    {
        OrchardTree("apple_old", new Vector3(-12f, UpperTop, 13f), 40f);
        OrchardTree("apple", new Vector3(-4.5f, UpperTop, 15f), 200f);
        OrchardTree("pear", new Vector3(10f, UpperTop, 15.2f), 100f);
        OrchardProp("well", new Vector3(5.5f, UpperTop, 13.6f), 15f);
        foreach (float x in new[] { 12.2f, 13.4f, 14.6f })
            OrchardProp("skep", new Vector3(x, UpperTop, 16.6f), x * 20f);
        OrchardProp("scarecrow", new Vector3(-8f, UpperTop, 12.6f), 160f);
        OrchardProp("basket", new Vector3(-3.4f, UpperTop, 14.2f), 0f);
        OrchardProp("apples", new Vector3(9.4f, UpperTop, 14.1f), 0f);
        float wallZ = OrchardBackZ - 0.35f;
        for (float x = -OrchardHalfWidth + 0.3f; x < OrchardHalfWidth - 0.4f; x += 4f)
            Place(Mathf.Abs(x + 2f) < 0.5f ? "Environment/orchard_wall_gate" : "Environment/orchard_wall_long", new Vector3(x, UpperTop, wallZ), 0f);
    }

    // Lanterns on crooks along the ways, each warm, flickering, a few small pale moths circling it.
    static void OrchardLanterns()
    {
        int lit = 0;
        foreach (var (at, yaw) in new[] {
                     (new Vector3(-10.5f, GroundTop, -4.2f), 30f), (new Vector3(0.5f, GroundTop, -5.2f), -20f), (new Vector3(10f, GroundTop, -5f), 10f),
                     (new Vector3(-8f, MiddleTop, 2.8f), 90f), (new Vector3(3.6f, MiddleTop, 5.2f), -60f),
                     (new Vector3(-9.5f, UpperTop, 11f), 20f), (new Vector3(1.6f, UpperTop, 11.2f), -80f), (new Vector3(13f, UpperTop, 11.4f), 200f) })
        {
            var lantern = OrchardProp("lantern", at, yaw);
            Vector3 lamp = GlowCentre(lantern, "OrchardLantern_Glow");
            var light = PointLight(lantern.transform, lamp, 7f, 2.4f, LanternLight);
            Flicker(light, 0.08f, 3f, 0.01f);
            LanternMoths(lantern.transform, lamp);
            // The Moths: a flock round the middle terrace's east lantern (by the press house), another round the top
            // terrace's middle one; not by the way in (they'd be on you the moment you came).
            if (lit == 4 || lit == 6)
                MothFlockAt(lit == 4 ? "Moths (middle terrace)" : "Moths (top terrace)", lamp, light.transform, lit == 4 ? 6 : 5);
            lit++;
        }
    }

    // A few small pale moths circling a lamp (not the Moths: just moths; but you'll think of them later).
    static void LanternMoths(Transform parent, Vector3 lamp)
    {
        var go = new GameObject("Moths");
        go.transform.SetParent(parent, true);
        go.transform.position = lamp;
        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.loop = true;
        main.prewarm = true;
        main.startLifetime = 5f;
        main.startSpeed = 0.2f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.035f, 0.06f);
        main.startColor = new Color(1f, 0.94f, 0.82f, 0.85f);
        main.maxParticles = 8;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        var emission = ps.emission;
        emission.rateOverTime = 1.6f;
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.35f;
        var orbit = ps.velocityOverLifetime;
        orbit.enabled = true;
        orbit.space = ParticleSystemSimulationSpace.Local;
        orbit.orbitalY = new ParticleSystem.MinMaxCurve(2.2f, 3.4f);
        orbit.radial = new ParticleSystem.MinMaxCurve(-0.05f, 0.05f);
        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.5f;
        noise.frequency = 2.5f;
        var fade = ps.colorOverLifetime;
        fade.enabled = true;
        var g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                  new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.15f), new GradientAlphaKey(1f, 0.85f), new GradientAlphaKey(0f, 1f) });
        fade.color = g;
        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = sparkMaterial;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    // The air: leaves always drifting down through it (a few at a time, never many), and mist lying low on each terrace.
    static void OrchardAir()
    {
        var air = new GameObject("Orchard Air").transform;
        // Falling leaves, over the whole hillside.
        {
            var go = new GameObject("Falling Leaves");
            go.transform.SetParent(air, false);
            go.transform.position = new Vector3(0f, 9f, 4f);
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true;
            main.prewarm = true;
            main.duration = 10f;
            main.startLifetime = 26f;
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.26f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 900;
            var colours = new Gradient();
            colours.SetKeys(new[] { new GradientColorKey(Hex("#b5552f"), 0f), new GradientColorKey(Hex("#c9923a"), 0.35f),
                                    new GradientColorKey(Hex("#8a2e27"), 0.7f), new GradientColorKey(Hex("#7a5432"), 1f) },
                            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            main.startColor = new ParticleSystem.MinMaxGradient(colours) { mode = ParticleSystemGradientMode.RandomColor };
            var emission = ps.emission;
            emission.rateOverTime = 26f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(34f, 0.5f, 30f);
            var fall = ps.velocityOverLifetime;
            fall.enabled = true;
            fall.space = ParticleSystemSimulationSpace.World;
            fall.x = new ParticleSystem.MinMaxCurve(0.08f, 0.2f);   // (a breath of air from the west)
            fall.y = new ParticleSystem.MinMaxCurve(-0.5f, -0.32f);
            fall.z = new ParticleSystem.MinMaxCurve(-0.05f, 0.05f);
            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.45f;
            noise.frequency = 0.35f;
            noise.scrollSpeed = 0.15f;
            var spin = ps.rotationOverLifetime;
            spin.enabled = true;
            spin.z = new ParticleSystem.MinMaxCurve(-2.2f, 2.2f);
            var land = ps.collision;
            land.enabled = true;
            land.type = ParticleSystemCollisionType.World;
            land.mode = ParticleSystemCollisionMode.Collision3D;
            land.quality = ParticleSystemCollisionQuality.Low;
            land.lifetimeLoss = 1f; // (down: gone, into the leaves already there)
            land.radiusScale = 0.3f;
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = LitParticleMaterial("M_Leaf", LeafTexture("T_Leaf"), 0.9f);
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        // Mist lying low on each terrace, and on the water.
        foreach (var (y, z0, z1) in new[] { (0.15f, -14f, GroundZ), (GroundTop + 0.35f, GroundZ, MiddleZ), (MiddleTop + 0.35f, MiddleZ, UpperZ), (UpperTop + 0.35f, UpperZ, OrchardBackZ) })
        {
            var go = new GameObject("Mist");
            go.transform.SetParent(air, false);
            go.transform.position = new Vector3(0f, y, (z0 + z1) / 2f);
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true;
            main.prewarm = true;
            main.startLifetime = 18f;
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(5f, 8f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = new Color(0.78f, 0.7f, 0.82f, 0.045f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 80;
            var emission = ps.emission;
            emission.rateOverTime = 3.5f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(34f, 0.3f, Mathf.Abs(z1 - z0));
            var drift = ps.velocityOverLifetime;
            drift.enabled = true;
            drift.space = ParticleSystemSimulationSpace.World;
            drift.x = new ParticleSystem.MinMaxCurve(0.06f, 0.18f);
            drift.y = new ParticleSystem.MinMaxCurve(0f, 0f);
            drift.z = new ParticleSystem.MinMaxCurve(-0.03f, 0.03f);
            var spin = ps.rotationOverLifetime;
            spin.enabled = true;
            spin.z = new ParticleSystem.MinMaxCurve(-0.05f, 0.05f);
            var fade = ps.colorOverLifetime;
            fade.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.3f), new GradientAlphaKey(1f, 0.7f), new GradientAlphaKey(0f, 1f) });
            fade.color = g;
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = LitParticleMaterial("M_Mist", RadialTexture("T_SoftDot", ring: false), 1f);
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.renderMode = ParticleSystemRenderMode.HorizontalBillboard; // (lying flat over the ground)
        }
    }

    static Material LitParticleMaterial(string name, Texture2D texture, float ambient)
    {
        var m = GeneratedMaterial(name, "MyGame/ParticleLit");
        m.SetTexture("_MainTex", texture);
        m.SetFloat("_Ambient", ambient);
        EditorUtility.SetDirty(m);
        return m;
    }

    // A 32x32 alpha texture: a leaf, almond-shaped, a little notch at its stalk, its midrib a shade thinner.
    static Texture2D LeafTexture(string name)
    {
        string path = $"{Generated}/{name}.asset";
        var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (existing != null)
            return existing;
        const int size = 32;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = name };
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float u = (x + 0.5f) / size - 0.5f, v = (y + 0.5f) / size;
                float half = 0.36f * Mathf.Sin(Mathf.Clamp01(v) * Mathf.PI) * (v < 0.15f ? v / 0.15f : 1f);
                float a = Mathf.Clamp01((half - Mathf.Abs(u)) * 30f);
                if (Mathf.Abs(u) < 0.025f && v > 0.08f && v < 0.9f)
                    a *= 0.7f;
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        tex.Apply();
        AssetDatabase.CreateAsset(tex, path);
        return tex;
    }
}
