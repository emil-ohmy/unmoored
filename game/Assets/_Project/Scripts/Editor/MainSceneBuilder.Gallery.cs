using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using MyGame;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// The animation gallery (Lab_Gallery): every rigged creature (art/models/characters) on its own plinth in a row, by
// Area, lit, nothing to fight: AnimationGallery plays their clips, and their attacks as a fight times them (taken from
// each creature as the scenes build it: every SouredAttack it carries). A creature added to art/models/characters
// shows up here on the next build, its clips with it; its attacks too, once it's in the list below.
public static partial class MainSceneBuilder
{
    const string GalleryPath = "Assets/_Project/Scenes/Lab_Gallery.unity";

    [MenuItem("Assets/MyGame/Build Lab (Animation Gallery)")]
    static void BuildGalleryFromMenu()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;
        BuildGallery();
    }

    public static void BuildGalleryFromCommandLine() => BuildGallery();

    // The order on the plinths (by Area, then the Apprentice's own), and how each is built in the world (for its
    // attacks); any rigged model not listed comes after, its clips only.
    static readonly (string model, string title, System.Func<Vector3, Component> build)[] GalleryOrder =
    {
        ("apprentice", "The Apprentice", null),
        ("laden", "Laden", p => Laden("Laden", p, 0f)),
        ("laden_archer", "Laden archer", p => LadenArcher("Laden archer", p, 0f)),
        ("clinger", "Clinger", p => Clinger("Clinger", p, 0f)),
        ("drowned", "Drowned", p => Drowned("Drowned", p, 0f)),
        ("unparted", "The Unparted (Hold)", p => TheUnparted("The Unparted", p, 0f)),
        ("anchoress", "The Antediluvian Anchoress", p => Anchoress("Anchoress", p, 0f)),
        ("moth", "Moth", p => Moth("Moth", p, 0f)),
        ("gleaner", "Gleaner", p => Gleaner("Gleaner", p, 0f)),
        ("thresher", "Thresher", p => Thresher("Thresher", p, 0f)),
        ("watchman", "The Watchman", null),
        ("father_moth", "Father Moth", p => FatherMoth(new GameObject("(Father Moth's)").transform, p, null, null)),
        ("punctual", "Punctual", p => Punctual("Punctual", p, 0f)),
        ("wound", "Wound", p => Wound("Wound", p, 0f)),
        ("keener", "Keener", p => Keener("Keener", p, 0f)),
        ("widow", "The Widow Hale", p => Widow("The Widow Hale", p, 0f)),
        ("the_hour", "The One Who Waits", p => TheHourBoss(p)),
    };

    // A studio's light, not a night's: even and bright enough to see every limb, the shadows soft.
    static readonly AreaNight GalleryLight = new AreaNight
    {
        volume = "GalleryVolume", moon = Hex("#fff6ea"), moonIntensity = 1.1f, moonAngles = new Vector3(50f, -30f, 0f), shadowStrength = 0.5f,
        sky = Hex("#9aa3b4"), equator = Hex("#6f7480"), ground = Hex("#3a3836"),
        fog = Hex("#2a2e36"), fogStart = 60f, fogEnd = 200f,
        saturation = 0f, contrast = 4f, exposure = 0.2f, vignette = 0.15f, grain = 0f, filter = Hex("#ffffff"),
    };

    // Its clips one after another as a fight chains them (Soured: a loop crossfades in over 0.15 s, a one-off over
    // 0.05 s; a one-off is stretched over its time and held on its last frame after). The Laden's, step by step from
    // its own code; the rest: standing, closing in, each attack and back after you, then losing you.
    static GalleryExhibit.Step[] GalleryChain(string model, GalleryExhibit e)
    {
        var steps = new List<GalleryExhibit.Step>();
        bool Has(string clip) => e.State(clip) != null;
        void Loop(string clip, float time, string note)
        {
            if (Has(clip))
                steps.Add(new GalleryExhibit.Step { clip = clip, duration = time, loop = true, fade = steps.Count == 0 ? 0f : 0.15f, note = note });
        }
        void Once(string clip, float time, string note, float fade = 0.05f)
        {
            if (Has(clip))
                steps.Add(new GalleryExhibit.Step { clip = clip, duration = time, loop = false, fade = steps.Count == 0 ? 0f : fade, note = note });
        }
        void Attack(GalleryExhibit.Attack a)
        {
            Once(a.windup, a.telegraph, $"{a.name}: the wind-up", Soured.WindupFade);
            Once(a.strike, a.strikeTime, $"{a.name}: the blow");
            if (!a.ranged && steps.Count > 0 && steps[steps.Count - 1].clip == a.strike)
                steps[steps.Count - 1].swipe = a.attack;
            Once(a.recover, a.recoverTime, $"{a.name}: recovering");
        }
        string idle = Has("Idle") ? "Idle" : e.Clips.FirstOrDefault();
        if (model == "laden")
        {
            var slam = e.attacks.FirstOrDefault(a => a.windup == "Windup");
            var sweep = e.attacks.FirstOrDefault(a => a.windup == "SweepWindup");
            Loop("Idle", 2.5f, "standing, unaware: the heap on its hip");
            Loop("Walk", 1.8f, "it's seen you: lumbering in, heap on the hip");
            Once("GuardUp", 0.35f, "near: the heap comes up before its face");
            Loop("GuardWalk", 1.6f, "shuffling in behind it");
            if (slam != null)
                Attack(slam);
            Loop("Walk", 1f / 30f, "after you again (a moment: still near)");
            Once("GuardUp", 0.35f, "...so the guard comes up");
            Loop("GuardWalk", 1.2f, "shuffling in behind it");
            Once("Block", 0.3f, "a blow turned on its guard");
            Loop("GuardWalk", 0.8f, "shuffling in");
            if (sweep != null)
                Attack(sweep);
            Once("Stagger", 1.2f, "struck hard enough: staggered");
            Loop("Walk", 1.4f, "after you");
            Loop("Idle", 2f, "lost you");
            return steps.ToArray();
        }
        string walk = Has("Walk") ? "Walk" : null;
        if (GalleryLoops.TryGetValue(model, out var loops))
        {
            idle = Has(loops.idle) ? loops.idle : idle;
            walk = Has(loops.chase) ? loops.chase : walk;
        }
        Loop(idle, 2.5f, "standing, unaware");
        if (walk != null && walk != idle)
            Loop(walk, 2f, "it's seen you: closing in");
        string after = walk ?? idle;
        foreach (var a in e.attacks)
        {
            // (Father Moth's blades are afoot: down on the ground before them, and staying there after)
            bool afoot = model == "father_moth" && a.recover == "Land";
            if (afoot && after != "Land")
                Loop("Land", 1.4f, "down among the rows: afoot");
            Attack(a);
            after = afoot ? "Land" : walk ?? idle;
            Loop(after, 1.4f, walk != null ? "after you again" : "waiting");
        }
        if (Has("Stagger"))
        {
            Once("Stagger", 1f, "staggered");
            Loop(after, 1.4f, "after you again"); // (a stagger hands back to the chase it broke into)
        }
        Loop(idle, 2f, "lost you");
        return steps.ToArray();
    }

    // Each one's own loops where they aren't Idle and Walk (its IdleClip and ChaseClip, awake and in the open):
    // standing, and coming after you.
    static readonly Dictionary<string, (string idle, string chase)> GalleryLoops = new Dictionary<string, (string, string)>
    {
        { "moth", ("Fly", "Fly") },
        { "father_moth", ("Fly", "Fly") },
        { "drowned", ("Idle", "Swim") },
        { "wound", ("Idle", "Run") },
        { "clinger", ("Idle", "Skitter") },
        { "thresher", ("Idle", "Idle") },
        { "the_hour", ("Idle", "Idle") },
        { "watchman", ("Idle", "Lurch") },
    };

    // A swipe trail the creature carries in the world, on the exhibit: the same two points on the same bone.
    sealed class GallerySwipeOf
    {
        public string bone;
        public Vector3 tip, inner;      // (in the bone's space)
        public Material material;
        public Color colour;
        public float lifetime;
        public string[] attacks;
    }

    static void GallerySwipe(GameObject go, GallerySwipeOf made)
    {
        var bone = go.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == made.bone);
        if (bone == null)
        {
            Debug.LogWarning($"[Gallery] {go.name}: no {made.bone} bone for its swipe");
            return;
        }
        Transform Point(string name, Vector3 at)
        {
            var point = new GameObject(name).transform;
            point.SetParent(bone, false);
            point.localPosition = at;
            return point;
        }
        var trail = go.AddComponent<SwingTrail>();
        trail.tip = Point("Swing Tip", made.tip);
        trail.inner = Point("Swing Inner", made.inner);
        trail.material = made.material;
        trail.colour = made.colour;
        trail.lifetime = made.lifetime;
        trail.attacks = made.attacks;
    }

    static void BuildGallery()
    {
        var scene = BeginScene(new Vector3(0f, -40f, 0f), GalleryLight); // (the water far below: none here)
        var root = new GameObject("The Gallery").transform;
        const float floor = 0.5f;

        var stone = GeneratedMaterial("M_GalleryStone", "MyGame/Toon");
        stone.SetColor("_BaseColor", Hex("#5d5a58"));
        EditorUtility.SetDirty(stone);
        var plinthStone = GeneratedMaterial("M_GalleryPlinth", "MyGame/Toon");
        plinthStone.SetColor("_BaseColor", Hex("#8e887d"));
        EditorUtility.SetDirty(plinthStone);

        var models = Directory.GetFiles(Models + "Characters", "*.fbx").Select(Path.GetFileNameWithoutExtension).ToList();
        var order = GalleryOrder.Where(o => models.Contains(o.model)).ToList();
        foreach (var m in models.Where(m => GalleryOrder.All(o => o.model != m)))
            order.Add((m, ObjectNames.NicifyVariableName(m), null));

        float x = 0f;
        float lastRadius = 0f;
        var exhibits = new List<GalleryExhibit>();
        foreach (var (model, title, build) in order)
        {
            // Its attacks, from the creature as the world builds it (then that copy's gone).
            var attacks = new List<GalleryExhibit.Attack>();
            var swipes = new List<GallerySwipeOf>(); // (its swipe trails, copied onto it below)
            if (build != null)
            {
                Component made = null;
                try
                {
                    made = build(new Vector3(0f, -200f, 0f));
                    foreach (var soured in made.GetComponentsInChildren<Soured>(true).Concat(made.GetComponents<Soured>()).Distinct())
                        foreach (var field in soured.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance))
                            if (field.FieldType == typeof(SouredAttack) && field.GetValue(soured) is SouredAttack a && !string.IsNullOrEmpty(a.windupClip)
                                && attacks.All(had => had.name != a.name || had.windup != a.windupClip))
                                attacks.Add(new GalleryExhibit.Attack
                                {
                                    name = $"{a.name}{(soured.name != made.name && soured.GetComponentInChildren<Animation>() != null ? $" ({soured.name})" : "")}",
                                    windup = a.windupClip, strike = a.strikeClip, recover = a.recoverClip,
                                    telegraph = a.telegraph, strikeTime = a.strike, recoverTime = a.recover,
                                    attack = a.name, ranged = a.ranged,
                                });
                    foreach (var trail in made.GetComponentsInChildren<SwingTrail>(true))
                        if (trail.tip != null && trail.inner != null)
                            swipes.Add(new GallerySwipeOf
                            {
                                bone = trail.tip.parent.name, tip = trail.tip.localPosition, inner = trail.inner.localPosition,
                                material = trail.material, colour = trail.colour, lifetime = trail.lifetime, attacks = trail.attacks,
                            });
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"[Gallery] {model}: its attacks couldn't be read ({ex.Message})");
                }
                if (made != null)
                {
                    foreach (var soured in made.GetComponentsInChildren<Soured>(true))
                        if (soured.droppedLantern != null)
                            Object.DestroyImmediate(soured.droppedLantern.gameObject);
                    var top = made.transform;
                    while (top.parent != null)
                        top = top.parent;
                    Object.DestroyImmediate(top.gameObject);
                }
                foreach (var stray in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                    if (stray != null && stray.parent == null && stray.position.y < -150f)
                        Object.DestroyImmediate(stray.gameObject); // (anything else that copy left down there)
            }

            // On its plinth, facing the camera (south).
            var go = Place("Characters/" + model, Vector3.zero, 180f);
            go.name = title;
            foreach (var c in go.GetComponentsInChildren<Collider>())
                Object.DestroyImmediate(c);
            var bounds = new Bounds(go.transform.position, Vector3.zero);
            foreach (var r in go.GetComponentsInChildren<Renderer>())
                bounds.Encapsulate(r.bounds);
            float radius = Mathf.Clamp(Mathf.Max(bounds.extents.x, bounds.extents.z), 0.6f, 6f);
            float height = Mathf.Max(1f, bounds.max.y - go.transform.position.y);
            x += lastRadius + radius + 1.6f;
            lastRadius = radius;
            float lift = model == "moth" ? 1.6f : 0f; // (it flies)
            go.transform.position = new Vector3(x, floor + 0.3f + lift, 0f);
            go.transform.SetParent(root, true);
            var exhibit = go.AddComponent<GalleryExhibit>();
            exhibit.title = title;
            exhibit.height = height + lift;
            exhibit.radius = radius;
            // (only what it really does: an attack whose wind-up it has a clip for; a Soured's unused default has none)
            exhibit.attacks = attacks.Where(a => exhibit.State(a.windup) != null && exhibit.State(a.strike) != null).ToArray();
            exhibit.chain = GalleryChain(model, exhibit);
            foreach (var made in swipes)
                GallerySwipe(go, made);
            exhibits.Add(exhibit);

            var plinth = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            plinth.name = "Plinth";
            Object.DestroyImmediate(plinth.GetComponent<Collider>());
            plinth.transform.SetParent(root, true);
            plinth.transform.position = new Vector3(x, floor + 0.15f, 0f);
            plinth.transform.localScale = new Vector3(Mathf.Min(radius, 2.4f) * 2f + 0.6f, 0.15f, Mathf.Min(radius, 2.4f) * 2f + 0.6f);
            plinth.GetComponent<MeshRenderer>().sharedMaterial = plinthStone;
            var key = PointLight(root, new Vector3(x - 1.5f, floor + Mathf.Min(height, 6f) + 2f, -3f), Mathf.Max(10f, height * 2f), 2.2f, Hex("#fff1d8"));
            key.shadows = LightShadows.None;
        }

        // The floor under them all.
        var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ground.name = "Floor";
        Object.DestroyImmediate(ground.GetComponent<Collider>());
        ground.transform.SetParent(root, true);
        ground.transform.position = new Vector3((x + lastRadius + 2f) / 2f, floor - 0.25f, 0f);
        ground.transform.localScale = new Vector3(x + lastRadius + 6f, 0.5f, 14f);
        ground.GetComponent<MeshRenderer>().sharedMaterial = stone;

        var gallery = new GameObject("Animation Gallery").AddComponent<AnimationGallery>();
        gallery.font = AssetDatabase.LoadAssetAtPath<Font>(Fonts + "IMFellEnglish-Regular.ttf");
        gallery.titleFont = AssetDatabase.LoadAssetAtPath<Font>(Fonts + "IMFellEnglishSC-Regular.ttf");
        var overlay = GeneratedMaterial("M_GalleryBones", "MyGame/Overlay");
        EditorUtility.SetDirty(overlay);
        gallery.boneMaterial = overlay;

        var cam = new GameObject("Main Camera", typeof(Camera)).GetComponent<Camera>();
        cam.tag = "MainCamera";
        cam.fieldOfView = 30f;
        cam.farClipPlane = 300f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Hex("#20242c");
        cam.GetUniversalAdditionalCameraData().renderPostProcessing = true;
        cam.gameObject.AddComponent<AudioListener>();
        var volume = new GameObject("Global Volume").AddComponent<Volume>();
        volume.isGlobal = true;
        volume.sharedProfile = VolumeProfileAsset();

        EditorSceneManager.SaveScene(scene, GalleryPath);
        AssetDatabase.SaveAssets();
        var listed = EditorBuildSettings.scenes.Where(s => File.Exists(s.path)).ToList();
        if (!listed.Exists(s => s.path == GalleryPath))
            listed.Add(new EditorBuildSettingsScene(GalleryPath, true));
        EditorBuildSettings.scenes = listed.ToArray();
        Debug.Log($"[MainSceneBuilder] Built {GalleryPath} ({exhibits.Count} creatures)");
    }
}
