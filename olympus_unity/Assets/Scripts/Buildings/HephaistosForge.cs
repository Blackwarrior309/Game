// HephaistosForge.cs
// Ablegen in: Assets/Scripts/Buildings/HephaistosForge.cs
// Die Schmiede — schaltet Schmiedemenü frei, kein Tempel-Slot
// Kosten: 120 Asche + 30 Erz

using UnityEngine;
using System.Collections.Generic;

public class HephaistosForge : BuildingBase
{
    // Singleton-Zugriff (max. 1 pro Run)
    public static HephaistosForge Instance { get; private set; }

    [Header("Forge")]
    [SerializeField] ParticleSystem forgeFireFX;
    [SerializeField] ParticleSystem hammerSparkFX;

    // ── Upgrade-Eigenschaften (zufälliger Pool) ────────────────────────────
    public enum WeaponProperty
    {
        Piercing,       // Durchdringen: Angriffe treffen alle Feinde in Linie
        FireEdge,       // Feuerkante: +15% Feuerschaden DoT
        ChainLightning, // Kettenblitz: 30% Chance Kettenreaktion
        LifeSteal,      // Lebensraub: 5% Schaden als HP zurück
        Shockwave,      // Erschütterung: Knockback
        Curse           // Fluch: Getroffene nehmen 10% mehr Schaden
    }

    // ── Waffen-Upgrade-Daten ───────────────────────────────────────────────
    public class WeaponUpgradeData
    {
        public string          WeaponId;
        public int             CurrentLevel;      // 1–3
        public List<WeaponProperty> Properties;

        public WeaponUpgradeData(string id)
        {
            WeaponId     = id;
            CurrentLevel = 1;
            Properties   = new List<WeaponProperty>();
        }
    }

    // ── Legendäre Waffen-Definitionen ─────────────────────────────────────
    public class LegendaryWeaponDef
    {
        public string           Id;
        public string           DisplayName;
        public string           BaseWeapon;
        public int              EreCost;
        public FavorManager.God RequiredGod;       // None = kein Tempel nötig
        public bool             RequiresTempel;
        public string           SpecialEffect;
        public bool             BonusActive;       // false wenn Tempel zerstört

        public LegendaryWeaponDef(string id, string name, string base_, int cost,
                                   FavorManager.God god, bool needsTemple, string effect)
        {
            Id = id; DisplayName = name; BaseWeapon = base_; EreCost = cost;
            RequiredGod = god; RequiresTempel = needsTemple; SpecialEffect = effect;
            BonusActive = true;
        }
    }

    public static readonly List<LegendaryWeaponDef> LegendaryWeapons = new()
    {
        new("keraunos",        "Keraunos (Donnerlanze)",      "spear",      80, FavorManager.God.Zeus,       true,  "zeus_lightning_on_hit"),
        new("flame_blade",     "Flammenklinge des Ares",      "shortsword", 80, FavorManager.God.Ares,       true,  "kill_explosion"),
        new("trident",         "Dreizack des Meeresgottes",   "throwing",   80, FavorManager.God.Poseidon,   true,  "return_on_throw"),
        new("soul_reaper",     "Seelenschnitter",             "hades_scythe",80,FavorManager.God.Hades,      true,  "instant_shadow_spawn"),
        new("aegis_spear",     "Ägis-Speer",                  "spear",      80, FavorManager.God.Athena,     true,  "dash_parry_reflect"),
        new("volcano_hammer",  "Vulkanhammer",                "new",        100,FavorManager.God.Zeus,       false, "aoe_lava_wave"),   // kein Tempel
        new("forge_bow",       "Götterschmiede-Bogen",        "bow",        100,FavorManager.God.Zeus,       false, "explosive_arrows"),// kein Tempel
    };

    // ── State ──────────────────────────────────────────────────────────────
    Dictionary<string, WeaponUpgradeData> upgradedWeapons = new();
    List<string>  craftedLegendaries = new();
    const int     MaxLegendaries     = 2;

    // ── Schmiede-Kosten-Modifikatoren (Synergien + Meta) ──────────────────
    float UpgradeCostModifier
    {
        get
        {
            float mod = 1f;
            // Schmied der Weisheit: -30% Erz-Kosten
            if (SynergySystem.Instance.IsActive("smiths_wisdom")) mod *= 0.70f;
            // Meta-Progression: Schmiede-Upgrade-Kosten -10%/20%
            // mod *= MetaProgression.Instance.ForgeCostMultiplier;
            return mod;
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    protected override void Awake()
    {
        buildingType = "forge";
        maxHp        = 160f;
        ashCost      = 120;
        oreCost      = 30;
        buildTime    = 5f;
        base.Awake();

        // Nur 1 Schmiede pro Run
        if (Instance != null)
        {
            Debug.LogWarning("HephaistosForge: Bereits eine Schmiede aktiv!");
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    protected override void ApplyEffects()
    {
        FavorManager.Instance.OnForgeBuilt();
        if (forgeFireFX != null) forgeFireFX.Play();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        FavorManager.Instance?.OnForgeDestroyed();

        // Alle legendären Waffen verlieren Bonus-Effekte
        LegendaryWeapons.ForEach(w => w.BonusActive = false);
    }

    // ══ WAFFEN UPGRADE ════════════════════════════════════════════════════

    /// <summary>Kostet für Stufe 1→2: 30 Erz, für 2→3: 60 Erz</summary>
    public int GetUpgradeCost(string weaponId)
    {
        int level = GetWeaponLevel(weaponId);
        int baseCost = level == 1 ? 30 : 60;
        return Mathf.RoundToInt(baseCost * UpgradeCostModifier);
    }

    public int GetWeaponLevel(string weaponId)
    {
        return upgradedWeapons.TryGetValue(weaponId, out var data) ? data.CurrentLevel : 1;
    }

    public bool CanUpgrade(string weaponId)
        => GetWeaponLevel(weaponId) < 3 && PlayerState.Instance.ore >= GetUpgradeCost(weaponId);

    public UpgradeResult UpgradeWeapon(string weaponId)
    {
        if (!CanUpgrade(weaponId))
            return new UpgradeResult { Success = false };

        int cost = GetUpgradeCost(weaponId);
        PlayerState.Instance.SpendOre(cost);

        if (!upgradedWeapons.ContainsKey(weaponId))
            upgradedWeapons[weaponId] = new WeaponUpgradeData(weaponId);

        var data = upgradedWeapons[weaponId];
        data.CurrentLevel++;

        // Zufällige Eigenschaft wählen
        WeaponProperty newProp = GetRandomProperty(data.Properties);
        data.Properties.Add(newProp);

        // Schmied der Weisheit: Fluch gratis
        if (SynergySystem.Instance.IsActive("smiths_wisdom") && !data.Properties.Contains(WeaponProperty.Curse))
            data.Properties.Add(WeaponProperty.Curse);

        // Hammer-Funken-Effekt
        if (hammerSparkFX != null) hammerSparkFX.Play();

        return new UpgradeResult
        {
            Success      = true,
            NewLevel     = data.CurrentLevel,
            NewProperty  = newProp,
            DamageBonus  = data.CurrentLevel == 2 ? 0.20f : 0.40f
        };
    }

    WeaponProperty GetRandomProperty(List<WeaponProperty> existing)
    {
        var all = System.Enum.GetValues(typeof(WeaponProperty));
        var available = new List<WeaponProperty>();
        foreach (WeaponProperty p in all)
            if (!existing.Contains(p)) available.Add(p);

        if (available.Count == 0) return WeaponProperty.Piercing;
        return available[Random.Range(0, available.Count)];
    }

    public class UpgradeResult
    {
        public bool           Success;
        public int            NewLevel;
        public WeaponProperty NewProperty;
        public float          DamageBonus;
    }

    // ══ LEGENDÄRE WAFFEN HERSTELLEN ═══════════════════════════════════════

    public bool CanCraftLegendary(LegendaryWeaponDef def)
    {
        if (craftedLegendaries.Count >= MaxLegendaries)          return false;
        if (craftedLegendaries.Contains(def.Id))                 return false;
        if (PlayerState.Instance.ore < def.EreCost)              return false;
        if (!FavorManager.Instance.IsPassiveActive(FavorManager.God.Hephaistos)) return false;

        // Tempel-Anforderung
        if (def.RequiresTempel && !FavorManager.Instance.IsTempleBuilt(def.RequiredGod))
        {
            // Synergie-Ausnahmen
            if (def.Id == "keraunos" && SynergySystem.Instance.IsActive("sky_fire"))
                return true;  // Himmelsfeuer: Keraunos ohne Zeus-Tempel
            if (def.Id == "flame_blade" && SynergySystem.Instance.IsActive("divine_weapon"))
                return true;  // Göttliche Waffe: Flammenklinge ohne Tempel
            return false;
        }
        return true;
    }

    public CraftResult CraftLegendary(LegendaryWeaponDef def)
    {
        if (!CanCraftLegendary(def))
            return new CraftResult { Success = false };

        PlayerState.Instance.SpendOre(def.EreCost);
        craftedLegendaries.Add(def.Id);
        FavorManager.Instance.OnLegendaryCrafted();

        if (hammerSparkFX != null) hammerSparkFX.Play();

        return new CraftResult { Success = true, WeaponDef = def };
    }

    public class CraftResult
    {
        public bool              Success;
        public LegendaryWeaponDef WeaponDef;
    }

    // ── Getter für UI ──────────────────────────────────────────────────────
    public int  CraftedLegendaryCount => craftedLegendaries.Count;
    public bool CanCraftMore          => craftedLegendaries.Count < MaxLegendaries;

    public List<WeaponProperty> GetWeaponProperties(string weaponId)
        => upgradedWeapons.TryGetValue(weaponId, out var d) ? d.Properties : new List<WeaponProperty>();
}
