using System.IO;
using System.Linq;
using MyGame;
using UnityEditor;
using UnityEngine;

// The Hub's lab (docs/brief.md): the Ferryman's island at the centre of the underworld, a quiet refuge after the Hold. A
// small island in still black water (hub.py): his hut at the north, where you come up out of the Hold through the
// trapdoor in its floor; a worn path down to the jetty and its landing, where his boat waits and the Mooring post
// stands; the merchant soul by the hut; and the dead, a long patient queue of them, waiting along the jetty and the
// shore to cross (soul.py, WaitingSoul). Warm lanterns (warm light is safety), mist on the water, the Far shore's
// light out in the dark to the south-east, and the ways to the Areas leading off into the mist, each marked by its
// lanterns (the Drowned City west, the Orchard east, the Hall north). Atmosphere only: no Lingerers, no Crossings yet.
public static partial class MainSceneBuilder
{
    const string HubLabPath = "Assets/_Project/Scenes/Lab_Hub.unity";

    // The Hub: a soft, kind dusk. A pale moon low through the mist, a blue-violet ambient, the fog warm-grey and not
    // too close; the colour kept (it's alive here), little grain, the edges open.
    static readonly AreaNight HubNight = new AreaNight
    {
        volume = "HubVolume", moon = Hex("#c3cde6"), moonIntensity = 0.65f, moonAngles = new Vector3(32f, -40f, 0f), shadowStrength = 0.7f,
        sky = Hex("#4d587e"), equator = Hex("#3a3f56"), ground = Hex("#16171d"),
        fog = Hex("#20253a"), fogStart = 20f, fogEnd = 58f,
        saturation = 6f, contrast = 10f, exposure = 0.3f, vignette = 0.3f, grain = 0.22f, filter = Hex("#fff2e2"),
    };

    static readonly Vector3 HubHut = new Vector3(0f, QuayTop, 5.2f);              // the hut's floor centre on the island (hub.py)
    const float HubHutFloor = 0.28f;                                               // (its floor above the island's top)
    static readonly Vector3 HubJettyHead = new Vector3(0f, QuayTop - 0.03f, -9.3f); // where the jetty leaves the shore
    const int HubJettyPieces = 3;

    [MenuItem("Assets/MyGame/Build Lab (The Hub)")]
    static void BuildHubLabFromMenu()
    {
        if (!UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;
        BuildHubLab();
    }

    public static void BuildHubLabFromCommandLine() => BuildHubLab();

    static void BuildHubLab()
    {
        var scene = BeginScene(new Vector3(0f, 0f, -8f), HubNight);
        // The water: still here (hardly a ripple), and darker, a mirror for the lanterns.
        var water = GameObject.Find("Water");
        var still = GeneratedMaterial("M_HubWater", "MyGame/Water");
        still.SetColor("_DeepColor", Hex("#080f16"));
        still.SetColor("_ShallowColor", Hex("#13232b"));
        still.SetColor("_FoamColor", Hex("#7f8c90"));
        still.SetFloat("_WaveHeight", 0.012f);
        still.SetFloat("_WaveLength", 5f);
        still.SetFloat("_WaveSpeed", 0.35f);
        still.SetFloat("_FoamDistance", 0.15f);
        EditorUtility.SetDirty(still);
        water.GetComponent<MeshRenderer>().sharedMaterial = still;

        var root = new GameObject("The Hub").transform;
        var island = Place("Environment/hub_island", Vector3.zero, 0f);
        island.transform.SetParent(root, true);

        // His hut: its roof fades while you're inside (Storeys); the trapdoor open in its floor, the way up from the Hold.
        var hut = HubHutWithin(root);

        // The path's lanterns, the jetty and its landing.
        Boardwalk(HubJettyHead, Vector3.back, HubJettyPieces);
        Vector3 landing = HubJettyHead + Vector3.back * (Segment * HubJettyPieces);
        Place("Environment/jetty_landing", landing, 0f).transform.SetParent(root, true);
        Vector3 landingMiddle = landing + new Vector3(0f, 0.03f, -1.6f);
        StreetLantern(new Vector3(1.25f, QuayTop, -8.7f), -90f, 2.6f);   // (the jetty's head)
        StreetLantern(new Vector3(-1.6f, QuayTop, -1.5f), 90f, 2.4f);    // (half-way down the path)
        StreetLantern(landingMiddle + new Vector3(-1.25f, 0f, -1.25f), 90f, 2.6f); // (the landing)
        MooringPostAt(landingMiddle + new Vector3(1.25f, 0f, -1.1f), 180f);
        var boat = BoatAt(landingMiddle + new Vector3(2.55f, -0.12f - QuayTop, 0f), 6f, landingMiddle + new Vector3(1.15f, 0f, 0.4f));
        var far = FarShore(boat, new Vector3(landingMiddle.x - 2.4f, 0f, landingMiddle.z + 25.6f)); // (where Main's is from its landing)
        far.leftBehind.SetActive(false); // (no one's crossed yet)
        MerchantAt(new Vector3(3.6f, QuayTop, 1.4f), 215f);               // (by the hut, east of the path, under its lantern)
        var bench = Place("Environment/hub_bench", new Vector3(-5.2f, QuayTop, 2.6f), 200f); // (for a Lingerer, later)
        bench.transform.SetParent(root, true);

        // The dead, waiting to cross: along the jetty's west side (room to pass on its east), then up the shore and
        // round to the west along the island's south edge. Each different, out of step with the rest.
        var queue = new GameObject("The Queue").transform;
        queue.SetParent(root, true);
        var souls = Directory.GetFiles(Models + "Characters", "soul*.fbx").Select(Path.GetFileNameWithoutExtension).OrderBy(n => n).ToArray();
        int k = 0;
        foreach (var (at, yaw) in HubQueue(landing))
        {
            if (souls.Length == 0)
                break;
            var soul = HubSoul(souls[(k * 7 + k / 3) % souls.Length], at, yaw);
            soul.transform.SetParent(queue, true);
            k++;
        }
        var murmurs = new GameObject("The Queue (murmurs)").AddComponent<AmbientOneShots>();
        murmurs.transform.SetParent(root, true);
        murmurs.transform.position = new Vector3(-1f, QuayTop + 1.2f, -12f);
        murmurs.clips = Clips("hub_murmur");
        murmurs.every = new Vector2(4f, 10f);
        murmurs.spread = 9f;
        murmurs.volume = 0.35f;

        // The ways off to the Areas, each marked out into the mist by its lanterns: the Drowned City west, the
        // Orchard east, the Hall of the Last Hour north, past the hut.
        foreach (var (model, colour, points) in new[]
        {
            ("city", Hex("#8fd6cc"), new[] { new Vector2(-17.5f, -3f), new Vector2(-23f, -5f), new Vector2(-29f, -6f), new Vector2(-35f, -6.5f) }),
            ("orchard", Hex("#ffb35c"), new[] { new Vector2(17.5f, -1.5f), new Vector2(23f, 0.5f), new Vector2(29f, 1.5f), new Vector2(35f, 2f) }),
            ("hall", Hex("#f2dfa6"), new[] { new Vector2(6f, 14.5f), new Vector2(9f, 20f), new Vector2(12f, 25.5f), new Vector2(15f, 31f) }),
        })
            foreach (var p in points)
            {
                var marker = Place("Environment/hub_marker_" + model, new Vector3(p.x, 0f, p.y), Mathf.Repeat(p.x * 31f + p.y * 17f, 360f));
                marker.transform.SetParent(root, true);
                var glow = PointLight(marker.transform, GlowCentre(marker, "Marker" + char.ToUpper(model[0]) + model.Substring(1) + "_Glow"), 5f, 1.6f, colour);
                Flicker(glow, 0.08f, 3f, 0.01f);
            }

        // Rocks and islets in the water round it.
        foreach (var (model, e, n, u, yaw) in new[]
        {
            ("rock_a", -15.2f, 4.5f, -0.12f, 40f), ("rock_b", 15.8f, 6.2f, -0.1f, 200f), ("rock_c", -9.5f, -14.6f, -0.15f, 120f),
            ("rock_b", 8.6f, -15.8f, -0.1f, 300f), ("islet_small", -19f, 10f, -0.6f, 80f), ("islet_small", 20f, -9f, -0.6f, 250f),
            ("islet_big", -6f, 22f, -0.8f, 160f), ("rock_a", 13.4f, 12.8f, -0.12f, 10f),
        })
            Place("Environment/" + model, new Vector3(e, u, n), yaw).transform.SetParent(root, true);

        // The air: mist lying on the water; a few warm motes round the lanterns; the moon's light soft.
        HoldMist(root, new Vector3(0f, 0.15f, -8f), new Vector3(70f, 0.3f, 60f));
        foreach (var p in new[] { new Vector3(1.25f, 2.2f, -8.7f), new Vector3(-1.6f, 2.2f, -1.5f), HubHut + new Vector3(0f, 2.2f, -3f) })
        {
            var motes = new GameObject("Motes").AddComponent<Motes>();
            motes.transform.SetParent(root, true);
            motes.transform.position = p;
            motes.perSecond = 0.6f;
            motes.radius = 1.6f;
            motes.colour = new Color(1f, 0.82f, 0.55f, 0.7f);
        }

        // Sounds: the water lapping at the piles and the shore; now and then, a bell far off across the water.
        var lap = new GameObject("The Water Lapping", typeof(AudioSource)).GetComponent<AudioSource>();
        lap.transform.SetParent(root, true);
        lap.transform.position = HubJettyHead + Vector3.back * 3f;
        lap.clip = Clips("hub_water") is var water1 && water1.Length > 0 ? water1[0] : null;
        lap.loop = true;
        lap.playOnAwake = true;
        lap.spatialBlend = 0.6f;
        lap.rolloffMode = AudioRolloffMode.Linear;
        lap.minDistance = 6f;
        lap.maxDistance = 30f;
        lap.volume = 0.5f;
        lap.dopplerLevel = 0f;
        var bell = new GameObject("The Far Bell").AddComponent<AmbientOneShots>();
        bell.transform.SetParent(root, true);
        bell.transform.position = far.light.transform.position;
        bell.clips = Clips("hub_far_bell");
        bell.every = new Vector2(30f, 60f);
        bell.spread = 2f;
        bell.volume = 0.4f;

        // The lab's posts by the landing; where you start: in his hut, beside the trapdoor you've just come up through.
        var tester = TesterPostAt(new Vector3(-2.6f, QuayTop, -6.5f), 160f);
        tester.spawner = LabTemplates();
        var start = new GameObject("Start Point").transform;
        start.position = HubHut + new Vector3(1.2f, HubHutFloor, -0.55f);
        var entrance = new GameObject("Area Entrance").AddComponent<AreaEntrance>();
        entrance.transform.position = HubHut + new Vector3(0f, 1f, -3.5f); // (as you come out of his door)
        entrance.areaName = "The Ferryman's Jetty";
        var apprentice = Apprentice(start.position);
        apprentice.transform.rotation = Quaternion.Euler(0f, 180f, 0f) * apprentice.transform.rotation; // (facing the door)
        apprentice.GetComponent<Equipment>().startingCharms = new[] { Charm.TestersKnot.Id };
        var readout = new GameObject("Lab Readout").AddComponent<LabReadout>();
        readout.font = AssetDatabase.LoadAssetAtPath<Font>(Fonts + "IMFellEnglish-Regular.ttf");
        tester.readout = readout;

        FinishScene(scene, HubLabPath, start, apprentice, "music_hub");
    }

    // His hut, its roof and what's in it; the trapdoor laid open (and covered by an unseen lid, so no one falls down
    // the shaft: the Hold is another scene for now).
    static Transform HubHutWithin(Transform parent)
    {
        var root = new GameObject("The Ferryman's Hut").transform;
        root.SetParent(parent, true);
        root.position = HubHut;
        var storeys = root.gameObject.AddComponent<Storeys>();
        storeys.footprint = new Vector2(2.8f, 2.3f);
        GameObject Part(string model)
        {
            var go = Place("Environment/" + model, HubHut, 0f);
            go.transform.SetParent(root, true);
            return go;
        }
        var ground = Part("hub_hut");
        var roof = Part("hub_hut_roof");
        storeys.storeys = new[]
        {
            new Storeys.Storey { floor = HubHut.y + HubHutFloor, parts = new[] { ground } },
            new Storeys.Storey { floor = HubHut.y + 99f, parts = new[] { roof } }, // (never stood on: gone whenever you're inside)
        };
        float floor = HubHut.y + HubHutFloor;
        Vector3 In(float e, float n) => new Vector3(HubHut.x + e, floor, HubHut.z + n);
        void Prop(string name, float e, float n, float yaw)
        {
            var go = Place("Environment/tower_props_" + name, In(e, n), yaw);
            go.transform.SetParent(ground.transform, true);
        }
        // The trapdoor's lid, thrown back open on its hinges (north), and the lid nobody sees over the shaft.
        var hatch = Place("Environment/hub_hatch", In(1.2f, 1.1f), 0f);
        hatch.transform.rotation = Quaternion.AngleAxis(100f, Vector3.right) * hatch.transform.rotation; // (up past upright, leaning back north)
        hatch.transform.SetParent(ground.transform, true);
        var heaved = hatch.AddComponent<AudioSource>(); // (as the scene opens: you've just heaved it open and climbed out)
        heaved.clip = Clips("hub_trapdoor") is var lids && lids.Length > 0 ? lids[0] : null;
        heaved.playOnAwake = true;
        heaved.spatialBlend = 0.8f;
        heaved.minDistance = 2f;
        heaved.maxDistance = 14f;
        heaved.volume = 0.7f;
        heaved.dopplerLevel = 0f;
        var cover = new GameObject("Over the Shaft (unseen)").AddComponent<BoxCollider>();
        cover.transform.SetParent(ground.transform, true);
        cover.transform.position = In(1.2f, 0.6f) + Vector3.down * 0.05f;
        cover.size = new Vector3(1.05f, 0.1f, 1.05f);
        // His things: his spare oars on their rack, a cot, a table and a stool, a shelf, a lantern by the door (inside
        // and out), a coil of rope; the hearth's glow.
        Prop("oar_rack", -1.0f, 2.08f, 0f);
        Prop("shelf", 0.4f, 2.08f, 0f);
        Prop("cot", 2.1f, 1.3f, 90f);
        Prop("table", -1.6f, -1.2f, 0f);
        Prop("stool", -1.0f, -0.6f, 30f);
        Prop("stool", -2.0f, 0.5f, 250f);
        var lamp = Place("Environment/tower_props_lantern", In(-1.6f, -2.08f), 180f);
        lamp.transform.SetParent(ground.transform, true);
        Flicker(PointLight(lamp.transform, GlowCentre(lamp, "HangingLantern_Glow"), 4.5f, 2f, LanternLight), 0.1f, 5f, 0.01f);
        var porch = Place("Environment/tower_props_lantern", new Vector3(HubHut.x - 1.0f, floor, HubHut.z - 2.32f), 0f); // (outside, by the door)
        porch.transform.SetParent(root, true);
        Flicker(PointLight(porch.transform, GlowCentre(porch, "HangingLantern_Glow") + Vector3.back * 0.3f, 6f, 2.6f, LanternLight), 0.1f, 5f, 0.01f);
        Place("Environment/tower_props_rope", new Vector3(HubHut.x + 1.5f, QuayTop, HubHut.z - 2.7f), 160f).transform.SetParent(root, true);
        var hearth = PointLight(ground.transform, In(-2.2f, 0f) + Vector3.up * 0.4f, 4f, 2.2f, Hex("#ff8a45"));
        Flicker(hearth, 0.25f, 4f, 0.03f);
        // The windows' light, out onto the grass east and west.
        foreach (float e in new[] { -3.2f, 3.2f })
            PointLight(root, new Vector3(HubHut.x + e, floor + 1.3f, HubHut.z), 4.5f, 1.2f, LanternLight);
        return root;
    }

    // A soul waiting to cross (soul.py's variants): idling, out of step, watching you go by.
    static GameObject HubSoul(string model, Vector3 at, float yaw)
    {
        var go = Place("Characters/" + model, at, yaw);
        go.name = "A Soul Waiting (" + model + ")";
        foreach (var c in go.GetComponentsInChildren<Collider>())
            Object.DestroyImmediate(c);
        // (solid: you walk round them, not through)
        var body = go.AddComponent<CapsuleCollider>();
        body.center = new Vector3(0f, 0.8f, 0f);
        body.height = 1.6f;
        body.radius = 0.25f;
        var soul = go.AddComponent<WaitingSoul>();
        soul.head = go.GetComponentsInChildren<Transform>().FirstOrDefault(t => t.name == "Head");
        go.AddComponent<NavMeshIgnore>();
        return go;
    }

    // Where the queue stands: from the landing's edge back up the jetty's west side, then up the shore path a little,
    // and round to the west along the island's south edge; each facing the one ahead (towards the boat).
    static System.Collections.Generic.IEnumerable<(Vector3 at, float yaw)> HubQueue(Vector3 landing)
    {
        const float apart = 0.95f;
        // On the jetty: facing south, the boat ahead.
        for (float z = landing.z + 0.4f; z < HubJettyHead.z - 0.3f; z += apart)
            yield return (new Vector3(-0.45f, QuayTop, z), 180f);
        // Off its head, up onto the shore, then west along the south edge, facing back along the line.
        var bend = new[]
        {
            new Vector2(-0.6f, -8.4f), new Vector2(-1.3f, -7.7f), new Vector2(-2.2f, -7.3f), new Vector2(-3.2f, -7.1f),
            new Vector2(-4.2f, -7.0f), new Vector2(-5.2f, -6.9f), new Vector2(-6.2f, -6.6f), new Vector2(-7.1f, -6.2f),
            new Vector2(-8.0f, -5.7f), new Vector2(-8.8f, -5.1f),
        };
        for (int i = 0; i < bend.Length; i++)
        {
            Vector2 ahead = i == 0 ? new Vector2(-0.45f, HubJettyHead.z) : bend[i - 1];
            Vector2 d = ahead - bend[i];
            yield return (new Vector3(bend[i].x, QuayTop, bend[i].y), Mathf.Atan2(d.x, d.y) * Mathf.Rad2Deg);
        }
    }
}
