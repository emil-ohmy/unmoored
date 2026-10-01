using UnityEngine;

namespace MyGame
{
    // A gateway shut while a fight is on (the Watchman's field: its two old plank gates swing to behind you and slam,
    // the moment he's found). An unseen wall holds the way while they're shut; when the fight ends (you fall, or he's
    // beaten) they creak open again.
    public sealed class ArenaGate : MonoBehaviour
    {
        public MonoBehaviour boss;            // the IBoss whose fight shuts it
        public Transform[] leaves;            // each hung at its hinge
        public float[] shutYaw, openYaw;      // each leaf's yaw, shut and open
        public Vector3 wallSize = new Vector3(2.4f, 3f, 0.5f); // across the gateway (this transform: its middle, facing through it)
        public float shutTime = 0.35f, openTime = 1.6f;
        public AudioClip[] shutSound, openSound;

        public bool Shut { get; private set; }
        public float Closed => closed; // 0 open, 1 shut

        BoxCollider wall;
        float closed;

        void Awake()
        {
            wall = gameObject.AddComponent<BoxCollider>();
            wall.size = wallSize;
            wall.center = Vector3.up * wallSize.y * 0.5f;
            wall.enabled = false;
        }

        void Update()
        {
            bool fight = boss is IBoss b && BossFight.Current == b;
            if (fight != Shut)
            {
                Shut = fight;
                wall.enabled = fight;
                Sfx.Instance?.Attack(fight ? shutSound : openSound, transform.position + Vector3.up, 1f, fight ? "gate_slam" : "gate_open");
                if (fight)
                    Effects.Instance?.Rubble(transform.position, 1.0f); // (dust shaken off the arch)
            }
            float was = closed;
            closed = Mathf.MoveTowards(closed, Shut ? 1f : 0f, Time.deltaTime / (Shut ? shutTime : openTime));
            if (Shut && was < 1f && closed >= 1f)
                CameraRig.Instance?.Shake(0.15f); // (slammed)
            float e = Shut ? closed * closed : UiKit.Smooth(0f, 1f, closed); // (swung to hard; let open slowly)
            for (int i = 0; i < leaves.Length; i++)
                leaves[i].rotation = Quaternion.Euler(0f, Mathf.LerpAngle(openYaw[i], shutYaw[i], e), 0f) * root[i];
        }

        Quaternion[] root;

        void Start()
        {
            // Each leaf keeps its model's own root rotation under the yaw (as placed).
            root = new Quaternion[leaves.Length];
            for (int i = 0; i < leaves.Length; i++)
                root[i] = Quaternion.Euler(0f, -openYaw[i], 0f) * leaves[i].rotation;
        }
    }
}
