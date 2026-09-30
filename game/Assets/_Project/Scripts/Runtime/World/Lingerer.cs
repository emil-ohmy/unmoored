using System.Collections.Generic;
using UnityEngine;

namespace MyGame
{
    // A Lingerer: a soul held back by unfinished business, who can cross once it's resolved and their fare is paid.
    // Speak with them (Interact): what they say follows where their business stands. Hesper's is a keepsake: first she
    // tells you of her son's light out on the water (Waiting); bring it and she lights it, lets him go, and asks after
    // the fare (Given); pay it (always optional) and she goes to wait by the boat for the Crossing (Paid).
    public sealed class Lingerer : MonoBehaviour, IInteractable
    {
        public enum Business { Unmet, Waiting, Given, Paid }

        public string id = "lingerer";
        public string displayName = "Lingerer";
        public string keepsake = "";          // the id of what they wait for
        public int fare = 100;
        public float interactRadius = 1.6f;
        public Transform waitAt;              // where they wait for the Crossing, once the fare is paid
        public GameObject heldKeepsake;       // shown in their hands once given
        public Light kept;                    // their own light (it brightens when the keepsake is given)

        [Header("Their words, by where their business stands")]
        [TextArea] public string[] meeting;   // the first time
        [TextArea] public string[] waiting;   // again, before the keepsake
        [TextArea] public string[] waitingAsides; // one before the reminder, a different one each visit
        [TextArea] public string[] waitingWithoutHook; // (added, when you can't reach it yet)
        [TextArea] public string[] given;     // the keepsake brought
        [TextArea] public string[] fareAsked; // the fare, as the keepsake is given
        [TextArea] public string[] fareAgain; // the fare, asked again on coming back
        [TextArea] public string[] declined;  // "Not yet"
        [TextArea] public string[] paid;      // the fare paid
        [TextArea] public string[] ready;     // waiting by the boat
        [TextArea] public string[] readyAsides; // after it, a different one each visit
        public string payLabel = "Pay the fare", declineLabel = "Not yet";

        public Business State { get; private set; }
        int visits; // (this sitting: which aside comes next)
        public string SaveId => id;

        Vector3 basePosition;
        Quaternion baseRotation;
        float keptIntensity;

        void Awake()
        {
            basePosition = transform.position;
            baseRotation = shown = transform.rotation;
            if (kept != null)
                keptIntensity = kept.intensity;
            renderers = GetComponentsInChildren<Renderer>(true);
            lights = GetComponentsInChildren<Light>(true);
            block = new MaterialPropertyBlock();
            Show();
        }

        void OnEnable() => Interactables.Register(this);
        void OnDisable() => Interactables.Unregister(this);

        public Vector3 InteractPosition => basePosition;
        public float InteractRadius => interactRadius;
        public bool Available => !Drifting;
        public string PromptLabel => "Speak";

        public void Interact(Apprentice apprentice)
        {
            Face(apprentice);
            var lines = new List<string>();
            List<Conversation.Choice> choices = null;
            switch (State)
            {
                case Business.Unmet:
                    lines.AddRange(meeting);
                    State = Business.Waiting;
                    break;
                case Business.Waiting when apprentice.Equipment.Carries(keepsake):
                    lines.AddRange(given);
                    apprentice.Equipment.Give(keepsake);
                    State = Business.Given;
                    Sfx.Instance?.LanternLit(transform.position + Vector3.up * 0.6f); // it's lit again, in her hands
                    Show();
                    lines.AddRange(fareAsked);
                    choices = FareChoices(apprentice);
                    break;
                case Business.Waiting:
                    AddAside(lines, waitingAsides);
                    lines.AddRange(waiting);
                    if (!apprentice.HasHook && waitingWithoutHook != null)
                        lines.AddRange(waitingWithoutHook);
                    break;
                case Business.Given:
                    lines.AddRange(fareAgain != null && fareAgain.Length > 0 ? fareAgain : fareAsked);
                    choices = FareChoices(apprentice);
                    break;
                default:
                    lines.AddRange(ready);
                    AddAside(lines, readyAsides);
                    break;
            }
            apprentice.ShowMenu(new Conversation(displayName, InteractPosition + Vector3.up, lines, choices));
        }

        void AddAside(List<string> lines, string[] asides)
        {
            if (asides != null && asides.Length > 0)
                lines.Add(asides[visits++ % asides.Length]);
        }

        List<Conversation.Choice> FareChoices(Apprentice apprentice)
        {
            bool afford = apprentice.Obols >= fare;
            return new List<Conversation.Choice>
            {
                new Conversation.Choice
                {
                    Label = $"{payLabel} ({fare} Obols)" + (afford ? "" : ": you carry too few"), Dim = !afford,
                    Chosen = () => { if (afford) Pay(apprentice); },
                },
                new Conversation.Choice
                {
                    Label = declineLabel,
                    Chosen = () =>
                    {
                        if (declined != null && declined.Length > 0)
                            apprentice.ShowMenu(new Conversation(displayName, InteractPosition + Vector3.up, declined));
                    },
                },
            };
        }

        void Pay(Apprentice apprentice)
        {
            if (!apprentice.SpendObols(fare))
                return;
            State = Business.Paid;
            Sfx.Instance?.ObolClink(transform.position + Vector3.up);
            // She thanks you, then goes to wait by the boat: a soul thins away and gathers again where it's going.
            apprentice.ShowMenu(new Conversation(displayName, InteractPosition + Vector3.up, paid, null, () =>
            {
                if (waitAt != null)
                    StartDrift();
            }));
        }

        // Loading a save: where their business stood.
        public void Restore(Business state)
        {
            State = state;
            Show();
            if (state == Business.Paid)
                GoToWait();
        }

        // What they hold: the keepsake in their hands once given, their light warmer.
        void Show()
        {
            if (heldKeepsake != null)
                heldKeepsake.SetActive(State >= Business.Given);
            if (kept == null)
                return;
            float intensity = keptIntensity * (State >= Business.Given ? 1.8f : 1f);
            if (kept.TryGetComponent<LightFlicker>(out var flicker))
                flicker.BaseIntensity = intensity;
            else
                kept.intensity = intensity;
        }

        void GoToWait()
        {
            if (waitAt == null)
                return;
            basePosition = waitAt.position;
            baseRotation = shown = waitAt.rotation;
            turnUntil = -1f;
        }

        // Going to wait by the boat: she thins away into wisps (her light with her), and gathers again there.
        public const float DriftOut = 1.3f, DriftPause = 0.4f, DriftIn = 1.3f;
        public bool Drifting => driftStart >= 0f;
        float driftStart = -1f;
        bool driftMoved;
        Renderer[] renderers;
        Light[] lights;
        float[] lightBases;
        MaterialPropertyBlock block;

        void StartDrift()
        {
            driftStart = Time.time;
            driftMoved = false;
            lightBases = new float[lights.Length];
            for (int i = 0; i < lights.Length; i++)
                lightBases[i] = lights[i].TryGetComponent<LightFlicker>(out var f) ? f.BaseIntensity : lights[i].intensity;
            Sfx.Instance?.SoulAbsorb(basePosition + Vector3.up * 0.6f);
        }

        void UpdateDrift()
        {
            float t = Time.time - driftStart;
            float fade; // 0: all there, 1: gone
            if (t < DriftOut)
                fade = UiKit.Smooth(0f, DriftOut, t);
            else if (t < DriftOut + DriftPause)
                fade = 1f;
            else
                fade = 1f - UiKit.Smooth(DriftOut + DriftPause, DriftOut + DriftPause + DriftIn, t);
            if (!driftMoved && t >= DriftOut)
            {
                driftMoved = true;
                GoToWait();
                Sfx.Instance?.SoulAbsorb(basePosition + Vector3.up * 0.6f);
            }
            bool fading = fade > 0.001f && (t < DriftOut || t > DriftOut + DriftPause);
            if (fading && Effects.Instance != null && Random.value < 0.6f)
                Effects.Instance.Wisp(basePosition + Vector3.up * Random.Range(0.2f, 1.1f) + Random.insideUnitSphere * 0.35f, new Color(1f, 0.8f, 0.55f, 1f));
            SetFade(fade);
            if (t >= DriftOut + DriftPause + DriftIn)
            {
                driftStart = -1f;
                SetFade(0f);
            }
        }

        void SetFade(float fade)
        {
            for (int i = 0; i < lights.Length; i++) // her light goes with her
            {
                float intensity = lightBases[i] * (1f - fade);
                if (lights[i].TryGetComponent<LightFlicker>(out var flicker))
                    flicker.BaseIntensity = intensity;
                else
                    lights[i].intensity = intensity;
            }
            foreach (var r in renderers)
            {
                if (fade <= 0f)
                {
                    r.SetPropertyBlock(null);
                    continue;
                }
                block.Clear();
                block.SetFloat("_Dissolve", Mathf.Lerp(0.02f, 1f, fade));
                block.SetColor("_DissolveEdgeColor", new Color(2.4f, 1.6f, 0.8f)); // a warm edge: her lamplight
                r.SetPropertyBlock(block);
            }
        }

        // A small turn towards whoever speaks with them (souls are slow to look away from the water), then back.
        void Face(Apprentice apprentice)
        {
            Vector3 to = apprentice.transform.position - basePosition;
            to.y = 0f;
            Vector3 front = -(baseRotation * Vector3.forward); // (a static model: its forward is its visual back)
            front.y = 0f;
            if (to.sqrMagnitude < 0.01f || front.sqrMagnitude < 0.01f)
                return;
            Quaternion full = Quaternion.FromToRotation(front.normalized, to.normalized);
            turnTo = Quaternion.Slerp(Quaternion.identity, full, 0.35f) * baseRotation;
            turnUntil = Time.time + 6f;
        }

        Quaternion turnTo, shown;
        float turnUntil = -1f;

        void Update()
        {
            if (Drifting)
                UpdateDrift();
            float t = Time.time;
            shown = Quaternion.Slerp(shown, t < turnUntil ? turnTo : baseRotation, Time.deltaTime * 2f);
            // A soul breathes a little: a slow bob and sway.
            transform.SetPositionAndRotation(basePosition + Vector3.up * (0.015f * Mathf.Sin(t * 0.9f)),
                shown * Quaternion.Euler(0.8f * Mathf.Sin(t * 0.6f), 0f, 1.2f * Mathf.Sin(t * 0.8f + 1f)));
        }
    }
}
