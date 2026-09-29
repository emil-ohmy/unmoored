using MyGame;
using UnityEditor;
using UnityEngine;

// Builds Assets/_Project/Scenes/BossArena.unity: the Anchoress's drowned chapel on its own, to play-test the Boss
// without the walk there (she moves into the village later). You start at a Mooring post on a landing south of the
// chapel, a short boardwalk from the fight; dying wakes you there and resets her.
//   Assets menu > MyGame > Build Boss Arena, or headless: -executeMethod MainSceneBuilder.BuildBossArenaFromCommandLine
public static partial class MainSceneBuilder
{
    const string BossScenePath = "Assets/_Project/Scenes/BossArena.unity";

    [MenuItem("Assets/MyGame/Build Boss Arena")]
    static void BuildBossArenaFromMenu()
    {
        if (!UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;
        BuildBossArena();
    }

    public static void BuildBossArenaFromCommandLine() => BuildBossArena();

    static void BuildBossArena()
    {
        var scene = BeginScene();

        // The chapel's upper floor (the altar to the north), and the way in from the south.
        var chapel = Place("Environment/drowned_chapel", Vector3.zero, 0f);
        var floor = chapel.AddComponent<ChapelFloor>(); // its sections break away in her second phase
        floor.crackSound = Clips("floor_crack");
        floor.breakSound = Clips("floor_break");
        const float edge = -7.8f; // the chapel's south edge
        Boardwalk(new Vector3(0f, QuayTop - 0.03f, edge), Vector3.back, 2);
        float landing = edge - Segment * 2;
        Place("Environment/jetty_landing", new Vector3(0f, QuayTop - 0.03f, landing), 0f);
        MooringPostAt(new Vector3(1.25f, QuayTop, landing - 2.66f), 180f);
        StreetLantern(new Vector3(-0.75f, QuayTop, edge - 5.2f), 90f, 2.2f);
        Place("Environment/boat", new Vector3(2.55f, -0.12f, landing - 1.3f), 186f);
        var start = new GameObject("Start Point").transform;
        start.position = new Vector3(0f, QuayTop, landing - 1.3f);
        var entrance = new GameObject("Area Entrance").AddComponent<AreaEntrance>();
        entrance.transform.position = new Vector3(0f, QuayTop + 1f, edge - 3f);
        entrance.areaName = "The Drowned Chapel";

        // Light: her green lanterns hung on the tall pillars at the altar end (green = danger), the candles on the
        // altar, and the moon. The near end stays dim.
        foreach (float angle in new[] { 202.5f, 247.5f, 292.5f, 337.5f })
        {
            // The pillars stand at the octagon's corners (angles measured in the model, whose front is the world's +Z).
            float a = angle * Mathf.Deg2Rad;
            var pillar = new Vector3(8.05f * Mathf.Cos(a), 0f, -8.05f * Mathf.Sin(a));
            var inwards = -new Vector3(pillar.x, 0f, pillar.z).normalized;
            var hung = Place("Environment/soured_lantern", pillar + inwards * 0.5f + Vector3.up * (QuayTop + 1.7f), angle);
            hung.name = "Hung Lantern";
            Flicker(PointLight(hung.transform, hung.transform.position + Vector3.down * 0.1f, 7.5f, 2.4f, SouredLight), 0.15f, 5f, 0.02f);
        }
        // Moonlight through the fallen roof, on the floor where she kneels: the stage, readable in the dark.
        var shaft = new GameObject("Moonlight Through The Roof").AddComponent<Light>();
        shaft.type = LightType.Spot;
        shaft.transform.SetPositionAndRotation(new Vector3(0f, 7.5f, -0.8f), Quaternion.Euler(86f, 0f, 0f)); // (low: point and spot lights fall off with distance squared)
        shaft.spotAngle = 88f;
        shaft.innerSpotAngle = 50f;
        shaft.range = 14f;
        shaft.intensity = 40f;
        shaft.color = Hex("#8ea8d8");
        shaft.shadows = LightShadows.None;
        var candles = new GameObject("Altar Candles");
        Flicker(PointLight(candles.transform, new Vector3(0f, QuayTop + 1.4f, 6.4f), 4.5f, 1.4f, LanternLight), 0.2f, 6f, 0.02f);

        var apprentice = Apprentice(start.position);
        // For play-testing: the Tester's Knot (four times the damage) is owned here; fit it at the Mooring post.
        apprentice.GetComponent<Equipment>().startingCharms = new[] { Charm.TestersKnot.Id };
        var boss = Anchoress("The Antediluvian Anchoress", new Vector3(0f, QuayTop, 1.2f), 0f); // kneeling on his mark, facing the altar, her back to you
        boss.floor = floor;

        FinishScene(scene, BossScenePath, start, apprentice, "music_drowned_dark");
    }

    static Anchoress Anchoress(string name, Vector3 position, float yaw)
    {
        var go = Place("Characters/anchoress", position, yaw);
        go.name = name;
        var controller = go.AddComponent<CharacterController>();
        controller.center = new Vector3(0f, 1.1f, 0f);
        controller.height = 2.2f;
        controller.radius = 0.75f;
        controller.minMoveDistance = 0f;
        var boss = go.AddComponent<Anchoress>();
        boss.maxHealth = 900f;
        boss.radius = 0.8f;
        boss.knockbackResistance = 0.95f;
        boss.staggerCapacity = 220f;
        boss.staggerDrainDelay = 2f;
        boss.staggerDrainPerSecond = 20f;
        boss.staggerDuration = 2.0f; // the anchor slips from her hands: a long opening
        boss.stunDuration = 2.0f;
        boss.aggroRadius = 6f; // wakes once you're well onto the floor, with her on screen
        boss.viewHalfAngle = 180f; // at her vigil she senses anyone who comes close, from any side
        boss.attackRange = 2.6f;
        boss.chaseSpeed = 1.6f;
        boss.turnSpeed = 110f; // slow to turn: her back is where she's open
        boss.guardRange = 3.6f;
        boss.guardSpeed = 0.9f;
        boss.guardRaiseTime = 0.35f;
        boss.flankAngle = 60f;
        boss.sweepChanceInFront = 0.3f;
        boss.obols = 400;
        boss.eyeHeight = 1.7f;
        boss.barHeight = 2.8f;
        boss.deathDuration = 1.0f;
        boss.fareMotes = 90;
        boss.primary = new SouredAttack
        {
            name = "Drop", windupClip = "Windup", strikeClip = "Strike", recoverClip = "Recover",
            telegraph = 1.1f, strike = 0.35f, recover = 1.5f, impactAt = 0.75f,
            lunge = 0.3f, maxLunge = 1.2f, trackUntil = 0.8f, damage = 34f, reach = 2.7f, arc = 35f, knockback = 9f,
        };
        boss.sweep = new SouredAttack
        {
            name = "Sweep", windupClip = "SweepWindup", strikeClip = "Sweep", recoverClip = "SweepRecover",
            telegraph = 0.85f, strike = 0.45f, recover = 0.9f, impactAt = 0.5f,
            lunge = 0f, maxLunge = 0f, trackUntil = 0.85f, damage = 26f, reach = 4.4f, arc = 120f, knockback = 9f, scripted = true,
        };
        // Her own sounds: each attack's tell its own, so you can hear which is coming (see art/audio/sounds.md).
        boss.primary.windupSound = Clips("anchoress_drop_windup");  // the chain rattling up as the anchor climbs
        boss.primary.swingSound = Clips("laden_swing");
        boss.primary.swingPitch = 0.75f;
        boss.primary.impactSound = Clips("anchoress_drop_impact");
        boss.sweep.windupSound = Clips("anchoress_sweep_windup");   // the chain paying out
        boss.sweep.swingSound = Clips("anchoress_sweep");
        boss.cast.windupSound = Clips("anchoress_cast_windup");     // the heft, a breath held
        boss.cast.swingSound = Clips("anchoress_cast");
        boss.landSound = Clips("anchor_land");
        boss.haulSound = Clips("anchor_haul");
        boss.riseSound = Clips("anchoress_rise");
        boss.music = new[]
        {
            AssetDatabase.LoadAssetAtPath<AudioClip>(Audio + "Music/music_anchoress_1.wav"),
            AssetDatabase.LoadAssetAtPath<AudioClip>(Audio + "Music/music_anchoress_2.wav"),
        };
        boss.yank.windupSound = Clips("hook_whirl");
        boss.yank.swingSound = Clips("hook_throw");
        boss.hookCatchSound = Clips("hook_catch");
        boss.undertow.windupSound = Clips("undertow"); // the kneel and the prayer, the water welling up

        // The Undertow's water: the river's own shader, soured: near-black green, churning fast.
        var pool = GeneratedMaterial("M_UndertowPool", "MyGame/Water");
        pool.SetColor("_DeepColor", Hex("#08140c"));
        pool.SetColor("_ShallowColor", Hex("#17301a"));
        pool.SetColor("_FoamColor", Hex("#9cc468"));
        pool.SetFloat("_FoamDistance", 0.03f);
        pool.SetFloat("_DepthFade", 0.3f);
        pool.SetFloat("_WaveHeight", 0.02f);
        pool.SetFloat("_WaveLength", 1.2f);
        pool.SetFloat("_WaveSpeed", 2.2f);
        pool.SetFloat("_RippleScale", 4f);
        pool.SetFloat("_RippleSpeed", 1.4f);
        pool.SetFloat("_RippleStrength", 0.3f);
        EditorUtility.SetDirty(pool);
        boss.poolMaterial = pool;

        // The anchor: its bone, and points on it measured from the model (the ring the chain ties to, the crown that
        // hits the floor), and the hook at her waist where the chain starts.
        var skinned = go.GetComponentInChildren<SkinnedMeshRenderer>();
        skinned.updateWhenOffscreen = true; // the anchor flies far from her on its chain
        // Her attacks land where the anchor really is, so she animates even when she isn't on screen (a cast from off
        // the edge of it, and headless tests).
        go.GetComponentInChildren<Animation>().cullingType = AnimationCullingType.AlwaysAnimate;
        boss.anchorBone = Bone(go, "Anchor");
        boss.anchorRing = Marker(go, "Anchor Ring", boss.anchorBone, GlowCentre(go, "AnchorRing"));
        boss.anchorCrown = Marker(go, "Anchor Crown", boss.anchorBone, FarthestPoint(go, "AnchorArms", boss.anchorRing.position));
        boss.chainStart = Marker(go, "Chain Start", Bone(go, "Spine"), GlowCentre(go, "HookIron"));
        // (from the bind pose: in the editor the model stands in its first clip's pose, not upright with the anchor)
        var flatForward = Vector3.ProjectOnPlane(go.transform.forward, Vector3.up).normalized;
        boss.anchorHeld = Quaternion.Inverse(Quaternion.LookRotation(flatForward, Vector3.up)) * RestRotation(skinned, boss.anchorBone);
        boss.chainMaterial = System.Array.Find(skinned.sharedMaterials, m => m != null && m.name.StartsWith("AnchorIron"));

        // Phase two's Hook Yank: the Ferryman's hook at her waist, and where its line leaves her hands.
        boss.hookBone = Bone(go, "Hook");
        boss.hookHand = Marker(go, "Hook Hand", Bone(go, "Arms"), GlowCentre(go, "Claw"));
        boss.hookModel = AssetDatabase.LoadAssetAtPath<GameObject>(Models + "Environment/hook_head.fbx");

        BeltLantern(go, boss, "Anchor"); // hung on the anchor: you can follow it in the dark
        Swoosh(go, "Anchor", "AnchorArms", SouredSwoosh, 0.2f, "AnchorRing"); // the whole anchor, ring to crown
        return boss;
    }

    static Transform Marker(GameObject go, string name, Transform parent, Vector3 position)
    {
        var marker = new GameObject(name).transform;
        marker.SetParent(parent, false);
        marker.position = position;
        return marker;
    }

    // The vertex of the parts with this material farthest from a point (the anchor's crown, from its ring): the same
    // whatever pose the model is in.
    static Vector3 FarthestPoint(GameObject go, string material, Vector3 from)
    {
        var skinned = go.GetComponentInChildren<SkinnedMeshRenderer>();
        var baked = new Mesh();
        skinned.BakeMesh(baked, true);
        int index = System.Array.FindIndex(skinned.sharedMaterials, m => m != null && m.name.StartsWith(material));
        var vertices = baked.vertices;
        Vector3 farthest = from;
        foreach (int i in baked.GetTriangles(index))
        {
            Vector3 p = skinned.transform.position + skinned.transform.rotation * vertices[i]; // (baked with its scale)
            if (Vector3.Distance(p, from) > Vector3.Distance(farthest, from))
                farthest = p;
        }
        return farthest;
    }

    // A bone's world rotation in the bind pose (the model as built: the anchor upright in her hands).
    static Quaternion RestRotation(SkinnedMeshRenderer skinned, Transform bone)
    {
        int i = System.Array.IndexOf(skinned.bones, bone);
        return skinned.transform.rotation * skinned.sharedMesh.bindposes[i].inverse.rotation;
    }
}
