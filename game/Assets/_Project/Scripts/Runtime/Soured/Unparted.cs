using System.Collections.Generic;
using UnityEngine;

namespace MyGame
{
    // The Unparted, of the Last Embrace: the Drowned City's miniboss. Two souls who never let each other go, fused into
    // one great hunched thing: Hold, who carries, and Fast, who rides its back (a Clinger: it leaps down at you, clings,
    // scrambles back up). One fight, one name plate, two bars. Scatter Fast first and Hold goes berserk; scatter Hold
    // first and Fast keens and weeps where it fell. Both scattered: its farewell (the game's joke: docs/brief.md).
    public sealed class Unparted : MonoBehaviour, IBoss, IBossParts
    {
        public UnpartedHold hold;
        public Clinger fast;
        public string title = "The Unparted";
        public string epithet = "of the Last Embrace";
        public string farewell = "TILL DEATH DID THEY PART";
        public string farewellLine = "Again.";
        public float leash = 16f; // further than this from them, the fight is off (they go back)

        public int TimesDefeated { get; private set; }

        Apprentice apprentice;
        bool defeated;
        readonly List<(string name, float fraction)> parts = new List<(string, float)>();

        public string Title => title;
        public string Epithet => epithet;
        public bool IsAlive => hold.IsAlive || fast.IsAlive;
        public float HealthFraction => (Left(hold) + Left(fast)) / (hold.maxHealth + fast.maxHealth);
        public float[] PhaseMarks => new float[0];
        public int Phase => hold.IsAlive && fast.IsAlive ? 1 : 2;
        public AudioClip[] Music => null; // (a miniboss: the Area's own music plays on)
        public string Farewell => farewell;
        public string FarewellLine => farewellLine;

        public IReadOnlyList<(string name, float fraction)> Parts
        {
            get
            {
                parts.Clear();
                parts.Add(("Hold", Left(hold) / hold.maxHealth));
                parts.Add(("Fast", Left(fast) / fast.maxHealth));
                return parts;
            }
        }

        public bool Owns(Soured soured) => soured == hold || soured == fast;

        static float Left(Soured s) => s.IsAlive ? Mathf.Max(0f, s.Health) : 0f;

        void Start() => apprentice = FindAnyObjectByType<Apprentice>();

        void Update()
        {
            if (!IsAlive)
            {
                if (!defeated)
                {
                    defeated = true; // both scattered: its farewell
                    TimesDefeated++;
                    BossFight.Defeat(this);
                    BossFight.End(this);
                }
                return;
            }
            defeated = false;
            bool aware = Aware(hold) || (!fast.Mounted && Aware(fast));
            bool near = apprentice != null && Vector3.Distance(apprentice.transform.position, (hold.IsAlive ? hold : (Soured)fast).transform.position) < leash;
            if (aware && near && apprentice.IsAlive)
                BossFight.Begin(this); // its name plate and bars on screen
            else
                BossFight.End(this);
        }

        static bool Aware(Soured s) =>
            s.IsAlive && s.CurrentState != Soured.State.Idle && s.CurrentState != Soured.State.Returning;

        void OnDisable() => BossFight.End(this);
    }
}
