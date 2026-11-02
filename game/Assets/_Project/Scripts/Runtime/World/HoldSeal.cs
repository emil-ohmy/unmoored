using UnityEngine;

namespace MyGame
{
    // The Hold's arena sealed while it fights: as it wakes, the floor of the way in heaves up behind you into a low ridge
    // of the living stone, hands of the kept clawing out of it (hold_pieces' seal), and an unseen wall holds the way;
    // when the fight ends (you fall, or it lets go) it sinks back into the floor. Only behind you: if you've stepped back
    // out while it woke, it stays down. This transform: the ridge's middle on the floor, its forward into the chamber.
    public sealed class HoldSeal : MonoBehaviour
    {
        public TheHoldWall hold;
        public Transform ridge;                // (its pieces: risen at this transform's height, sunk `sunk` below)
        public Vector3 wallSize = new Vector3(6.5f, 3f, 1.2f);
        public float sunk = 1.9f, riseTime = 0.55f, sinkTime = 2.5f;
        public AudioClip[] riseSound, sinkSound;

        public bool Shut { get; private set; }
        public float Raised => up; // 0 down, 1 up (tests)

        BoxCollider wall;
        Apprentice apprentice;
        float up;

        void Awake()
        {
            wall = gameObject.AddComponent<BoxCollider>();
            wall.size = wallSize;
            wall.center = Vector3.up * wallSize.y * 0.5f;
            wall.enabled = false;
            Place();
        }

        void Start() => apprentice = FindAnyObjectByType<Apprentice>();

        void Update()
        {
            if (hold == null)
                return;
            bool on = (hold.State == TheHoldWall.Mood.Waking || hold.State == TheHoldWall.Mood.Fighting)
                      && apprentice != null && apprentice.IsAlive
                      && (Shut || Vector3.Dot(apprentice.transform.position - transform.position, transform.forward) > 0.8f);
            if (on != Shut)
            {
                Shut = on;
                wall.enabled = on;
                Sfx.Instance?.Attack(on ? riseSound : sinkSound, transform.position + Vector3.up, 1f, on ? "seal_rise" : "seal_sink");
                if (on)
                {
                    Effects.Instance?.Rubble(transform.position, 1.4f);
                    CameraRig.Instance?.Shake(0.2f);
                }
            }
            float was = up;
            up = Mathf.MoveTowards(up, Shut ? 1f : 0f, Time.deltaTime / (Shut ? riseTime : sinkTime));
            if (up != was)
                Place();
        }

        void Place()
        {
            if (ridge == null)
                return;
            float e = Shut ? 1f - (1f - up) * (1f - up) : UiKit.Smooth(0f, 1f, up); // (heaved up hard; sunk slowly)
            ridge.localPosition = Vector3.up * Mathf.Lerp(-sunk, 0f, e);
            ridge.gameObject.SetActive(up > 0f);
        }
    }
}
