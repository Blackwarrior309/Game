// FavorManager.cs
// Singleton — Götter-Gunst-System
// Ablegen in: Assets/Scripts/Core/FavorManager.cs

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class FavorManager : MonoBehaviour
{
    public static FavorManager Instance { get; private set; }

    // ── Gott-IDs ───────────────────────────────────────────────────────────
    public enum God { Zeus, Athena, Ares, Poseidon, Hades, Hephaistos }

    public static readonly string[] GodNames =
        { "Zeus", "Athena", "Ares", "Poseidon", "Hades", "Hephaistos" };

    // ── Datenklasse ────────────────────────────────────────────────────────
    [System.Serializable]
    public class GodFavor
    {
        public God   godId;
        public float favor          = 0f;   // 0–100
        public bool  templeBuilt    = false;
        public bool  forgeBuilt     = false;
        public int   templeLevel    = 0;
        public bool  passiveActive  = false;
        public bool  avatarActive   = false;
        public float avatarTimer    = 0f;
        public float favorRegenRate = 0f;   // per Sekunde (Tempel: +2/min ≈ 0.0333/s)

        public GodFavor(God id) { godId = id; }
    }

    // ── Konstanten ─────────────────────────────────────────────────────────
    public const float AvatarDuration = 30f;

    // ── State ──────────────────────────────────────────────────────────────
    public Dictionary<God, GodFavor> Gods { get; private set; } = new();
    public God MainGod = God.Zeus;

    // ── Events ─────────────────────────────────────────────────────────────
    public static event Action<God, float>   OnFavorChanged;
    public static event Action<God>          OnPassiveActivated;
    public static event Action<God>          OnPassiveDeactivated;
    public static event Action<God, string>  OnThresholdReached;  // "intervention_1" etc.
    public static event Action<God>          OnAvatarStarted;
    public static event Action<God>          OnAvatarEnded;

    // ── Favor-Event-Werte ──────────────────────────────────────────────────
    const float FavorEnemyKill       = 1f;
    const float FavorSacrifice       = 10f;
    const float FavorTempleBuild     = 25f;
    const float FavorForgeBuild      = 15f;
    const float FavorLegendaryCraft  = 15f;
    const float FavorBossKill        = 20f;
    const float FavorPyrosDamaged    = -5f;
    const float FavorPlayerDeath     = -10f;

    // ── Unity Lifecycle ────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        InitializeGods();
    }

    void InitializeGods()
    {
        Gods.Clear();
        foreach (God g in Enum.GetValues(typeof(God)))
            Gods[g] = new GodFavor(g);
    }

    void Update()
    {
        foreach (var kvp in Gods)
        {
            var g = kvp.Value;
            if (g.avatarActive)
            {
                g.avatarTimer -= Time.deltaTime;
                if (g.avatarTimer <= 0f) EndAvatar(kvp.Key);
            }
            if (g.favorRegenRate > 0f && !g.avatarActive)
                ModifyFavor(kvp.Key, g.favorRegenRate * Time.deltaTime);
        }
    }

    // ── Favor ändern ───────────────────────────────────────────────────────
    void ModifyFavor(God god, float amount)
    {
        var g = Gods[god];
        float old = g.favor;
        g.favor = Mathf.Clamp(g.favor + amount, 0f, 100f);
        OnFavorChanged?.Invoke(god, g.favor);
        CheckThresholds(god, old, g.favor);
    }

    void CheckThresholds(God god, float oldVal, float newVal)
    {
        var g = Gods[god];

        // Passive: ≥ 50
        if (oldVal < 50f && newVal >= 50f)
        {
            g.passiveActive = true;
            OnPassiveActivated?.Invoke(god);
            OnThresholdReached?.Invoke(god, "passive");
            SynergySystem.Instance.CheckSynergies();
        }
        else if (oldVal >= 50f && newVal < 50f)
        {
            g.passiveActive = false;
            OnPassiveDeactivated?.Invoke(god);
            SynergySystem.Instance.CheckSynergies();
        }

        // Interventions-Schwellen
        CheckThreshold(god, oldVal, newVal, 25f,  "intervention_1");
        CheckThreshold(god, oldVal, newVal, 75f,  "intervention_2");
        CheckThreshold(god, oldVal, newVal, 100f, "avatar");
    }

    void CheckThreshold(God god, float oldVal, float newVal, float threshold, string key)
    {
        if (oldVal < threshold && newVal >= threshold)
            OnThresholdReached?.Invoke(god, key);
    }

    // ── Öffentliche Event-Methoden ─────────────────────────────────────────
    public void OnEnemyKill(God? specificGod = null)
    {
        God target = specificGod ?? GetWeightedRandomGod();
        ModifyFavor(target, FavorEnemyKill);
    }

    public bool OnSacrifice(God god, int ashCost)
    {
        if (!PlayerState.Instance.SpendAsh(ashCost)) return false;
        ModifyFavor(god, FavorSacrifice);
        return true;
    }

    public void OnTempleBuilt(God god)
    {
        var g = Gods[god];
        g.templeBuilt    = true;
        g.templeLevel    = 1;
        g.favorRegenRate = TempleRegenForLevel(1);
        ModifyFavor(god, FavorTempleBuild);
        SynergySystem.Instance.CheckSynergies();
    }

    // ── Tempel-Upgrade (P3-12) ─────────────────────────────────────────────
    // Level 1 → 2 → 3. Auto-Effekte (Zeus-Blitz / Hades-Schatten / …)
    // skalieren über Temple.Level; Favor-Regen wird hier zentral gesetzt:
    //   L1: 2/min · L2: 3.5/min · L3: 5/min
    public bool UpgradeTemple(God god)
    {
        var g = Gods[god];
        if (!g.templeBuilt || g.templeLevel >= 3) return false;
        g.templeLevel++;
        g.favorRegenRate = TempleRegenForLevel(g.templeLevel);
        return true;
    }

    public int GetTempleLevel(God god) => Gods[god].templeLevel;

    static float TempleRegenForLevel(int level)
    {
        switch (level)
        {
            case 1:  return 2f   / 60f;
            case 2:  return 3.5f / 60f;
            case 3:  return 5f   / 60f;
            default: return 0f;
        }
    }

    public void OnTempleDestroyed(God god)
    {
        var g = Gods[god];
        g.templeBuilt    = false;
        g.templeLevel    = 0;
        g.favorRegenRate = 0f;
        g.favor          = 0f;
        if (g.avatarActive) EndAvatar(god);
        OnFavorChanged?.Invoke(god, 0f);
        SynergySystem.Instance.CheckSynergies();
        PlayerState.Instance.activeTemples--;
    }

    public void OnForgeBuilt()
    {
        Gods[God.Hephaistos].forgeBuilt = true;
        PlayerState.Instance.hasForge   = true;
        ModifyFavor(God.Hephaistos, FavorForgeBuild);
        SynergySystem.Instance.CheckSynergies();
    }

    public void OnForgeDestroyed()
    {
        Gods[God.Hephaistos].forgeBuilt = false;
        PlayerState.Instance.hasForge   = false;
        SynergySystem.Instance.CheckSynergies();
    }

    public void OnLegendaryCrafted()   => ModifyFavor(God.Hephaistos, FavorLegendaryCraft);
    public void OnBossKill()           { foreach (God g in Enum.GetValues(typeof(God))) ModifyFavor(g, FavorBossKill); }
    public void OnPyrosDamaged()       { foreach (God g in Enum.GetValues(typeof(God))) ModifyFavor(g, FavorPyrosDamaged); }
    public void OnPlayerDeath()        { foreach (God g in Enum.GetValues(typeof(God))) ModifyFavor(g, FavorPlayerDeath); }

    // ── Avatar ─────────────────────────────────────────────────────────────
    public bool TryActivateAvatar(God god)
    {
        if (god == God.Hephaistos) return false;
        var g = Gods[god];
        if (g.favor < 100f || g.avatarActive) return false;

        g.avatarActive = true;
        g.avatarTimer  = AvatarDuration;
        g.favor        = 0f;
        OnFavorChanged?.Invoke(god, 0f);
        OnAvatarStarted?.Invoke(god);
        return true;
    }

    void EndAvatar(God god)
    {
        Gods[god].avatarActive = false;
        Gods[god].avatarTimer  = 0f;
        OnAvatarEnded?.Invoke(god);
    }

    // ── Getter ─────────────────────────────────────────────────────────────
    public float GetFavor(God god)       => Gods[god].favor;
    public bool  IsPassiveActive(God god) => Gods[god].passiveActive;
    public bool  IsTempleBuilt(God god)   => Gods[god].templeBuilt;

    God GetWeightedRandomGod()
    {
        var pool = new List<God>();
        foreach (God g in Enum.GetValues(typeof(God)))
        {
            pool.Add(g);
            if (Gods[g].templeBuilt || Gods[g].forgeBuilt) pool.Add(g); // Doppelgewicht
        }
        return pool[UnityEngine.Random.Range(0, pool.Count)];
    }

    // ── Reset ──────────────────────────────────────────────────────────────
    public void Reset() => InitializeGods();
}
