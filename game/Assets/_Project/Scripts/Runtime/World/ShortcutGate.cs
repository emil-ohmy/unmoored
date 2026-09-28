using UnityEngine;

namespace MyGame
{
    // A shortcut: a barred gate that opens only from its far side (the way you've fought through), swinging open
    // for good. From the near side (the Mooring post's) it rattles: barred. Opened, it stays open through Rest and
    // death (the world remembers what you've opened; saving it to disk comes later).
    public sealed class ShortcutGate : MonoBehaviour, IInteractable
    {
        public Transform door;               // swings about its hinge (the model's GateDoor, re-parented at runtime)
        public Vector3 openFrom = Vector3.left; // world direction of the side it can be opened from
        public float swingDegrees = 105f;
        public float swingTime = 0.9f;
        public float interactRadius = 1.8f;

        public bool IsOpen { get; private set; }
        public float Swing => hinge != null ? Mathf.Abs(Mathf.DeltaAngle(0f, hinge.eulerAngles.y)) : 0f;
        public Transform Door => door;

        Transform hinge;
        float openedAt = -1f, rattleAt = -10f, swingSign = 1f;

        // The hinge is the door's edge across the walkway (found from its bounds, whatever the model's axes), and it
        // swings away from the side it's opened from.
        void Awake()
        {
            if (door == null)
                door = transform.Find("GateDoor");
            var renderer = door != null ? door.GetComponentInChildren<Renderer>() : null;
            if (renderer == null)
                return;
            Bounds b = renderer.bounds;
            Vector3 across = Vector3.Cross(Vector3.up, openFrom).normalized;
            Vector3 edge = b.center + across * Vector3.Scale(b.extents, new Vector3(Mathf.Abs(across.x), 0f, Mathf.Abs(across.z))).magnitude;
            hinge = new GameObject("Hinge").transform;
            hinge.SetParent(transform, true);
            hinge.position = new Vector3(edge.x, transform.position.y, edge.z);
            door.SetParent(hinge, true);
            Vector3 centre = b.center - hinge.position;
            Vector3 swung = Quaternion.Euler(0f, 90f, 0f) * centre;
            swingSign = Vector3.Dot(swung, openFrom) < 0f ? 1f : -1f;
        }

        void OnEnable() => Interactables.Register(this);
        void OnDisable() => Interactables.Unregister(this);

        public Vector3 InteractPosition => transform.position;
        public float InteractRadius => interactRadius;
        public bool Available => !IsOpen;
        public string PromptLabel => OnOpeningSide(FindAnyObjectByType<Apprentice>()) ? "Open" : "Barred";

        bool OnOpeningSide(Apprentice apprentice) =>
            apprentice != null && Vector3.Dot(apprentice.transform.position - transform.position, openFrom) > 0f;

        public void Interact(Apprentice apprentice)
        {
            if (!OnOpeningSide(apprentice))
            {
                rattleAt = Time.time; // barred from this side
                Sfx.Instance?.GateRattle(transform.position + Vector3.up);
                return;
            }
            IsOpen = true;
            openedAt = Time.time;
            Sfx.Instance?.GateOpen(transform.position + Vector3.up);
        }

        void Update()
        {
            if (hinge == null)
                return;
            if (IsOpen)
            {
                float u = Mathf.Clamp01((Time.time - openedAt) / swingTime);
                float eased = 1f - Mathf.Pow(1f - u, 3f);
                // World space: the model's root carries its import rotation, which the hinge mustn't inherit.
                hinge.rotation = Quaternion.Euler(0f, swingSign * swingDegrees * eased, 0f); // away from whoever opened it
            }
            else
            {
                float r = Time.time - rattleAt; // a short shudder against the bar
                hinge.rotation = Quaternion.Euler(0f, r < 0.35f ? 2.5f * Mathf.Sin(r * 60f) * (1f - r / 0.35f) : 0f, 0f);
            }
        }
    }
}
