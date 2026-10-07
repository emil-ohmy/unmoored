using System.Collections.Generic;
using UnityEngine;

namespace MyGame
{
    // Something the Stroke's wave moves or puts out, that isn't a Soured: a barrel afloat or a crate left loose (shoved),
    // burning pitch and a fireball (put out, if the wave carries water), a mill-race (it carries the wave on).
    public interface IWaveable
    {
        Vector3 WavePoint { get; }  // where the wave meets it
        float WaveRadius { get; }   // how far round that it's met
        void TakeWave(StrokeWave wave, Vector3 push); // once per wave; `push`: away from the wave's origin
    }

    public static class Waveables
    {
        static readonly List<IWaveable> all = new List<IWaveable>();
        public static IReadOnlyList<IWaveable> All => all;
        public static void Register(IWaveable w) { if (!all.Contains(w)) all.Add(w); }
        public static void Unregister(IWaveable w) => all.Remove(w);
    }
}
