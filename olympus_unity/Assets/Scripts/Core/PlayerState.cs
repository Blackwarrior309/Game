// PlayerState.cs
// Singleton — Zentraler Spieler-Zustand
// Ablegen in: Assets/Scripts/Core/PlayerState.cs
// Verwendung: PlayerState.Instance.hp

using UnityEngine;
using System;

public class PlayerState : MonoBehaviour
{
    public static PlayerState Instance { get; private set; }

    // ── Basis-Attribute ────────────────────────────────────────────────────
    public float hp           = 100f;
    public float maxHp        = 100f;
    public float moveSpeed    = 5f;
    public float attackSpeed  = 1f;    // Multiplikator
    public float damage       = 10f;
    public float armor        = 0f;
    public float pickupRadius = 3f;

    // ── Ressourcen ─────────────────────────────────────────────────────────
    public int ash = 0;
    public int ore = 0;

    // ── XP / Level ─────────────────────────────────────────────────────────
    public int   level          = 1;
    public float xp             = 0f;
    public float xpToNextLevel  = 100f;
    public float xpMultiplier   = 1f;   // Goldenes Vlies Artefakt

    // ── Building ───────────────────────────────────────────────────────────
    public int  activeTemples = 0;
    public const int MaxTemples = 3;
    public bool hasForge      = false;

    // ── Events ─────────────────────────────────────────────────────────────
    public static event Action<float, float> OnHpChanged;     // current, max
    public static event Action<int>          OnAshChanged;
    public static event Action<int>          OnOreChanged;
    public static event Action<float, float> OnXpChanged;     // current, required
    public static event Action<int>          OnLevelUp;
    public static event Action               OnPlayerDied;

    // ── Unity Lifecycle ────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ── HP ─────────────────────────────────────────────────────────────────
    public void TakeDamage(float amount)
    {
        float reduced = Mathf.Max(0f, amount - armor);
        hp = Mathf.Max(0f, hp - reduced);
        OnHpChanged?.Invoke(hp, maxHp);
        if (hp <= 0f) OnDeath();
    }

    public void Heal(float amount)
    {
        hp = Mathf.Min(maxHp, hp + amount);
        OnHpChanged?.Invoke(hp, maxHp);
    }

    void OnDeath()
    {
        OnPlayerDied?.Invoke();
        FavorManager.Instance.OnPlayerDeath();
        // Respawn mit 25 % HP — Position setzt GameManager
        hp = maxHp * 0.25f;
        OnHpChanged?.Invoke(hp, maxHp);
    }

    // ── Ressourcen ─────────────────────────────────────────────────────────
    public void AddAsh(int amount) { ash += amount; OnAshChanged?.Invoke(ash); }
    public bool SpendAsh(int amount)
    {
        if (ash < amount) return false;
        ash -= amount; OnAshChanged?.Invoke(ash); return true;
    }

    public void AddOre(int amount) { ore += amount; OnOreChanged?.Invoke(ore); }
    public bool SpendOre(int amount)
    {
        if (ore < amount) return false;
        ore -= amount; OnOreChanged?.Invoke(ore); return true;
    }

    // ── XP / Level ─────────────────────────────────────────────────────────
    public void AddXP(float amount)
    {
        xp += amount * xpMultiplier;
        OnXpChanged?.Invoke(xp, xpToNextLevel);
        while (xp >= xpToNextLevel) { xp -= xpToNextLevel; LevelUp(); }
    }

    void LevelUp()
    {
        level++;
        xpToNextLevel = 100f * Mathf.Pow(1.2f, level - 1);
        OnLevelUp?.Invoke(level);
    }

    // ── Reset (neuer Run) ──────────────────────────────────────────────────
    public void Reset()
    {
        hp = maxHp = 100f; moveSpeed = 5f; attackSpeed = 1f;
        damage = 10f; armor = 0f; pickupRadius = 3f;
        ash = ore = 0; level = 1; xp = 0f; xpToNextLevel = 100f;
        xpMultiplier = 1f; activeTemples = 0; hasForge = false;
        OnHpChanged?.Invoke(hp, maxHp);
        OnAshChanged?.Invoke(ash);
        OnOreChanged?.Invoke(ore);
    }
}
