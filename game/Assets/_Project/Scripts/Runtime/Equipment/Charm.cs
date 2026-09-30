namespace MyGame
{
    // A Charm: passive Equipment that changes the Apprentice without changing the Oar. Defined in code; what each
    // one does lives where it takes effect (e.g. the Apprentice's dash cost checks for the Eel Skin).
    public sealed class Charm
    {
        public readonly string Id, Name, Description;
        public readonly int Price;

        Charm(string id, string name, string description, int price)
        {
            Id = id;
            Name = name;
            Description = description;
            Price = price;
        }

        public static readonly Charm EelSkin = new Charm("eel_skin", "Eel Skin",
            "Slick as the river. Dashing costs half the stamina. Smells about the same.", 120);

        // For play-testing only (owned from the start in development, never sold or found): every blow lands four
        // times as hard.
        public static readonly Charm TestersKnot = new Charm("testers_knot", "Tester's Knot (dev)",
            "For play-testing. Your blows land four times as hard (300% more damage).", 0);

        // Old Hesper's parting gift (after her Crossing): the bow lantern's light reaches further.
        public static readonly Charm HespersWick = new Charm("hespers_wick", "Hesper's Wick",
            "A wick from a lamp that was never once let go out. Your lantern's light reaches further into the dark. What it's for, nobody's said yet.", 0);

        public static readonly Charm[] All = { EelSkin, TestersKnot, HespersWick };

        public static Charm ById(string id) => System.Array.Find(All, c => c.Id == id);
    }
}
