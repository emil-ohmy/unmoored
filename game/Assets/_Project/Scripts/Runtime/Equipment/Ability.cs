namespace MyGame
{
    // An Ability: a permanent new oar technique, with a use in a fight and a use for getting about (docs/brief.md).
    // Defined here for the character screen's Abilities page (what it is, how it's used, what it's for); what it does
    // lives where it takes effect (the Hook: Apprentice). More will come; the page lists however many are found.
    public sealed class Ability
    {
        public readonly string Id, Name, Description;
        public readonly string Keys, Pad;   // how it's used, on a keyboard and on a gamepad
        public readonly string[] Uses;      // what it's for
        readonly System.Func<Apprentice, bool> has;

        Ability(string id, string name, string description, string keys, string pad, string[] uses, System.Func<Apprentice, bool> has)
        {
            Id = id;
            Name = name;
            Description = description;
            Keys = keys;
            Pad = pad;
            Uses = uses;
            this.has = has;
        }

        public bool FoundBy(Apprentice apprentice) => apprentice != null && has(apprentice);

        public static readonly Ability Hook = new Ability("hook", "The Hook",
            "The Ferryman's own: a small iron grappling hook on a length of chain. What he couldn't reach, he fetched.",
            "Hold F to aim, let go to throw", "Hold RB to aim, let go to throw",
            new[]
            {
                "Pull yourself to a hook post, across water or a gap.",
                "Yank a Soured over to you, out of its guard.",
                "Tear the heap off a Laden's back.",
                "Whatever is too heavy to move is jerked off balance instead.",
                "While aiming, steer to choose what it catches.",
            },
            a => a.HasHook);

        public static readonly Ability Stroke = new Ability("stroke", "The Stroke",
            "The Drowned Moon: the harvest moon's reflection, which Father Moth lifted out of the river and kept in his ribs. Set into the blade, it pulls water as the moon pulls the tide.",
            "Hold K, let go", "Hold Y, let go",
            new[]
            {
                "A wave of force ahead of you: Soured knocked back and staggered.",
                "Throw off whatever clings to you, or leaps at you.",
                "Push a Soured into the water, and what floats across it.",
                "In shallows the wave carries water: bigger, and it puts fire out.",
                "Turn a wheel: a wave down a mill-race, a stopped gear.",
                "Rowing: a surge through a current, and a wave rolling ahead.",
            },
            a => a.HasStroke);

        public static readonly Ability Vault = new Ability("vault", "The Vault",
            "The Ferrule: an iron shoe for the foot of an oar, pitted and old, found where the drowned city kept its secrets. Fitted, the oar bites into stone and holds your weight.",
            "Shift: plant the oar and leap", "RT: plant the oar and leap",
            new[]
            {
                "Leap a gap (always the same distance: judge it well), a low wall, up onto a ledge.",
                "Over what's low: a ring across the floor, a sweep at your legs, a charge.",
                "Locked on, it carries you onto your target.",
            },
            a => a.HasVault);

        public static readonly Ability[] All = { Hook, Stroke, Vault };
    }
}
