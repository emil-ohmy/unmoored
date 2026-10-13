namespace MyGame
{
    // An entry in the Ledger (the character screen's): a kind of Soured, a miniboss or a Boss, written in the first
    // time the Apprentice scatters one (a Boss: beats it). What it is, where it's met, what it couldn't let go of, and
    // what's known of how to deal with it. In the order they're met on the way, Area by Area.
    public sealed class LedgerEntry
    {
        public enum Rank { Soured, Miniboss, Boss }

        public readonly string Id, Name, Area, Description, Known;
        public readonly Rank Kind;
        public readonly string Model; // its model, for the page (Characters/<Model>)

        LedgerEntry(string id, string name, Rank kind, string area, string model, string description, string known)
        {
            Id = id;
            Name = name;
            Kind = kind;
            Area = area;
            Model = model;
            Description = description;
            Known = known;
        }

        public bool Standing => this != Moths && this != FatherMoth; // (on the page: a figure on its feet, facing you; a Moth's seen from above)

        public string KindLine => (Kind == Rank.Soured ? "Soured" : Kind == Rank.Miniboss ? "Miniboss" : "Boss") + "  ·  " + Area;

        const string DrownedCity = "The Drowned City", Orchard = "The Orchard of Last Autumn", LastHour = "The Hall of the Last Hour";

        public static readonly LedgerEntry Laden = new LedgerEntry("laden", "The Laden", Rank.Soured, DrownedCity, "laden",
            "Each carries all it ever owned, roped in a heap on its back. Come close and it lifts the heap like a shield: it would sooner be crushed under it than set it down.",
            "Get round it: the heap guards only its front, and the slam leaves it open a long while. The Hook tears the heap away.");

        public static readonly LedgerEntry LadenArcher = new LedgerEntry("laden_archer", "The Laden archer", Rank.Soured, DrownedCity, "laden_archer",
            "A Laden that let most of its heap go, and kept a bow. It keeps its distance and shoots: one arrow, or three.",
            "Rush it: it has no guard, and less to it. A sidestep or a dash slips the arrows.");

        public static readonly LedgerEntry Clinger = new LedgerEntry("clinger", "The Clinger", Rank.Soured, DrownedCity, "clinger",
            "They come in twos, circling, and leap onto your back to hold on, the way they once held on to someone. It was never you they wanted.",
            "Dash to throw one off, even out of breath. Scatter one and the other grows frantic.");

        public static readonly LedgerEntry Drowned = new LedgerEntry("drowned", "The Drowned", Rank.Soured, DrownedCity, "drowned",
            "They wait under the water, holding the breath they took as it closed over them, and rise to spit poison at whoever comes too near.",
            "A perfect block turns the spit back, and its own spit is the end of it. Hide, and it comes ashore looking for you. The Hook hauls it out.");

        public static readonly LedgerEntry Unparted = new LedgerEntry("unparted", "The Unparted, of the Last Embrace", Rank.Miniboss, DrownedCity, "unparted",
            "Two souls so long together that they grew into one great hunched thing: Hold, who carries, and Fast, who rides its back.",
            "Scatter Fast first and Hold goes berserk. Scatter Hold first and Fast weeps where it fell.");

        public static readonly LedgerEntry Anchoress = new LedgerEntry("anchoress", "The Antediluvian Anchoress, of Fleeting Memories", Rank.Boss, DrownedCity, "anchoress",
            "She chained the Ferryman's anchor to herself, fastened with his stolen Hook, so his ferry could never leave her. She keeps vigil in the drowned chapel still.",
            "Her anchor reaches further than it looks. Beaten, she gave up the Hook.");

        public static readonly LedgerEntry Moths = new LedgerEntry("moths", "The Moths", Rank.Soured, Orchard, "moth",
            "Pale and dusty, they circle a light, and come to yours. Each carries a memory; when one goes, the memory goes with it: a note, as from a music box.",
            "One at a time, each stalls, flaring, before it dives. A heavy swing goes through several; fire takes the whole flock.");

        public static readonly LedgerEntry Gleaners = new LedgerEntry("gleaners", "The Gleaners", Rank.Soured, Orchard, "gleaner",
            "After the harvest failed they went on gleaning, walking the rows at night with lanterns on long poles, picking the field over for grain that was never there.",
            "Hard to catch: it skips back from the oar, but not while it winds up to throw. Turn its fire back with a perfect block and it goes up at once. The Hook brings it close.");

        public static readonly LedgerEntry Threshers = new LedgerEntry("threshers", "The Threshers", Rank.Soured, Orchard, "thresher",
            "The orchard's old threshing machines, and the men who drove them, still in the seat, grown into the levers. They run the rows as if there were still a harvest to bring in.",
            "It charges straight and barely turns: step aside, and let it run into something. From the front the oar glances off; reach the driver from behind. Fire only stokes it. The Hook wrenches its chain off.");

        public static readonly LedgerEntry Watchman = new LedgerEntry("watchman", "Abner Wick, Who Merits Flame", Rank.Miniboss, Orchard, "watchman",
            "The man who worked the orchard, a scarecrow among scarecrows in the Unreaped Rows, still keeping watch over a harvest that never came.",
            "Straw: the oar goes through him. Turn his own fire back at him.");

        public static readonly LedgerEntry FatherMoth = new LedgerEntry("father_moth", "Father Moth, Who Gathered Autumn", Rank.Boss, Orchard, "father_moth",
            "The man who tended the orchard and its glasshouse. He gathered every memory of the last autumn, and they gathered to him, as Moths to a light, until he was the greatest of them.",
            "On the wing the oar can't reach him: dodge his leaves, block them, step between them, until his wings give out. Spent, he lies on the floor a while: that's the time. Fire burns him badly.");

        public static readonly LedgerEntry Punctual = new LedgerEntry("punctual", "The Punctual", Rank.Soured, LastHour, "punctual",
            "The household of the Hall, still keeping its hours. Where the heart was, a watch has grown; it does nothing that isn't on the beat.",
            "Listen: every blow lands on the tick. Block one right on the beat and it loses count, and stands lost until it finds the bar again.");

        public static readonly LedgerEntry Wound = new LedgerEntry("wound", "The Wound", Rank.Soured, LastHour, "wound",
            "The household's hounds. They wanted more time, and were given it a turn of a key at a time: they run as they did when they were young, run down, and wind themselves again.",
            "Outlast it: it slows as it runs down, and sits to wind its key. That's the time. The Stroke knocks the key loose.");

        public static readonly LedgerEntry[] All = { Laden, LadenArcher, Clinger, Drowned, Unparted, Anchoress, Moths, Gleaners, Threshers, Watchman, FatherMoth, Punctual, Wound };

        public static LedgerEntry ById(string id) => System.Array.Find(All, e => e.Id == id);

        // The entry a scattered Soured is written in as (none: it's part of a Boss, written in when the Boss is beaten;
        // or it has no soul, a scarecrow).
        public static LedgerEntry For(Soured s) => s switch
        {
            IBoss _ => null,
            MyGame.UnpartedHold _ => null,
            MyGame.Clinger c when c.mount != null => null, // (Fast, on Hold's back: the Unparted)
            MyGame.Scarecrow _ => null,
            MyGame.LadenArcher _ => LadenArcher,
            MyGame.Laden _ => Laden,
            MyGame.Clinger _ => Clinger,
            MyGame.Drowned _ => Drowned,
            MyGame.Moth _ => Moths,
            MyGame.Gleaner _ => Gleaners,
            MyGame.Thresher _ => Threshers,
            MyGame.Punctual _ => Punctual,
            MyGame.Wound _ => Wound,
            _ => null,
        };

        // The entry a beaten Boss is written in as.
        public static LedgerEntry For(IBoss boss) => boss switch
        {
            MyGame.Anchoress _ => Anchoress,
            MyGame.Unparted _ => Unparted,
            MyGame.Watchman _ => Watchman,
            MyGame.FatherMoth _ => FatherMoth,
            _ => null,
        };
    }
}
