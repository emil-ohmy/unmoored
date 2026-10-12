using UnityEngine;

namespace MyGame
{
    // Another place a Clockwork can be set running from (a drive on the far side of what it moves: a door's, inside).
    public sealed class ClockworkDrive : MonoBehaviour, IStoodOnWaveable
    {
        public Clockwork clockwork;
        public Transform strikePoint;
        public float strikeRadius = 1.3f;

        void OnEnable() => Waveables.Register(this);
        void OnDisable() => Waveables.Unregister(this);

        public Vector3 WavePoint => strikePoint != null ? strikePoint.position : transform.position;
        public float WaveRadius => strikeRadius;
        public void TakeWave(StrokeWave wave, Vector3 push) => clockwork?.Set();
    }
}
