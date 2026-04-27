// AthenaInterventions.cs
// Ablegen in: Assets/Scripts/Gods/AthenaInterventions.cs
// Anhängen an: Singletons-GameObject (DontDestroyOnLoad)
//
// Athena = Strategie + Defensive. Diese Klasse deckt drei Bereiche ab:
//   - Passiv (≥ 50 Favor): Spieler regeneriert HP passiv (Schutzschild-Idee
//     vereinfacht als HP-Regen)
//   - Intervention 1 (25): Strategische Übersicht — Türme +25 % Reichweite
//     für 15 s, Spieler bekommt einmalig +30 HP
//   - Intervention 2 (75): Tempo-Schub — alle Türme doppelte Feuerrate
//     für 15 s (nutzt TurretBase.SetFireRateMultiplier-Hook, der für genau
//     diese Athena-Intervention bereits in AttackBuildings.cs vorhanden ist)
//
// Der Tempel-Effekt (+30 % Gebäude-HP, Türme Stufe-1-Upgrade) liegt bereits
// in TurretBase.ApplyEffects bzw. wird über IsTempleBuilt(Athena) abgefragt.
//
// Synergien:
//   - war_strategy (Athena + Ares): Während Berserker sind Gebäude
//     unzerstörbar — wird in AresInterventions/AresAvatar gehandhabt.
//   - smiths_wisdom (Athena + Hephaistos): Erz-Rabatt + Curse gratis —
//     liegt in HephaistosForge.

using UnityEngine;
using System.Collections;

public class AthenaInterventions : MonoBehaviour
{
    [Header("Passiv: HP-Regen")]
    [SerializeField] float passiveHpRegen      = 0.5f;   // HP/s wenn Passiv aktiv
    [SerializeField] float passiveTickInterval = 0.5f;

    [Header("Intervention 1: Strategische Übersicht")]
    [SerializeField] float towerRangeBonus     = 0.25f;  // +25 % Reichweite
    [SerializeField] float overviewDuration    = 15f;
    [SerializeField] float playerHealAmount    = 30f;

    [Header("Intervention 2: Tempo-Schub")]
    [SerializeField] float fireRateBoost       = 2f;     // 2× Feuerrate
    [SerializeField] float boostDuration       = 15f;

    bool overviewRunning = false;
    bool boostRunning    = false;

    // ── Unity Lifecycle ────────────────────────────────────────────────────
    void OnEnable()
    {
        FavorManager.OnThresholdReached += HandleThreshold;
        StartCoroutine(PassiveRegenLoop());
    }

    void OnDisable()
    {
        FavorManager.OnThresholdReached -= HandleThreshold;
    }

    // ── Passiv-Regen ───────────────────────────────────────────────────────
    IEnumerator PassiveRegenLoop()
    {
        var wait = new WaitForSeconds(passiveTickInterval);
        while (true)
        {
            if (FavorManager.Instance != null &&
                FavorManager.Instance.IsPassiveActive(FavorManager.God.Athena) &&
                PlayerState.Instance != null)
            {
                PlayerState.Instance.Heal(passiveHpRegen * passiveTickInterval);
            }
            yield return wait;
        }
    }

    // ── Threshold-Handler ──────────────────────────────────────────────────
    void HandleThreshold(FavorManager.God god, string key)
    {
        if (god != FavorManager.God.Athena) return;
        switch (key)
        {
            case "intervention_1": StartCoroutine(StrategischeUebersicht()); break;
            case "intervention_2": StartCoroutine(TempoSchub());             break;
        }
    }

    // ── I1: Strategische Übersicht ─────────────────────────────────────────
    IEnumerator StrategischeUebersicht()
    {
        if (overviewRunning) yield break;
        overviewRunning = true;

        // Sofortige Spieler-Heilung
        PlayerState.Instance?.Heal(playerHealAmount);

        // Alle Türme bekommen Reichweiten-Boost — TurretBase hat keinen
        // dedizierten Range-Multiplier, also setzen wir detectionRadius
        // direkt und stellen ihn nach Ablauf zurück.
        var turrets = FindObjectsOfType<TurretBase>();
        var snapshots = new (TurretBase t, float origRange)[turrets.Length];
        for (int i = 0; i < turrets.Length; i++)
        {
            snapshots[i] = (turrets[i], turrets[i].DetectionRadius);
            turrets[i].SetDetectionRadius(turrets[i].DetectionRadius * (1f + towerRangeBonus));
        }

        yield return new WaitForSeconds(overviewDuration);

        foreach (var s in snapshots)
            if (s.t != null) s.t.SetDetectionRadius(s.origRange);

        overviewRunning = false;
    }

    // ── I2: Tempo-Schub ────────────────────────────────────────────────────
    IEnumerator TempoSchub()
    {
        if (boostRunning) yield break;
        boostRunning = true;

        var turrets = FindObjectsOfType<TurretBase>();
        foreach (var t in turrets)
            t.SetFireRateMultiplier(fireRateBoost, boostDuration);

        yield return new WaitForSeconds(boostDuration);
        boostRunning = false;
    }
}
