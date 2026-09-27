using UnityEngine;

// PROTOTYPE (wayfinder ticket "Combat feel: core verbs and pace"). Throwaway code, not production.
namespace MyGame.Prototypes.CombatFeel
{
    // Debug overlay that shows all the state that matters for judging feel.
    public class CombatHud : MonoBehaviour
    {
        ProtoPlayer player;
        ProtoSoured enemy;
        GUIStyle label, big, small;

        void Start()
        {
            player = FindAnyObjectByType<ProtoPlayer>();
            enemy = FindAnyObjectByType<ProtoSoured>();
        }

        void OnGUI()
        {
            var tuning = CombatTuning.Instance;
            if (tuning == null || player == null)
                return;
            label ??= new GUIStyle(GUI.skin.label) { fontSize = 16, richText = true };
            big ??= new GUIStyle(GUI.skin.label) { fontSize = 34, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            small ??= new GUIStyle(GUI.skin.label) { fontSize = 13, richText = true };
            var p = tuning.P;

            GUILayout.BeginArea(new Rect(16, 12, 480, 400));
            GUILayout.Label("<b>PROTOTYPE: combat feel</b>", label);
            GUILayout.Label($"Pace: <b><color=#ffcc55>[{tuning.current + 1}] {p.name}</color></b>   (keys 1/2/3 or D-pad left/right)", label);
            Bar("HP", player.Health / player.maxHealth, new Color(0.85f, 0.25f, 0.25f));
            Bar("Stamina", player.Stamina / p.maxStamina, new Color(0.3f, 0.8f, 0.35f));
            string iframes = player.Invulnerable ? "  <color=#66ccff><b>INVULNERABLE</b></color>" : "";
            GUILayout.Label($"Player: {player.CurrentState} {player.Phase}{iframes}", label);
            if (enemy != null)
            {
                GUILayout.Space(6);
                Bar("Soured", Mathf.Max(0f, enemy.Health) / enemy.maxHealth, new Color(0.6f, 0.4f, 0.8f));
                string enemyState = enemy.CurrentState == ProtoSoured.State.Scattered
                    ? $"Scattered (re-forms in {enemy.reformDelay - enemy.StateTime:0.0}s)"
                    : enemy.CurrentState.ToString();
                GUILayout.Label($"Soured: {enemyState}", label);
            }
            if (GUILayout.Button("Log this preset's values to the Console", GUILayout.Width(330)))
                Debug.Log($"[CombatFeel] {p.name}\n{JsonUtility.ToJson(p, true)}");
            GUILayout.EndArea();

            if (Time.unscaledTime - player.LastEventTime < 0.8f)
                GUI.Label(new Rect(0, 70, Screen.width, 50), player.LastEvent, big);

            GUILayout.BeginArea(new Rect(16, Screen.height - 110, 900, 100));
            GUILayout.Label("<b>Move</b> WASD / arrows / left stick    <b>Roll</b> Space / B    " +
                            "<b>Light</b> J / LMB / X / RB    <b>Heavy</b> K / RMB / Y / RT    <b>Reset</b> R / Select", small);
            GUILayout.Label($"move {p.moveSpeed}  ·  roll {p.rollDuration}s/{p.rollDistance}m, i-frames {p.iFrameStart}–{p.iFrameEnd}s, cost {p.rollStaminaCost}  ·  " +
                            $"stamina regen {p.staminaRegenPerSec}/s after {p.staminaRegenDelay}s", small);
            GUILayout.Label($"light {p.lightWindup}/{p.lightActive}/{p.lightRecovery}s ×{p.lightComboLength}, cost {p.lightStaminaCost}  ·  " +
                            $"heavy {p.heavyWindup}/{p.heavyActive}/{p.heavyRecovery}s, cost {p.heavyStaminaCost}  ·  " +
                            $"roll cancels recovery: {(p.rollCancelsRecovery ? "yes" : "no")}  ·  Soured telegraph {p.enemyTelegraph}s, recovery {p.enemyRecovery}s", small);
            GUILayout.EndArea();
        }

        void Bar(string name, float fill, Color color)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(name, label, GUILayout.Width(80));
            Rect r = GUILayoutUtility.GetRect(260, 18, GUILayout.Width(260));
            GUI.color = new Color(0f, 0f, 0f, 0.6f);
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = color;
            GUI.DrawTexture(new Rect(r.x, r.y, r.width * Mathf.Clamp01(fill), r.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUILayout.EndHorizontal();
        }
    }
}
