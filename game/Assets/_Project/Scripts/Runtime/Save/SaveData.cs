using System;
using UnityEngine;

namespace MyGame
{
    // One save slot's progress. Only what lasts: where the Apprentice last Rested (they wake there on loading), the
    // Mooring posts lit, the Obols carried and the Shade left (where, with how many), Equipment, Healing charges,
    // Abilities, items taken, keepsakes carried, where each Lingerer's business stands, gates opened, Bosses beaten. Not enemies (they re-form on Rest), nor health (a Rest fills
    // it), nor where the Apprentice stands. World things are named by SaveKey.
    [Serializable]
    public sealed class SaveData
    {
        public int version = 1;
        public string area = "The Drowned City"; // for the slot's summary
        public float playTime;                      // seconds
        public string lastSaved;                    // when, as text (the slot's summary)

        public string restedAt = "";                // the Mooring post they wake at ("" : the start)
        public string[] litPosts = new string[0];
        public int obols;
        public bool shade;
        public Vector3 shadePosition;
        public Quaternion shadeRotation = Quaternion.identity;
        public int shadeObols;
        public string[] charmsOwned = new string[0];
        public string[] charmsFitted = new string[0];
        public int healingCharges = 3;              // the most they can carry
        public bool hasHook;
        public int oarStep;                         // the oar's step (0: the Apprentice's own)
        public int ferryNails;                      // carried, not yet spent
        public int warmStones, heldBreaths;         // found: more health, more stamina
        public string[] provisions = new string[0]; // the Satchel: each Provision carried (its id), and how many
        public int[] provisionCounts = new int[0];
        public string[] belt = new string[0];       // what's on each belt slot (up, right, down, left; "" empty)
        public string[] taken = new string[0];      // items picked up (Obol caches, Embers, upgrade pieces, Provisions found, keepsakes)
        public string[] keepsakes = new string[0];  // keepsakes carried (not yet given)
        public string[] lingerers = new string[0];  // each Lingerer's business: "id=State" (Waiting, Given, Paid)
        public string[] opened = new string[0];     // gates opened (shortcuts)
        public string[] beaten = new string[0];     // Bosses beaten
    }

    // A stable name for something in the world, for the save: its kind, its name and where it stands (to a decimetre).
    public static class SaveKey
    {
        public static string Of(Component c)
        {
            Vector3 p = c.transform.position;
            return $"{c.GetType().Name}:{c.gameObject.name}:{Mathf.RoundToInt(p.x * 10f)},{Mathf.RoundToInt(p.z * 10f)}";
        }
    }
}
