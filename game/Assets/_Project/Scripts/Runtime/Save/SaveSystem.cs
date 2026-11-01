using System.Collections;
using System.Linq;
using UnityEngine;

namespace MyGame
{
    // Keeps the slot being played (SaveSlots.Current) in step with the world, like a soulslike: no save button. On
    // loading the scene it puts the world back as the slot has it and wakes the Apprentice at the Mooring post they
    // last Rested at; then, whenever something that lasts has changed (checked every couple of seconds), on every Rest,
    // and on quitting, it writes the slot. Playing a scene straight from the editor (no slot) saves nothing.
    public sealed class SaveSystem : MonoBehaviour
    {
        public static SaveSystem Instance { get; private set; }
        public bool Active => SaveSlots.Current >= 0;
        public float PlayTime => playTime;
        public int Writes { get; private set; }

        Apprentice apprentice;
        float playTime, nextCheck;
        string area = "The Drowned City", lastWritten;
        bool ready;

        void Awake() => Instance = this;

        IEnumerator Start()
        {
            apprentice = FindAnyObjectByType<Apprentice>();
            AreaEntrance.Entered += OnArea;
            MooringPost.Rested += OnRested;
            if (!Active)
                yield break;
            yield return null; // (after everything's Start: the world is set up)
            var data = SaveSlots.Load(SaveSlots.Current);
            if (data != null)
                Apply(data);
            ready = true;
            SaveNow(); // (a new game: its slot is taken from the start)
        }

        void OnDestroy()
        {
            AreaEntrance.Entered -= OnArea;
            MooringPost.Rested -= OnRested;
            if (Instance == this)
                Instance = null; // (no save here: the world is half torn down; leaving saves first: PauseMenu)
        }

        void OnApplicationQuit() => SaveNow();

        void OnArea(string name) => area = name;
        void OnRested(MooringPost post, bool firstLight) => SaveNow();

        void Update()
        {
            if (!Active || !ready)
                return;
            if (!Pause.IsPaused)
                playTime += Time.unscaledDeltaTime;
            if (Time.unscaledTime < nextCheck)
                return;
            nextCheck = Time.unscaledTime + 2f;
            var data = Capture();
            if (Fingerprint(data) != lastWritten)
                Write(data);
        }

        public void SaveNow()
        {
            if (Active && ready && apprentice != null)
                Write(Capture());
        }

        void Write(SaveData data)
        {
            data.playTime = playTime;
            data.lastSaved = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm");
            SaveSlots.Write(SaveSlots.Current, data);
            lastWritten = Fingerprint(data);
            Writes++;
        }

        // What changed matters, not the clock: the save without its time stamps.
        static string Fingerprint(SaveData d)
        {
            float t = d.playTime;
            string s = d.lastSaved;
            d.playTime = 0f;
            d.lastSaved = "";
            string json = JsonUtility.ToJson(d);
            d.playTime = t;
            d.lastSaved = s;
            return json;
        }

        public SaveData Capture()
        {
            var d = new SaveData { area = area };
            var rested = WorldState.Instance != null ? WorldState.Instance.LastRested : null;
            d.restedAt = rested != null ? rested.SaveId : "";
            d.litPosts = MooringPost.All.Where(p => p.IsLit).Select(p => p.SaveId).ToArray();
            d.obols = apprentice.Obols;
            var shade = Shade.Current;
            if (shade != null && !shade.Claimed)
            {
                d.shade = true;
                d.shadePosition = shade.RestPosition;
                d.shadeRotation = shade.transform.rotation;
                d.shadeObols = shade.Obols;
            }
            d.charmsOwned = apprentice.Equipment.Owned.Select(c => c.Id).ToArray();
            d.charmsFitted = apprentice.Equipment.Fitted.Select(c => c.Id).ToArray();
            d.healingCharges = apprentice.maxHealingCharges;
            d.hasHook = apprentice.HasHook;
            d.hasStroke = apprentice.HasStroke;
            d.hasVault = apprentice.HasVault;
            d.hallTimeRunning = HallTime.Instance != null && HallTime.Instance.Running;
            d.oarStep = apprentice.Equipment.OarStep;
            d.ferryNails = apprentice.Equipment.FerryNails;
            d.warmStones = apprentice.Equipment.WarmStones;
            d.heldBreaths = apprentice.Equipment.HeldBreaths;
            (d.provisions, d.provisionCounts, d.belt, d.beltChosen) = apprentice.Equipment.SatchelForSave();
            (d.ledger, d.ledgerCounts) = apprentice.Equipment.LedgerForSave();
            d.taken = FindObjectsByType<ObolCache>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(c => c.Taken).Select(c => c.SaveId)
                .Concat(FindObjectsByType<ChargePiece>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(c => c.Taken).Select(c => c.SaveId))
                .Concat(FindObjectsByType<Keepsake>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(k => k.Taken).Select(k => k.SaveId))
                .Concat(FindObjectsByType<UpgradePiece>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(u => u.Taken).Select(u => u.SaveId))
                .Concat(FindObjectsByType<ProvisionFind>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(p => p.Taken).Select(p => p.SaveId)).ToArray();
            d.keepsakes = apprentice.Equipment.Keepsakes.ToArray();
            d.lingerers = FindObjectsByType<Lingerer>(FindObjectsSortMode.None).Where(l => l.State != Lingerer.Business.Unmet)
                .Select(l => $"{l.SaveId}={l.State}").ToArray();
            d.opened = FindObjectsByType<ShortcutGate>(FindObjectsSortMode.None).Where(g => g.IsOpen).Select(g => g.SaveId).ToArray();
            d.beaten = FindObjectsByType<Anchoress>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(b => b.TimesDefeated > 0).Select(b => b.SaveId).ToArray();
            return d;
        }

        // Puts the world back as the save has it, then wakes the Apprentice at their Mooring post.
        void Apply(SaveData d)
        {
            area = d.area;
            playTime = d.playTime;
            foreach (var post in MooringPost.All.ToArray())
                if (d.litPosts.Contains(post.SaveId))
                    post.RestoreLit();
            foreach (var cache in FindObjectsByType<ObolCache>(FindObjectsSortMode.None))
                if (d.taken.Contains(cache.SaveId))
                    cache.RestoreTaken();
            foreach (var piece in FindObjectsByType<ChargePiece>(FindObjectsSortMode.None))
                if (d.taken.Contains(piece.SaveId))
                    piece.RestoreTaken();
            foreach (var piece in FindObjectsByType<UpgradePiece>(FindObjectsSortMode.None))
                if (d.taken.Contains(piece.SaveId))
                    piece.RestoreTaken();
            foreach (var find in FindObjectsByType<ProvisionFind>(FindObjectsSortMode.None))
                if (d.taken.Contains(find.SaveId))
                    find.RestoreTaken();
            apprentice.Equipment.Restore(d.oarStep, d.ferryNails, d.warmStones, d.heldBreaths);
            apprentice.Equipment.RestoreSatchel(d.provisions, d.provisionCounts, d.belt, d.beltChosen);
            apprentice.Equipment.RestoreLedger(d.ledger, d.ledgerCounts);
            foreach (var keepsake in FindObjectsByType<Keepsake>(FindObjectsSortMode.None))
                if (d.taken.Contains(keepsake.SaveId))
                    keepsake.RestoreTaken();
            foreach (var id in d.keepsakes)
                apprentice.Equipment.Carry(id);
            foreach (var lingerer in FindObjectsByType<Lingerer>(FindObjectsSortMode.None))
                foreach (var entry in d.lingerers)
                    if (entry.StartsWith(lingerer.SaveId + "=") && System.Enum.TryParse(entry.Substring(lingerer.SaveId.Length + 1), out Lingerer.Business state))
                        lingerer.Restore(state);
            foreach (var gate in FindObjectsByType<ShortcutGate>(FindObjectsSortMode.None))
                if (d.opened.Contains(gate.SaveId))
                    gate.RestoreOpen();
            foreach (var boss in FindObjectsByType<Anchoress>(FindObjectsSortMode.None))
                if (d.beaten.Contains(boss.SaveId))
                    boss.RestoreBeaten(d.hasHook);
            foreach (var id in d.charmsOwned)
                if (Charm.ById(id) is Charm charm)
                    apprentice.Equipment.Own(charm);
            foreach (var id in d.charmsFitted)
                if (Charm.ById(id) is Charm charm && !apprentice.Equipment.IsFitted(charm))
                    apprentice.Equipment.Toggle(charm);
            apprentice.Restore(d.obols, d.healingCharges, d.hasHook, d.hasStroke, d.hasVault);
            if (d.hallTimeRunning)
            {
                if (CentralEngine.Instance != null)
                    CentralEngine.Instance.Settle(); // (going: the hands at the hour, every machine running)
                else
                    HallTime.Instance?.RunAtOnce();
            }
            if (d.shade)
            {
                var ghosts = apprentice.GetComponent<DashGhosts>();
                Shade.Restore(apprentice, d.shadeObols, d.shadePosition, d.shadeRotation, ghosts != null ? ghosts.ghostMaterial : null);
            }
            var rested = MooringPost.All.FirstOrDefault(p => p.SaveId == d.restedAt);
            if (rested != null)
            {
                WorldState.Instance?.RestoreRested(rested);
                apprentice.Teleport(rested.RespawnPoint);
            }
        }
    }
}
