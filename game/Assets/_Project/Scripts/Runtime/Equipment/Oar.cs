namespace MyGame
{
    // The Apprentice's oar: one oar, made stronger step by step with Ferry Nails at a Mooring post (in order: no step
    // skipped, however many nails are found first). Each step has its own look, name and description (the game's own
    // voice: dry, now and then). docs/brief.md, Progression.
    public sealed class Oar
    {
        public readonly string Name, Description, FoundLine;
        public readonly float Strength; // its blows' damage and stagger, times this
        public readonly int Cost;       // Ferry Nails to strengthen it to this step (from the one before)

        Oar(string name, string description, string foundLine, float strength, int cost)
        {
            Name = name;
            Description = description;
            FoundLine = foundLine;
            Strength = strength;
            Cost = cost;
        }

        public static readonly Oar[] Steps =
        {
            new Oar("The Apprentice's Oar",
                "An oar, worn smooth where your hands go. It floats, at least. Not everything down here does.", "", 1f, 0),
            new Oar("The Oarhand's Oar",
                "Tarred, and bound with twine. A little heavier, a little surer. It no longer looks borrowed.", "It hits harder.", 1.2f, 1),
            new Oar("The Boatman's Oar",
                "Shod in iron at the blade. It hits harder, and the Soured have noticed.", "Iron-shod: it hits harder.", 1.45f, 2),
            new Oar("The Steersman's Oar",
                "Carved with a mark for every crossing it has made. Most of them are yours. Some of them aren't.", "It hits harder still.", 1.7f, 3),
            new Oar("The Ferryman's Oar",
                "His. Pale, cold to hold, and it knows the way across. You've earned it, which is more than he did.", "His, now. It will grow no stronger.", 2f, 4),
        };

        public const int Last = 4;
    }
}
