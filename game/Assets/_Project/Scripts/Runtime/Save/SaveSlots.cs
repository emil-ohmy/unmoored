using System.IO;
using UnityEngine;

namespace MyGame
{
    // The three save slots, as JSON files in the player's data folder. Current is the slot being played (-1: none,
    // e.g. playing a scene straight from the editor, when nothing is saved).
    public static class SaveSlots
    {
        public const int Count = 3;
        public static int Current { get; set; } = -1;
        static string folder;
        public static string Folder // (tests point it elsewhere)
        {
            get => folder ??= Application.persistentDataPath;
            set => folder = value;
        }

        static string Path(int slot) => System.IO.Path.Combine(Folder, $"save_{slot + 1}.json");

        public static bool Exists(int slot) => File.Exists(Path(slot));

        public static SaveData Load(int slot)
        {
            if (!Exists(slot))
                return null;
            try
            {
                return JsonUtility.FromJson<SaveData>(File.ReadAllText(Path(slot)));
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[Save] slot {slot + 1} unreadable: {e.Message}");
                return null;
            }
        }

        public static void Write(int slot, SaveData data)
        {
            Directory.CreateDirectory(Folder);
            string path = Path(slot), temp = path + ".tmp";
            File.WriteAllText(temp, JsonUtility.ToJson(data, true)); // written aside first: a crash mid-write can't eat the save
            if (File.Exists(path))
                File.Delete(path);
            File.Move(temp, path);
        }

        public static void Delete(int slot)
        {
            if (Exists(slot))
                File.Delete(Path(slot));
        }

        // The slot played most recently (for Continue), or -1.
        public static int MostRecent()
        {
            int best = -1;
            System.DateTime latest = System.DateTime.MinValue;
            for (int i = 0; i < Count; i++)
                if (Exists(i))
                {
                    var t = File.GetLastWriteTimeUtc(Path(i));
                    if (t > latest)
                    {
                        latest = t;
                        best = i;
                    }
                }
            return best;
        }

        public static string PlayTime(float seconds)
        {
            int m = Mathf.FloorToInt(seconds / 60f);
            return m >= 60 ? $"{m / 60}h {m % 60:00}m" : $"{m}m";
        }
    }
}
