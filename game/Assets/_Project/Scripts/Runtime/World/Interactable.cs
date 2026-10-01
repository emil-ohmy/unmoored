using System.Collections.Generic;
using UnityEngine;

namespace MyGame
{
    // Something the Apprentice can use with Interact when close: a Mooring post (Rest), the merchant soul (Trade), a
    // shortcut gate (Open). The nearest one in reach shows its prompt; Interact uses it.
    public interface IInteractable
    {
        Vector3 InteractPosition { get; }
        float InteractRadius { get; }
        bool Available { get; }      // e.g. an opened gate no longer is
        string PromptLabel { get; }  // "Rest", "Trade", "Open", "Barred"
        void Interact(Apprentice apprentice);
    }

    public static class Interactables
    {
        // Only on its own level: never through a floor (a storey above or below is at least 2.4 m off).
        public const float LevelTolerance = 1.4f;

        static readonly List<IInteractable> all = new List<IInteractable>();

        public static void Register(IInteractable i)
        {
            if (!all.Contains(i))
                all.Add(i);
        }

        public static void Unregister(IInteractable i) => all.Remove(i);

        public static IInteractable Nearest(Vector3 position)
        {
            IInteractable best = null;
            float bestDistance = float.MaxValue;
            foreach (var i in all)
            {
                if (i == null || (i is Object o && o == null) || !i.Available)
                    continue;
                Vector3 to = i.InteractPosition - position;
                if (Mathf.Abs(to.y) > LevelTolerance)
                    continue;
                to.y = 0f;
                float d = to.magnitude;
                if (d <= i.InteractRadius && d < bestDistance)
                {
                    best = i;
                    bestDistance = d;
                }
            }
            return best;
        }
    }
}
