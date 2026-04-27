// WeaponManager.cs
// Ablegen in: Assets/Scripts/Core/WeaponManager.cs
// Anhängen an: Singletons-GameObject (DontDestroyOnLoad)
//
// Zentraler Verwalter für ausgerüstete Waffen + Basis-Waffen-Pool.
// Aktuell ist die Waffenlogik in PlayerController.DoAttack inlined und
// nutzt PlayerState.damage direkt. Dieser Manager ist die Foundation für:
//   - P8-05  Alle 7 Basis-Waffen (Auto-Ziel, Reichweite, Spezialeffekt)
//   - P8-06  Evolutions-Upgrade-System (Waffe 3× im LevelUp → Evolution)
//   - HephaistosForge.LegendaryWeapons-Bonus-Anwendung (BaseWeapon-Match)
//
// Damit der Refactor klein bleibt, ändert dieser Commit PlayerController
// noch NICHT — Manager + Daten-Pool stehen, der Player liest weiterhin
// PlayerState.damage. Folgender Schritt: PlayerController.HandleAutoAttack
// auf WeaponManager.Instance.GetCurrentDamage() / GetCurrentFireRate() /
// GetCurrentRange() umstellen.

using UnityEngine;
using System;
using System.Collections.Generic;

public class WeaponManager : MonoBehaviour
{
    public static WeaponManager Instance { get; private set; }

    // ── Datentypen ─────────────────────────────────────────────────────────
    public enum WeaponKind
    {
        Shortsword,   // Kurzschwert (Standard, Nahkampf, schnell)
        Spear,        // Speer (Reichweite, Stoß)
        Throwing,     // Wurfklinge (Distanz, kehrt zurück)
        Bow,          // Bogen (Distanz, Multi-Pfeil)
        Scythe,       // Hades-Sense (AoE-Schwung)
        Hammer,       // Kriegshammer (langsam, Schock-AoE)
        Fist          // Faustkampf (sehr schnell, geringer Schaden)
    }

    [Serializable]
    public class WeaponData
    {
        public string     Id;
        public string     DisplayName;
        public WeaponKind Kind;
        public float      BaseDamage;
        public float      FireRate;     // Schüsse / Schwünge pro Sekunde
        public float      Range;        // Auto-Ziel-Reichweite
        public string     IconEmoji;    // Platzhalter bis Sprites da sind

        public WeaponData(string id, string name, WeaponKind k,
                          float dmg, float rate, float range, string icon)
        {
            Id = id; DisplayName = name; Kind = k;
            BaseDamage = dmg; FireRate = rate; Range = range; IconEmoji = icon;
        }
    }

    // ── Pool: 7 Basis-Waffen (P8-05) ───────────────────────────────────────
    public List<WeaponData> AllWeapons { get; private set; } = new();

    void BuildWeaponPool()
    {
        AllWeapons = new List<WeaponData>
        {
            new("shortsword", "Kurzschwert",     WeaponKind.Shortsword, 10f, 1.0f, 3.0f,  "🗡"),
            new("spear",      "Speer",            WeaponKind.Spear,      14f, 0.8f, 4.5f,  "🔱"),
            new("throwing",   "Wurfklinge",       WeaponKind.Throwing,   8f,  1.4f, 8.0f,  "🪃"),
            new("bow",        "Bogen",            WeaponKind.Bow,        9f,  1.2f, 12.0f, "🏹"),
            new("hades_scythe","Hades-Sense",     WeaponKind.Scythe,     16f, 0.6f, 3.5f,  "⚰"),
            new("hammer",     "Kriegshammer",     WeaponKind.Hammer,     22f, 0.5f, 3.0f,  "🔨"),
            new("fist",       "Faustkampf",       WeaponKind.Fist,       5f,  2.0f, 2.0f,  "👊"),
        };
    }

    // ── Equipped State ─────────────────────────────────────────────────────
    public WeaponData Equipped { get; private set; }
    public int        EquippedLevel { get; private set; } = 1;     // 1..3 (HephaistosForge)
    public List<HephaistosForge.WeaponProperty> EquippedProperties { get; private set; } = new();
    public List<string> ActiveLegendaries { get; private set; } = new();   // Ids aus HephaistosForge.LegendaryWeapons

    public static event Action<WeaponData> OnWeaponEquipped;

    // ── Unity Lifecycle ────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        BuildWeaponPool();
        Equip("shortsword");   // Default-Waffe für jeden Run
    }

    // ── Ausrüsten ──────────────────────────────────────────────────────────
    public void Equip(string weaponId)
    {
        var w = FindWeapon(weaponId);
        if (w == null)
        {
            Debug.LogWarning($"WeaponManager: Waffe '{weaponId}' nicht gefunden");
            return;
        }
        Equipped           = w;
        EquippedLevel      = 1;
        EquippedProperties = new List<HephaistosForge.WeaponProperty>();
        OnWeaponEquipped?.Invoke(w);
    }

    public WeaponData FindWeapon(string id)
    {
        foreach (var w in AllWeapons) if (w.Id == id) return w;
        return null;
    }

    // ── Schmiede-Hooks ─────────────────────────────────────────────────────
    // Nach erfolgreichem HephaistosForge.UpgradeWeapon: Level + Eigenschaft
    // hier reinmelden, damit GetCurrentDamage stimmt.
    public void ApplySmithyUpgrade(int newLevel, HephaistosForge.WeaponProperty addedProp)
    {
        EquippedLevel = Mathf.Clamp(newLevel, 1, 3);
        if (!EquippedProperties.Contains(addedProp)) EquippedProperties.Add(addedProp);
    }

    // Nach HephaistosForge.CraftLegendary: Id der gefertigten Legendären
    // hier registrieren — Modifikatoren werden in GetCurrentDamage etc. addiert.
    public void RegisterLegendary(string legendaryId)
    {
        if (!ActiveLegendaries.Contains(legendaryId)) ActiveLegendaries.Add(legendaryId);
    }

    public void UnregisterLegendary(string legendaryId)
    {
        ActiveLegendaries.Remove(legendaryId);
    }

    // ── Live-Werte (PlayerController liest hier) ───────────────────────────
    // Endformel: BaseDamage × Level-Bonus × PlayerState.damageMultiplier ×
    //            ArtifactManager-Multiplier (falls/wenn vorhanden)
    public float GetCurrentDamage()
    {
        if (Equipped == null) return PlayerState.Instance != null ? PlayerState.Instance.damage : 10f;

        float dmg = Equipped.BaseDamage * LevelDamageMultiplier(EquippedLevel);
        if (PlayerState.Instance != null) dmg *= PlayerState.Instance.damageMultiplier;
        return dmg;
    }

    public float GetCurrentFireRate()
    {
        float rate = Equipped != null ? Equipped.FireRate : 1f;
        if (PlayerState.Instance != null) rate *= PlayerState.Instance.attackSpeed;
        return rate;
    }

    public float GetCurrentRange() => Equipped != null ? Equipped.Range : 3f;

    // L1: ×1.0, L2: ×1.2, L3: ×1.4 (entspricht HephaistosForge.UpgradeResult.DamageBonus)
    static float LevelDamageMultiplier(int level)
    {
        switch (level)
        {
            case 2:  return 1.20f;
            case 3:  return 1.40f;
            default: return 1.00f;
        }
    }

    // ── Reset (neuer Run) ──────────────────────────────────────────────────
    public void Reset()
    {
        EquippedProperties.Clear();
        ActiveLegendaries.Clear();
        Equip("shortsword");
    }
}
