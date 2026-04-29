// ArtifactManager.cs
// Ablegen in: Assets/Scripts/Core/ArtifactManager.cs
// Anhängen an: Singletons-GameObject (DontDestroyOnLoad)
//
// Zentrale Verwaltung der 9 Artefakte (P8-04). Manche Artefakte (Lebenskraut,
// Hermes-Sandalen, Aigis-Schild, Goldenes Vlies, Mars-Klinge, Delphi-Orakel)
// mutieren PlayerState direkt — die Pick-Logik dafür bleibt in
// LevelUpSystem.ApplyUpgrade. Andere (Prometheus-Feuer, Hephaistos-Amboss,
// Kronos-Scherbe) brauchen einen Live-Query-Hook, weil sie laufende Systeme
// modifizieren (Turm-Schaden, Schmiede-Effekt-Boost, Zeit-Slow-Resistenz).
// Genau dafür ist dieser Manager da.
//
// Verbraucher (TurretBase.Fire, GiantPrecursor-Aura, …) fragen ihren Multiplier
// hier ab. Der LevelUpSystem.ApplyUpgrade-Switch ruft `PickArtifact(id)` für
// jedes Artefakt — auch jene, die direkt PlayerState mutieren — damit der
// Manager den vollen Pick-State trackt (z.B. für die HUD-Artefakt-Leiste).

using UnityEngine;
using System;
using System.Collections.Generic;

public class ArtifactManager : MonoBehaviour
{
    public static ArtifactManager Instance { get; private set; }

    // ── Artefakt-Katalog ───────────────────────────────────────────────────
    [Serializable]
    public class ArtifactDef
    {
        public string Id;
        public string DisplayName;
        public string Description;
        public string IconEmoji;

        public ArtifactDef(string id, string name, string desc, string icon)
        { Id = id; DisplayName = name; Description = desc; IconEmoji = icon; }
    }

    public static readonly List<ArtifactDef> AllArtifacts = new()
    {
        new("artifact_hp",         "Lebenskraut",       "+30 maximale HP",                            "💚"),
        new("artifact_speed",      "Hermes-Sandalen",   "+20 % Bewegungsgeschwindigkeit",             "👟"),
        new("artifact_armor",      "Aigis-Schild",      "+2 Rüstung",                                  "🛡"),
        new("artifact_xp",         "Goldenes Vlies",    "+25 % XP-Sammelrate",                         "🐑"),
        new("artifact_damage",     "Mars-Klinge",       "+10 % Schaden",                              "⚔"),
        new("artifact_oracle",     "Delphi-Orakel",     "4 Optionen pro Levelup statt 3",             "🏛"),
        new("artifact_prometheus", "Prometheus-Feuer",  "Türme schlagen mit +20 % Schaden zu",        "🔥"),
        new("artifact_anvil",      "Hephaistos-Amboss", "Schmiede-Upgrade-Eigenschaften +10 % Effekt", "⚒"),
        new("artifact_shard",      "Kronos-Scherbe",    "Zeit-Slow-Effekte wirken nur halb so stark", "⏳"),
    };

    // ── State ──────────────────────────────────────────────────────────────
    HashSet<string> picked = new();

    public static event Action<string> OnArtifactPicked;

    // ── Unity Lifecycle ────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ── Pick / Query ───────────────────────────────────────────────────────
    public void PickArtifact(string id)
    {
        if (string.IsNullOrEmpty(id) || !id.StartsWith("artifact_")) return;
        if (picked.Add(id)) OnArtifactPicked?.Invoke(id);
    }

    public bool HasArtifact(string id) => picked.Contains(id);

    public IReadOnlyCollection<string> Picked => picked;

    public static ArtifactDef Find(string id)
    {
        foreach (var a in AllArtifacts) if (a.Id == id) return a;
        return null;
    }

    // ── Multiplier-API für Live-Verbraucher ────────────────────────────────
    // Türme (TurretBase.Fire) greifen hier ab.
    public float GetTurretDamageMultiplier()
    {
        return HasArtifact("artifact_prometheus") ? 1.20f : 1f;
    }

    // HephaistosForge.UpgradeWeapon kann das einrechnen, wenn Eigenschaften
    // vergeben werden (z.B. FireEdge +10 % stärker).
    public float GetSmithyPropertyMultiplier()
    {
        return HasArtifact("artifact_anvil") ? 1.10f : 1f;
    }

    // GiantPrecursor-Aura, Kronos Phase 1 Slow-Aura, ApplySlow-Pfade können
    // diesen Faktor einrechnen — Slow-Effekt = 1 - (1 - factor) * resistance.
    // Halbiert die Wirksamkeit eingehender Slows.
    public float GetSlowResistance()
    {
        return HasArtifact("artifact_shard") ? 0.5f : 1f;
    }

    // ── Reset (neuer Run) ──────────────────────────────────────────────────
    public void Reset() => picked.Clear();
}
