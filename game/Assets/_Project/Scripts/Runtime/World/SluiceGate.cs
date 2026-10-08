using UnityEngine;

namespace MyGame
{
    // A sluice gate shutting a doorway, chained to a water wheel: each time the wheel's turned it winds the gate up a
    // stage (a ratchet holds it there, with a clunk), until it's right up: open for good. Its chain is drawn from the
    // wheel's axle, over the pulleys, down to the ring on the gate's top.
    public sealed class SluiceGate : MonoBehaviour
    {
        public Transform board;            // the gate itself (rises)
        public float travel = 1.85f;
        public int stages = 2;             // turns of the wheel to raise it right up
        public float riseSpeed = 0.7f;
        public LineRenderer chain;         // its last point follows the board's ring
        public float ringHeight = 2.05f;   // the ring, above the board's base
        public AudioClip[] riseSound, clunkSound;

        public int Stage { get; private set; }
        public bool Open => Stage >= stages && Mathf.Abs(Height - travel) < 0.01f;
        public float Height { get; private set; }

        Vector3 closed;

        void Awake()
        {
            if (board != null)
                closed = board.position;
            Place();
        }

        // The wheel turned: wound up a stage.
        public void Raise()
        {
            if (Stage >= stages)
                return;
            Stage++;
            Sfx.Instance?.Attack(riseSound, transform.position + Vector3.up * 2f, 0.9f, "sluice_rise");
        }

        void Update()
        {
            float target = travel * Stage / Mathf.Max(1, stages);
            if (Mathf.Approximately(Height, target))
                return;
            Height = Mathf.MoveTowards(Height, target, riseSpeed * Time.deltaTime);
            if (Mathf.Approximately(Height, target))
            {
                Sfx.Instance?.Attack(clunkSound, transform.position + Vector3.up * 2f, Stage >= stages ? 1f : 0.8f, "sluice_clunk");
                CameraRig.Instance?.Shake(Stage >= stages ? 0.2f : 0.1f);
            }
            Place();
        }

        void Place()
        {
            if (board != null)
                board.position = closed + Vector3.up * Height;
            if (chain != null && chain.positionCount > 0 && board != null)
                chain.SetPosition(chain.positionCount - 1, board.position + Vector3.up * ringHeight);
        }
    }
}
