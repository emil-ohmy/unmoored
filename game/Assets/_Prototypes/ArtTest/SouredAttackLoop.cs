using System.Collections;
using System.Linq;
using UnityEngine;

// PROTOTYPE (wayfinder ticket "Art style test"). Throwaway.
namespace MyGame.Prototypes.ArtTest
{
    // Loops the Soured's attack so the animation-only telegraph can be judged from the game camera.
    public class SouredAttackLoop : MonoBehaviour
    {
        public float idleSeconds = 1.5f;
        public string Phase { get; private set; } = "";

        Animation anim;

        IEnumerator Start()
        {
            anim = GetComponentInChildren<Animation>();
            string Clip(string suffix) => anim.Cast<AnimationState>().First(s => s.name.EndsWith(suffix)).name;
            string idle = Clip("Idle"), windup = Clip("Windup"), strike = Clip("Strike"), recover = Clip("Recover");
            anim[idle].wrapMode = WrapMode.Loop;
            foreach (var clip in new[] { windup, strike, recover })
                anim[clip].wrapMode = WrapMode.ClampForever;

            while (true)
            {
                Phase = "Idle";
                anim.CrossFade(idle, 0.2f);
                yield return new WaitForSeconds(idleSeconds);
                Phase = "WINDUP (telegraph)";
                anim.CrossFade(windup, 0.08f);
                yield return new WaitForSeconds(anim[windup].length);
                Phase = "STRIKE";
                anim.Play(strike);
                yield return new WaitForSeconds(anim[strike].length);
                Phase = "Recover (punish window)";
                anim.CrossFade(recover, 0.05f);
                yield return new WaitForSeconds(anim[recover].length);
            }
        }

        void OnGUI()
        {
            GUI.Label(new Rect(16, 12, 600, 24), "<b>PROTOTYPE: art style test</b>", new GUIStyle(GUI.skin.label) { richText = true, fontSize = 16 });
            GUI.Label(new Rect(16, 36, 600, 24), $"Soured: {Phase}", new GUIStyle(GUI.skin.label) { fontSize = 16 });
        }
    }
}
