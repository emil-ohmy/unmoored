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

        public string KindLine => (Kind == Rank.Soured ? "Soured" : Kind == Rank.Miniboss ? "Miniboss" : "Boss") + "  ·  " + Area;

        const string DrownedCity = "The Drowned City", Orchard = "The Orchard of Last Autumn";

        public static readonly LedgerEntry Laden = new LedgerEntry("laden", "The Laden", Rank.Soured, DrownedCity, "laden",
            "Couldn't let go of their things. Everything they owned rides on their back in a heap, and they lift it as a shield when you come close.",
            "Get round it: the heap guards only its front, and the slam leaves it open a long while. The Hook tears the heap away.");

        public static readonly LedgerEntry LadenArcher = new LedgerEntry("laden_archer", "The Laden archer", Rank.Soured, DrownedCity, "laden_archer",
            "A Laden that let most of its heap go, and kept a bow. It keeps its distance and shoots: one arrow, or three.",
            "Rush it: it has no guard, and less to it. A sidestep or a dash slips the arrows.");

        public static readonly LedgerEntry Clinger = new LedgerEntry("clinger", "The Clinger", Rank.Soured, DrownedCity, "clinger",
            "Couldn't let go of a person. They come in twos, circling, and leap to cling to your back, holding on as they once held on to someone.",
            "Dash to throw one off, even out of breath. Scatter one and the other grows frantic.");

        public static readonly LedgerEntry Drowned = new LedgerEntry("drowned", "The Drowned", Rank.Soured, DrownedCity, "drowned",
            "Couldn't let go of life. They wait under the water, holding a breath they'll never let out, and rise to spit poison.",
            "A perfect block turns the spit back, and its own spit is the end of it. Hide, and it comes ashore looking for you. The Hook hauls it out.");

        public static readonly LedgerEntry Unparted = new LedgerEntry("unparted", "The Unparted, of the Last Embrace", Rank.Miniboss, DrownedCity, "unparted",
            "Two souls who never let each other go, grown into one great hunched thing: Hold, who carries, and Fast, who rides its back.",
            "Scatter Fast first and Hold goes berserk. Scatter Hold first and Fast weeps where it fell.");

        public static readonly LedgerEntry Anchoress = new LedgerEntry("anchoress", "The Antediluvian Anchoress, of Fleeting Memories", Rank.Boss, DrownedCity, "anchoress",
            "A Laden who would not let the Ferryman go. She chained his ferry's anchor to herself, fastened with his stolen Hook, and keeps vigil in the drowned chapel.",
            "Her anchor reaches further than it looks. Beaten, she gave up the Hook.");

        public static readonly LedgerEntry Moths = new LedgerEntry("moths", "The Moths", Rank.Soured, Orchard, "moth",
            "Couldn't let go of a memory. Pale and dusty, they circle a light, and come to yours. When one goes, the memory goes with it: a note, as from a music box.",
            "One at a time, each stalls, flaring, before it dives. A heavy swing goes through several; fire takes the whole flock.");

        public static readonly LedgerEntry Watchman = new LedgerEntry("watchman", "Abner Wick, Who Merits Flame", Rank.Miniboss, Orchard, "watchman",
            "The man who worked the orchard, a scarecrow among scarecrows in the Unreaped Rows, still keeping watch over a harvest that never came.",
            "Straw: the oar goes through him. Turn his own fire back at him.");

        public static readonly LedgerEntry[] All = { Laden, LadenArcher, Clinger, Drowned, Unparted, Anchoress, Moths, Watchman };

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
            _ => null,
        };

        // The entry a beaten Boss is written in as.
        public static LedgerEntry For(IBoss boss) => boss switch
        {
            MyGame.Anchoress _ => Anchoress,
            MyGame.Unparted _ => Unparted,
            MyGame.Watchman _ => Watchman,
            _ => null,
        };
    }
}
