// LevelUpSystem.cs
// Ablegen in: Assets/Scripts/Core/LevelUpSystem.cs
// UpgradeData-Assets im Inspector in "upgradePool" ziehen.
//
// Evolutions-System (P8-06): Wird ein Upgrade dreimal im selben Run gewählt
// (z.B. eine Waffen-Verstärkung), schaltet sich die zugehörige Evolution
// frei und erscheint ab dem nächsten Levelup im Pool. Mapping wird per
// Inspector über `evolutionMap` gepflegt; die Evolution-UpgradeData-Assets
// kommen separat in `evolutionUpgrades`, damit sie nicht standardmäßig
// im Pool auftauchen.

using UnityEngine;
using System.Collections.Generic;

public class LevelUpSystem : MonoBehaviour
{
    public static LevelUpSystem Instance { get; private set; }

    [Header("Upgrade Pool")]
    [SerializeField] List<UpgradeData> upgradePool = new();

    [Header("Evolutionen (P8-06)")]
    [SerializeField] List<EvolutionEntry> evolutionMap     = new();
    [SerializeField] List<UpgradeData>    evolutionUpgrades = new();

    [System.Serializable]
    public class EvolutionEntry
    {
        public string SourceId;        // z.B. "weapon_spear_dmg"
        public string EvolutionId;     // z.B. "weapon_spear_evolution"
        public int    RequiredPicks = 3;
    }

    int offersCount = 3;  // Delphi-Orakel setzt auf 4

    Dictionary<string, int> pickCounts        = new();
    HashSet<string>         unlockedEvolutions = new();
    HashSet<string>         consumedEvolutions = new();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void OnEnable()  => PlayerState.OnLevelUp += OnLevelUp;
    void OnDisable() => PlayerState.OnLevelUp -= OnLevelUp;

    void OnLevelUp(int _)
    {
        var choices = GetRandomChoices();
        GameEvents.RaiseShowLevelUpChoices(choices);
    }

    // ── Pool-Auswahl mit Evolutions-Insertion ──────────────────────────────
    List<UpgradeData> GetRandomChoices()
    {
        var pool = new List<UpgradeData>(upgradePool);

        // Frisch freigeschaltete (noch nicht konsumierte) Evolutionen
        // erscheinen GARANTIERT in den Optionen — sie ersetzen ein
        // zufälliges Standard-Element, damit die Anzahl gleich bleibt.
        var fresh = new List<UpgradeData>();
        foreach (var evId in unlockedEvolutions)
        {
            if (consumedEvolutions.Contains(evId)) continue;
            var data = FindEvolution(evId);
            if (data != null) fresh.Add(data);
        }

        // Fisher-Yates Shuffle über den Standard-Pool
        for (int i = pool.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (pool[i], pool[j]) = (pool[j], pool[i]);
        }

        int count = Mathf.Min(offersCount, pool.Count);
        var chosen = pool.GetRange(0, count);

        // Frische Evolutionen vorne einfügen (verdrängen das letzte Standard-Element)
        for (int i = 0; i < fresh.Count && chosen.Count > 0; i++)
            chosen[chosen.Count - 1 - i] = fresh[i];

        return chosen;
    }

    UpgradeData FindEvolution(string id)
    {
        foreach (var e in evolutionUpgrades)
            if (e != null && e.upgradeId == id) return e;
        return null;
    }

    bool IsEvolutionId(string id)
    {
        foreach (var e in evolutionMap)
            if (e.EvolutionId == id) return true;
        return false;
    }

    // ── Apply ──────────────────────────────────────────────────────────────
    public void ApplyUpgrade(string upgradeId)
    {
        var ps = PlayerState.Instance;

        // ArtifactManager trackt jeden Pick (auch direkt-mutierende), damit
        // HUD/Save/Synergien einen einheitlichen "welche Artefakte aktiv?"-
        // Zustand abfragen können.
        ArtifactManager.Instance?.PickArtifact(upgradeId);

        // Pick-Count pro Upgrade-Id für das Evolutions-System
        pickCounts.TryGetValue(upgradeId, out int prev);
        pickCounts[upgradeId] = prev + 1;

        if (IsEvolutionId(upgradeId))
        {
            // Evolution selbst gewählt → konsumiert, nicht erneut anbieten
            consumedEvolutions.Add(upgradeId);
            unlockedEvolutions.Remove(upgradeId);
        }

        // Schwelle prüfen — schaltet ggf. die Evolution für die nächste Auswahl frei
        foreach (var entry in evolutionMap)
        {
            if (entry == null || string.IsNullOrEmpty(entry.SourceId)) continue;
            if (!pickCounts.TryGetValue(entry.SourceId, out int pc)) continue;
            if (pc < entry.RequiredPicks) continue;
            if (consumedEvolutions.Contains(entry.EvolutionId)) continue;
            unlockedEvolutions.Add(entry.EvolutionId);
        }

        switch (upgradeId)
        {
            case "artifact_hp":
                ps.maxHp += 30f; ps.Heal(30f); break;
            case "artifact_speed":
                ps.moveSpeed *= 1.2f; break;
            case "artifact_armor":
                ps.armor += 2f; break;
            case "artifact_xp":
                ps.xpMultiplier *= 1.25f; break;
            case "artifact_damage":
                ps.damage *= 1.1f; break;
            case "artifact_oracle":
                offersCount = 4; break;        // Delphi-Orakel
            case "artifact_prometheus":
                // Türme fragen ArtifactManager.GetTurretDamageMultiplier()
                // direkt ab — kein State hier nötig.
                break;
            case "artifact_anvil":
                // HephaistosForge fragt ArtifactManager.GetSmithyPropertyMultiplier()
                break;
            case "artifact_shard":
                // ApplySlow-Pfade fragen ArtifactManager.GetSlowResistance()
                break;
            default:
                Debug.Log("LevelUpSystem: Unbekanntes Upgrade: " + upgradeId);
                break;
        }
    }

    public void SetOffersCount(int count) => offersCount = count;

    // ── Reset (neuer Run) ──────────────────────────────────────────────────
    public void Reset()
    {
        offersCount = 3;
        pickCounts.Clear();
        unlockedEvolutions.Clear();
        consumedEvolutions.Clear();
    }

    // ── Debug-Getter ───────────────────────────────────────────────────────
    public int GetPickCount(string id) => pickCounts.TryGetValue(id, out int n) ? n : 0;
    public bool HasUnlockedEvolution(string evolutionId) => unlockedEvolutions.Contains(evolutionId);
}
