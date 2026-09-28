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
            "Slick as the river. Dashing costs half the stamina.", 120);

        public static readonly Charm[] All = { EelSkin };
    }
}
