using System.Collections.Generic;
using UnityEngine;

namespace MyGame
{
    // Something in the world the oar can strike that isn't a Soured (a water wheel): it answers in its own way, to say
    // something can be done here. One per swing, and only when the swing finds no Soured.
    public interface IStrikable
    {
        Vector3 StrikePoint { get; }
        float StrikeRadius { get; }
        void Struck(Apprentice by, bool heavy);
    }

    public static class Strikables
    {
        static readonly List<IStrikable> all = new List<IStrikable>();
        public static IReadOnlyList<IStrikable> All => all;
        public static void Register(IStrikable s) { if (!all.Contains(s)) all.Add(s); }
        public static void Unregister(IStrikable s) => all.Remove(s);
    }
}
