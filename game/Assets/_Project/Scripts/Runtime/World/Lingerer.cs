using System.Collections.Generic;
using UnityEngine;

namespace MyGame
{
    // A Lingerer: a soul held back by unfinished business, who can cross once it's resolved and their fare is paid.
    // Speak with them (Interact): what they say follows where their business stands. Hesper's is a keepsake: first she
    // tells you of her son's light out on the water (Waiting); bring it and she lights it, lets him go, and asks after
    // the fare (Given); pay it (always optional) and she goes to wait by the boat (Paid). Take the oars and she comes
    // aboard: the Crossing. Out on the water, nearing the Far shore, the oar rests and she talks as the boat drifts in;
    // put in there and she says goodbye, leaves a parting gift, and goes on into the light (Crossed).
    // Two figures: on land (`figure`, sat where she waits) and in the boat's bow (`boatFigure`), each a SoulFade.
    public sealed class Lingerer : MonoBehaviour, IInteractable
    {
        public enum Business { Unmet, Waiting, Given, Paid, Crossed }

        public string id = "lingerer";
        public string displayName = "Lingerer";
        public string keepsake = "";          // the id of what they wait for
        public int fare = 100;
        public float interactRadius = 1.6f;
        public Transform figure;              // them, on land
        public Transform waitAt;              // where they wait for the Crossing, once the fare is paid
        public GameObject heldKeepsake;       // shown in their hands once given
        public Light kept;                    // their own light (it brightens when the keepsake is given)

        [Header("The Crossing")]
        public GameObject boatFigure;         // them, sat in the boat's bow (hidden until they come aboard)
        public Light farLight;                // the Far shore's light, which flares as they go on into it
        public GameObject leftBehind;         // what they leave on the Far shore (shown once they've crossed)
        public string giftCharm = "";         // their parting gift
        public string giftHint = "";
        public float talkDistance = 13f;      // this near the Far shore, the oar rests and they talk

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
        [TextArea] public string[] crossing;  // on the water, drifting in to the Far shore
        [TextArea] public string[] farewell;  // at the Far shore, before they go
        public string payLabel = "Pay the fare", declineLabel = "Not yet";

        public Business State { get; private set; }
        public bool Aboard { get; private set; }
        public bool Spoke { get; private set; }       // the talk on the water (this Crossing)
        public bool Departing => departAt >= 0f;
        public string SaveId => id;
        int visits; // (this sitting: which aside comes next)

        Vector3 basePosition;
        Quaternion baseRotation, turnTo, shown;
        float turnUntil = -1f, keptIntensity, farIntensity, flare;
        SoulFade figureFade, boatFade;
        Boat boat;
        Apprentice apprentice;
        float departAt = -1f, boatInAt = -1f;
        bool gifted;

        void Awake()
        {
            if (figure == null)
                figure = transform;
            basePosition = figure.position;
            baseRotation = shown = figure.rotation;
            if (kept != null)
                keptIntensity = kept.intensity;
            figureFade = Fade(figure.gameObject);
            if (boatFigure != null)
            {
                boatFade = Fade(boatFigure);
                boatFigure.SetActive(false);
            }
            if (farLight != null)
                farIntensity = farLight.TryGetComponent<LightFlicker>(out var f) ? f.BaseIntensity : farLight.intensity;
            if (leftBehind != null)
                leftBehind.SetActive(false);
            Show();
        }

        static SoulFade Fade(GameObject go) => go.TryGetComponent<SoulFade>(out var f) ? f : go.AddComponent<SoulFade>();

        void Start()
        {
            apprentice = FindAnyObjectByType<Apprentice>();
            boat = FindAnyObjectByType<Boat>();
            if (boat != null)
            {
                boat.Boarded += OnBoarded;
                boat.SteppedAshore += OnAshore;
                boat.WentHome += OnWentHome;
            }
        }

        void OnDestroy()
        {
            if (boat != null)
            {
                boat.Boarded -= OnBoarded;
                boat.SteppedAshore -= OnAshore;
                boat.WentHome -= OnWentHome;
            }
            if (Aboard)
                MusicPlayer.Hushed = false;
        }

        void OnEnable() => Interactables.Register(this);
        void OnDisable() => Interactables.Unregister(this);

        public Vector3 InteractPosition => basePosition;
        public float InteractRadius => interactRadius;
        public bool Available => State != Business.Crossed && !Aboard && !Drifting;
        public string PromptLabel => "Speak";
        public Vector3 FigurePosition => Aboard && boatFigure != null ? boatFigure.transform.position : figure.position;

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
                    Sfx.Instance?.LanternLit(basePosition + Vector3.up * 0.6f); // it's lit again, in her hands
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
            Sfx.Instance?.ObolClink(basePosition + Vector3.up);
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
            if (state >= Business.Paid)
                GoToWait();
            if (state == Business.Crossed)
                Gone();
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
            figure.SetPositionAndRotation(basePosition, baseRotation);
        }

        // Crossed: nothing of them left here but what they left on the Far shore.
        void Gone()
        {
            Aboard = false;
            figure.gameObject.SetActive(false);
            if (boatFigure != null)
                boatFigure.SetActive(false);
            if (leftBehind != null)
                leftBehind.SetActive(true);
        }

        // ---- going to wait by the boat: she thins away into wisps (her light with her), and gathers again there

        public const float DriftOut = 1.3f, DriftPause = 0.4f, DriftIn = 1.3f;
        public bool Drifting => driftStart >= 0f;
        float driftStart = -1f;
        bool driftMoved, driftIn;

        void StartDrift()
        {
            driftStart = Time.time;
            driftMoved = driftIn = false;
            figureFade.Out(DriftOut);
            Sfx.Instance?.SoulAbsorb(basePosition + Vector3.up * 0.6f);
        }

        void UpdateDrift()
        {
            float t = Time.time - driftStart;
            if (!driftMoved && t >= DriftOut)
            {
                driftMoved = true;
                GoToWait();
                Sfx.Instance?.SoulAbsorb(basePosition + Vector3.up * 0.6f);
            }
            if (!driftIn && t >= DriftOut + DriftPause)
            {
                driftIn = true;
                figureFade.In(DriftIn);
            }
            if (t >= DriftOut + DriftPause + DriftIn)
                driftStart = -1f;
        }

        // ---- the Crossing

        // The Apprentice takes the oars: with her fare paid, she comes aboard (thinning away on the landing, gathering
        // again in the bow).
        void OnBoarded()
        {
            if (State != Business.Paid || Aboard || boatFigure == null)
                return;
            if (Drifting)
            {
                driftStart = -1f;
                GoToWait();
                figureFade.Set(0f);
            }
            Aboard = true;
            Spoke = false;
            figureFade.Out(0.9f);
            boatFigure.SetActive(true);
            boatFade.Set(1f);
            boatInAt = Time.time + 0.5f;
            Sfx.Instance?.SoulAbsorb(basePosition + Vector3.up * 0.6f);
        }

        // Put in somewhere: at the Far shore, her goodbye; anywhere else she stays sat in the boat.
        void OnAshore(Dock dock)
        {
            if (!Aboard || !dock.farShore || Departing || State == Business.Crossed)
                return;
            MusicPlayer.Hushed = true;
            if (apprentice != null)
                apprentice.ShowMenu(new Conversation(displayName, FigurePosition + Vector3.up, farewell, null, Depart));
            else
                Depart();
        }

        // Back home without crossing (waking at a Mooring post): she's waiting on the landing again.
        void OnWentHome()
        {
            if (!Aboard || State == Business.Crossed)
                return;
            Aboard = false;
            boatInAt = -1f;
            boatFigure.SetActive(false);
            figure.gameObject.SetActive(true);
            figureFade.Set(0f);
            GoToWait();
            MusicPlayer.Hushed = false;
        }

        // Nearing the Far shore: the oar rests, the boat drifts in, and she talks.
        void TalkOnTheWater()
        {
            Spoke = true;
            var far = FarDock();
            boat.Coast(far);
            MusicPlayer.Hushed = true;
            apprentice.ShowMenu(new Conversation(displayName, FigurePosition + Vector3.up, crossing, null, () => boat.Coast(null)));
        }

        Dock FarDock()
        {
            if (boat == null)
                return null;
            foreach (var d in boat.docks)
                if (d != null && d.farShore)
                    return d;
            return null;
        }

        // She goes on: her parting gift, then she thins away from the boat, her light drifting ahead of her into the
        // Far shore's, which flares as she reaches it. What she carried stays behind on the jetty, still lit.
        void Depart()
        {
            State = Business.Crossed;
            departAt = Time.time;
            gifted = false;
            if (farLight != null)
                boatFade.StreamTo = farLight.transform.position;
            boatFade.Out(2.4f);
            Sfx.Instance?.SoulAbsorb(FigurePosition + Vector3.up * 0.6f);
        }

        void UpdateDeparture()
        {
            float t = Time.time - departAt;
            if (t >= 1.8f && flare == 0f && farLight != null)
            {
                flare = 1f; // she reaches it
                Sfx.Instance?.LanternLit(farLight.transform.position);
                if (leftBehind != null)
                    leftBehind.SetActive(true);
            }
            if (!gifted && t >= 2.6f)
            {
                gifted = true;
                var charm = Charm.ById(giftCharm);
                if (charm != null && apprentice != null)
                {
                    apprentice.Equipment.Own(charm);
                    apprentice.AnnounceFind(charm.Name, giftHint);
                }
            }
            if (t >= 3.2f)
            {
                departAt = -1f;
                MusicPlayer.Hushed = false;
                Gone();
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

        void Update()
        {
            if (Drifting)
                UpdateDrift();
            if (Aboard && boatInAt >= 0f && Time.time >= boatInAt)
            {
                boatInAt = -1f;
                boatFade.In(0.9f);
            }
            if (Aboard && !figureFade.Busy && figureFade.Amount >= 1f && figure.gameObject.activeSelf)
                figure.gameObject.SetActive(false); // (gone from the landing: nothing to bump into there)
            if (Aboard && !Spoke && State == Business.Paid && boat != null && boat.Rowing && apprentice != null && apprentice.OpenMenu == null
                && FarDock() is Dock far && Vector3.Distance(Flat(boat.transform.position), Flat(far.mooring.position)) < talkDistance)
                TalkOnTheWater();
            if (Departing)
                UpdateDeparture();
            if (farLight != null && flare > 0f)
            {
                flare = Mathf.MoveTowards(flare, 0.001f, Time.deltaTime * 0.4f); // (never quite 0: it flares only once)
                float intensity = farIntensity * (1f + 4f * flare);
                if (farLight.TryGetComponent<LightFlicker>(out var f))
                    f.BaseIntensity = intensity;
                else
                    farLight.intensity = intensity;
            }
            if (!figure.gameObject.activeInHierarchy)
                return;
            float time = Time.time;
            shown = Quaternion.Slerp(shown, time < turnUntil ? turnTo : baseRotation, Time.deltaTime * 2f);
            // A soul breathes a little: a slow bob and sway.
            figure.SetPositionAndRotation(basePosition + Vector3.up * (0.015f * Mathf.Sin(time * 0.9f)),
                shown * Quaternion.Euler(0.8f * Mathf.Sin(time * 0.6f), 0f, 1.2f * Mathf.Sin(time * 0.8f + 1f)));
        }

        static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);
    }
}
