// SynergySystem.cs
// Singleton — Götter-Synergien (alle 10)
// Ablegen in: Assets/Scripts/Core/SynergySystem.cs

using UnityEngine;
using System;
using System.Collections.Generic;
using static FavorManager;

public class SynergySystem : MonoBehaviour
{
    public static SynergySystem Instance { get; private set; }

    // ── Synergie-Daten ─────────────────────────────────────────────────────
    public class Synergy
    {
        public string Id;
        public string DisplayName;
        public God    GodA;
        public God    GodB;
        public bool   Active;

        public Synergy(string id, string name, God a, God b)
        { Id = id; DisplayName = name; GodA = a; GodB = b; }
    }

    public List<Synergy> Synergies { get; private set; } = new();

    public static event Action<string, string> OnSynergyActivated;    // id, displayName
    public static event Action<string>         OnSynergyDeactivated;

    // ── Unity Lifecycle ────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        BuildSynergyTable();
    }

    void BuildSynergyTable()
    {
        Synergies = new List<Synergy>
        {
            new("storm_flood",      "Gewitterflut",         God.Zeus,       God.Poseidon),
            new("wargod_wrath",     "Kriegsgott-Zorn",      God.Zeus,       God.Ares),
            new("underworld_storm", "Unterwelt-Sturm",      God.Zeus,       God.Hades),
            new("war_strategy",     "Kriegs-Strategie",     God.Ares,       God.Athena),
            new("flood_of_souls",   "Flut der Seelen",      God.Poseidon,   God.Hades),
            new("lava_sea",         "Lavameer",              God.Hephaistos, God.Poseidon),
            new("divine_weapon",    "Göttliche Waffe",       God.Hephaistos, God.Ares),
            new("smiths_wisdom",    "Schmied der Weisheit",  God.Hephaistos, God.Athena),
            new("soul_forge",       "Seelenschmiede",        God.Hephaistos, God.Hades),
            new("sky_fire",         "Himmelsfeuer",          God.Hephaistos, God.Zeus),
        };
    }

    // ── Kern-Check (event-getrieben) ───────────────────────────────────────
    public void CheckSynergies()
    {
        var activeGods = GetActiveGods();

        foreach (var syn in Synergies)
        {
            bool shouldBeActive = activeGods.Contains(syn.GodA) && activeGods.Contains(syn.GodB);

            if (shouldBeActive && !syn.Active)
            {
                syn.Active = true;
                OnSynergyActivated?.Invoke(syn.Id, syn.DisplayName);
            }
            else if (!shouldBeActive && syn.Active)
            {
                syn.Active = false;
                OnSynergyDeactivated?.Invoke(syn.Id);
            }
        }
    }

    HashSet<God> GetActiveGods()
    {
        var result = new HashSet<God>();
        foreach (var kvp in FavorManager.Instance.Gods)
        {
            var g = kvp.Value;
            if (g.favor >= 50f || g.templeBuilt || g.forgeBuilt)
                result.Add(kvp.Key);
        }
        return result;
    }

    // ── Abfrage-Helfer ─────────────────────────────────────────────────────
    public bool IsActive(string id)
    {
        foreach (var syn in Synergies)
            if (syn.Id == id) return syn.Active;
        return false;
    }

    public List<Synergy> GetActiveSynergies()
    {
        var result = new List<Synergy>();
        foreach (var syn in Synergies)
            if (syn.Active) result.Add(syn);
        return result;
    }

    // ── Reset ──────────────────────────────────────────────────────────────
    public void Reset()
    {
        foreach (var syn in Synergies) syn.Active = false;
    }
}
