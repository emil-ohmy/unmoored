namespace MyGame
{
    // A Provision: something carried in the Satchel and used up, from the belt (up to four on it: the d-pad or the arrow
    // keys, left/right to choose, up to use).
    // Bought from the merchant soul with Obols, or found; each kind has a carry cap, and Resting doesn't refill them
    // (docs/brief.md, Progression). What each one does lives in the Apprentice (UseProvision).
    public sealed class Provision
    {
        public readonly string Id, Name, Description, Effect;
        public readonly int Cap, Price;

        Provision(string id, string name, string description, string effect, int cap, int price)
        {
            Id = id;
            Name = name;
            Description = description;
            Effect = effect;
            Cap = cap;
            Price = price;
        }

        public static readonly Provision PitchPot = new Provision("pitch_pot", "Pitch Pot",
            "Pitch, a rag, a pot. The ferrymen's answer to most things.",
            "Thrown ahead (or at what you're locked on to): it bursts into burning pitch, and whatever stands in it burns. No guard stops fire.",
            5, 40);

        public static readonly Provision EmberTar = new Provision("ember_tar", "Ember Tar",
            "Smear it on, swing hard, try not to hold the wrong end.",
            "The oar burns for 30 seconds: your blows land harder, and set the Soured alight.",
            3, 60);

        public static readonly Provision WayBack = new Provision("way_back", "The Way Back",
            "A knotted cord. Whoever knotted it knew they wouldn't be the one to use it.",
            "Takes you back to the last Mooring post you Rested at. Not in a Boss's arena, and not on the water.",
            3, 80);

        public static readonly Provision WarmBroth = new Provision("warm_broth", "Warm Broth",
            "Broth, somehow still warm. It mends you slowly, which is more than most things down here will.",
            "Mends 30 health over 10 seconds. A blow doesn't stop it.",
            3, 50);

        public static readonly Provision[] All = { PitchPot, EmberTar, WayBack, WarmBroth };

        public static Provision ById(string id) => System.Array.Find(All, p => p.Id == id);

        public const int BeltSlots = 4; // (the most on the belt at once)
    }
}
