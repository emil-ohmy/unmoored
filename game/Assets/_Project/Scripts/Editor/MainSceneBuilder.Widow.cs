using MyGame;
using UnityEditor;
using UnityEngine;

// The Widow Hale, Who Kept the Minute: the Hall of the Last Hour's miniboss (docs/brief.md; Widow.cs). Placed by the
// Hall's lab at its central engine, which she keeps (MainSceneBuilder.LastHour).
public static partial class MainSceneBuilder
{
    static Widow Widow(string name, Vector3 position, float yaw)
    {
        var go = Place("Characters/widow", position, yaw);
        go.name = name;
        var controller = go.AddComponent<CharacterController>();
        controller.center = new Vector3(0f, 1.2f, 0f);
        controller.height = 2.4f;
        controller.radius = 0.5f;
        controller.minMoveDistance = 0f;
        var w = go.AddComponent<Widow>();
        w.maxHealth = 420f;
        w.radius = 0.55f;
        w.staggerCapacity = 50f;     // (three blows, or one heavy: she reels, her hold on a part broken)
        w.staggerDuration = 0.9f;
        w.knockbackResistance = 0.7f;
        w.aggroRadius = 13f;
        w.viewHalfAngle = 180f;      // (she keeps watch for you)
        w.noticeCloseRadius = 4f;
        w.chaseSpeed = 1.3f;
        w.turnSpeed = 200f;
        w.leashRadius = 13f;
        w.giveUpRange = 18f;
        w.obols = 260;
        w.eyeHeight = 2.3f;
        w.barHeight = 3.0f;
        w.deathDuration = 2.5f;
        w.dissolveDuration = 2.2f;
        w.fareMotes = 120;
        w.primary = new SouredAttack
        {
            name = "Knell", windupClip = "Breath", strikeClip = "Toll", recoverClip = "TollRecover",
            telegraph = 1.2f, strike = 1.2f, recover = 1.0f, impactAt = 0.15f,
            lunge = 0f, maxLunge = 0f, trackUntil = 0.8f, damage = 0f, reach = 12f, arc = 180f, knockback = 0f,
            ranged = true, volley = 4, volleyInterval = 0.2f, volleyTrack = 0f, // (one band: four rings a metre apart)
            windupSound = Clips("widow_breath"),
        };
        w.veil = new SouredAttack
        {
            name = "Veil", windupClip = "VeilDraw", strikeClip = "Veil", recoverClip = "VeilRecover",
            telegraph = 0.9f, strike = 0.6f, recover = 1.0f, impactAt = 0.45f,
            lunge = 0.3f, maxLunge = 1.2f, trackUntil = 0.75f, damage = 12f, reach = 4.5f, arc = 75f, knockback = 2f,
            low = true, scripted = true, // (along the floor: vaulted, it passes under; Widow.VeilLands)
            windupSound = Clips("widow_veil_draw"), swingSound = Clips("widow_veil"),
        };
        w.knellSound = Clips("widow_knell");
        w.throughSound = Clips("widow_through");
        w.struckSound = Clips("widow_struck");
        w.tendSound = Clips("widow_tend");
        w.deathSound = Clips("widow_death");
        w.music = new[] // (her lament; at half, the same loop more urgent, crossfading in step)
        {
            AssetDatabase.LoadAssetAtPath<AudioClip>(Audio + "Music/music_widow_1.wav"),
            AssetDatabase.LoadAssetAtPath<AudioClip>(Audio + "Music/music_widow_2.wav"),
        };
        BeltLantern(go, w, "Lantern");
        Swoosh(go, "VeilTip", "WidowLace", SouredSwoosh, 0.2f, null, 0.5f, "Veil"); // (the veil's lace hem, swept low along the floor)
        return w;
    }
}
