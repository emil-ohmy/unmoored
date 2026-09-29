using UnityEngine;

namespace MyGame
{
    // The Ferryman's hook, freed when the Anchoress lets go: it rises out of her in his cold blue light, floats to his
    // mark at the chapel's heart (floor that never breaks) and comes to rest there, glowing like the key item it is.
    // Take it: the key-item ceremony, and the Hook is the Apprentice's.
    public sealed class FerrymansHook : MonoBehaviour, IInteractable
    {
        public float flightTime = 1.6f;
        public float interactRadius = 1.6f;
        public AudioClip[] landSound;

        public bool Landed { get; private set; }
        public bool Taken { get; private set; }

        Vector3 from, to;
        Quaternion restRotation;
        float since;
        Transform model;
        PickupGlow glow;

        // Frees it at `from` (her waist), to come to rest at `to`.
        public static FerrymansHook Free(GameObject hookModel, Vector3 from, Vector3 to, AudioClip[] landSound)
        {
            var go = new GameObject("The Ferryman's Hook");
            go.SetActive(false); // (its glow reads its settings as it wakes)
            go.transform.position = from;
            var hook = go.AddComponent<FerrymansHook>();
            hook.from = from;
            hook.to = to;
            hook.landSound = landSound;
            if (hookModel != null)
            {
                var m = Instantiate(hookModel, go.transform);
                foreach (var c in m.GetComponentsInChildren<Collider>())
                    Destroy(c);
                m.transform.localPosition = Vector3.zero;
                m.transform.localScale = Vector3.one * 1.4f;
                hook.model = m.transform;
                hook.restRotation = Quaternion.Euler(90f, 30f, 0f) * hookModel.transform.localRotation; // lying on its side
            }
            var glow = go.AddComponent<PickupGlow>(); // a key item: bright, in his cold blue
            glow.tier = PickupGlow.Tier.Key;
            glow.colour = new Color(0.62f, 0.77f, 1f);
            glow.height = 0.9f;
            glow.glowing = hook.model != null ? hook.model.GetComponentsInChildren<Renderer>() : new Renderer[0];
            hook.glow = glow;
            go.SetActive(true);
            return hook;
        }

        // Loading a save: already at rest on his mark, waiting to be taken.
        public static FerrymansHook AtRest(GameObject hookModel, Vector3 spot)
        {
            var hook = Free(hookModel, spot, spot, null);
            hook.Landed = true;
            hook.transform.position = spot;
            if (hook.model != null)
                hook.model.rotation = hook.restRotation;
            return hook;
        }

        void OnEnable() => Interactables.Register(this);
        void OnDisable() => Interactables.Unregister(this);

        void Awake() => since = Time.time;

        public Vector3 InteractPosition => transform.position;
        public float InteractRadius => interactRadius;
        public bool Available => Landed && !Taken;
        public string PromptLabel => "Take";

        public void Interact(Apprentice apprentice)
        {
            Taken = true;
            gameObject.SetActive(false);
            apprentice.ReceiveKeyItem("The Hook", Apprentice.HookHint, apprentice.GiveHook);
        }

        void Update()
        {
            if (Landed)
            {
                if (model != null) // at rest, it breathes a little
                    model.localPosition = Vector3.up * (0.05f + 0.02f * Mathf.Sin(Time.time * 1.6f));
                return;
            }
            float t = Mathf.Clamp01((Time.time - since) / flightTime);
            float e = UiKit.Smooth(0f, 1f, t);
            transform.position = Vector3.Lerp(from, to, e) + Vector3.up * (1.6f * Mathf.Sin(t * Mathf.PI)); // up out of her, over, down
            if (model != null)
                model.rotation = Quaternion.Slerp(Quaternion.Euler(0f, Time.time * 360f, 0f) * restRotation, restRotation, e);
            if (Effects.Instance != null && Random.value < 0.5f)
                Effects.Instance.Wisp(transform.position);
            if (t >= 1f)
            {
                Landed = true;
                transform.position = to;
                Sfx.Instance?.Attack(landSound, to, 0.9f, "hook_rest");
                Effects.Instance?.KeyItemRing(to + Vector3.up * 0.3f);
            }
        }
    }
}
