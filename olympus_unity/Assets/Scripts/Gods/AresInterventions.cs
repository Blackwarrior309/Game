// AresInterventions.cs
// Ablegen in: Assets/Scripts/Gods/AresInterventions.cs
// Anhängen an: Singletons-GameObject (DontDestroyOnLoad)
//
// Ares = Krieg, Kills, Berserker. Diese Klasse deckt drei Bereiche ab:
//   - Passiv (≥ 50): Kill-Streak — jeder Kill stackt einen kurzlebigen
//     Schadens-Bonus auf PlayerState.damageMultiplier; Stacks zerfallen
//     wenn länger nicht getötet wird. Cap bei 10 Stacks (+50 % Schaden).
//   - Intervention 1 (25): Kriegsschrei — Feinde im 15-m-Radius werden
//     für 1.5 s gestunnt UND für 5 s auf den Spieler aggro'd
//     (EnemyBase.ForcedTarget — Hook ist bereits da).
//   - Intervention 2 (75): Berserker — 15 s × 1.5 Schaden / Speed /
//     Attack-Speed plus Lifesteal (5 HP pro Kill).
//
// Synergien:
//   - wargod_wrath  (Ares + Zeus):   wird in PlayerController & ZeusInterventions
//     als Schaden-/Slow-Multiplier gehandhabt.
//   - war_strategy  (Ares + Athena): während Berserker sind Gebäude
//     unzerstörbar — wird hier durch Override von BuildingBase.TakeDamage
//     unmöglich (würde mehrere Klassen ändern). Stattdessen heilen wir
//     alle Gebäude im Tick auf, was effektiv das gleiche ist.
//   - divine_weapon (Ares + Hephaistos): Streak-Boost — wir verdoppeln
//     bei aktiver Synergie den per-Stack-Bonus.

using UnityEngine;
using System.Collections;

public class AresInterventions : MonoBehaviour
{
    [Header("Passiv: Kill-Streak")]
    [SerializeField] float perStackBonus     = 0.05f;   // +5 % Damage je Stack
    [SerializeField] int   maxStacks         = 10;
    [SerializeField] float stackDecayTime    = 3f;      // s ohne Kill → 1 Stack ab

    [Header("Intervention 1: Kriegsschrei")]
    [SerializeField] float warCryRadius      = 15f;
    [SerializeField] float warCryStunTime    = 1.5f;
    [SerializeField] float warCryAggroTime   = 5f;

    [Header("Intervention 2: Berserker")]
    [SerializeField] float berserkerDuration = 15f;
    [SerializeField] float berserkerDamage   = 1.5f;
    [SerializeField] float berserkerSpeed    = 1.5f;
    [SerializeField] float berserkerAtkSpeed = 1.5f;
    [SerializeField] float lifestealPerKill  = 5f;
    [SerializeField] float warStrategyHealPerSec = 5f;  // Synergie war_strategy

    int   killStacks       = 0;
    float aresContribution = 1f;
    float lastKillTime     = 0f;
    bool  berserkerActive  = false;

    // ── Unity Lifecycle ────────────────────────────────────────────────────
    void OnEnable()
    {
        FavorManager.OnThresholdReached    += HandleThreshold;
        FavorManager.OnPassiveDeactivated  += HandlePassiveDeactivated;
        GameEvents.OnEnemyKilled           += HandleEnemyKilled;
    }

    void OnDisable()
    {
        FavorManager.OnThresholdReached    -= HandleThreshold;
        FavorManager.OnPassiveDeactivated  -= HandlePassiveDeactivated;
        GameEvents.OnEnemyKilled           -= HandleEnemyKilled;
        ResetStacks();
    }

    // ── Passiv: Stack-Verfall ─────────────────────────────────────────────
    void Update()
    {
        if (killStacks > 0 && Time.time - lastKillTime > stackDecayTime)
        {
            killStacks = Mathf.Max(0, killStacks - 1);
            lastKillTime = Time.time;
            ApplyStackMultiplier();
        }
    }

    // ── Passiv: Kill-Stack ────────────────────────────────────────────────
    void HandleEnemyKilled(GameObject _, Vector3 __)
    {
        if (FavorManager.Instance == null) return;

        // Berserker-Lifesteal
        if (berserkerActive && PlayerState.Instance != null)
            PlayerState.Instance.Heal(lifestealPerKill);

        // Passiv-Stack
        if (!FavorManager.Instance.IsPassiveActive(FavorManager.God.Ares)) return;

        float stackMult = SynergySystem.Instance != null &&
                          SynergySystem.Instance.IsActive("divine_weapon") ? 2f : 1f;
        // Bei aktivem divine_weapon kommen 2 Stacks pro Kill
        killStacks = Mathf.Min(maxStacks, killStacks + (int)stackMult);
        lastKillTime = Time.time;
        ApplyStackMultiplier();
    }

    void HandlePassiveDeactivated(FavorManager.God god)
    {
        if (god == FavorManager.God.Ares) ResetStacks();
    }

    void ResetStacks()
    {
        if (PlayerState.Instance != null)
            PlayerState.Instance.damageMultiplier /= aresContribution;
        aresContribution = 1f;
        killStacks = 0;
    }

    void ApplyStackMultiplier()
    {
        var ps = PlayerState.Instance;
        if (ps == null) return;
        ps.damageMultiplier /= aresContribution;
        aresContribution = 1f + (killStacks * perStackBonus);
        ps.damageMultiplier *= aresContribution;
    }

    // ── Threshold-Handler ─────────────────────────────────────────────────
    void HandleThreshold(FavorManager.God god, string key)
    {
        if (god != FavorManager.God.Ares) return;
        switch (key)
        {
            case "intervention_1": StartCoroutine(Kriegsschrei()); break;
            case "intervention_2": StartCoroutine(Berserker());    break;
        }
    }

    // ── I1: Kriegsschrei ─────────────────────────────────────────────────
    IEnumerator Kriegsschrei()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) yield break;

        Collider[] hits = Physics.OverlapSphere(player.transform.position, warCryRadius,
            LayerMask.GetMask("Enemy"));

        var affected = new System.Collections.Generic.List<EnemyBase>();
        foreach (var hit in hits)
        {
            var e = hit.GetComponent<EnemyBase>();
            if (e == null) continue;
            e.ApplyStun(warCryStunTime);
            e.ForcedTarget = player.transform;
            affected.Add(e);
        }

        yield return new WaitForSeconds(warCryAggroTime);

        // Aggro nach Ablauf wieder freigeben
        foreach (var e in affected)
            if (e != null) e.ForcedTarget = null;
    }

    // ── I2: Berserker ────────────────────────────────────────────────────
    IEnumerator Berserker()
    {
        if (berserkerActive) yield break;
        berserkerActive = true;

        var ps = PlayerState.Instance;
        if (ps == null) { berserkerActive = false; yield break; }

        ps.damageMultiplier *= berserkerDamage;
        ps.moveSpeed        *= berserkerSpeed;
        ps.attackSpeed      *= berserkerAtkSpeed;

        // war_strategy: Gebäude während Berserker aufheilen statt unzerstörbar
        Coroutine healCo = null;
        if (SynergySystem.Instance != null && SynergySystem.Instance.IsActive("war_strategy"))
            healCo = StartCoroutine(WarStrategyBuildingHeal());

        yield return new WaitForSeconds(berserkerDuration);

        ps.damageMultiplier /= berserkerDamage;
        ps.moveSpeed        /= berserkerSpeed;
        ps.attackSpeed      /= berserkerAtkSpeed;

        if (healCo != null) StopCoroutine(healCo);
        berserkerActive = false;
    }

    IEnumerator WarStrategyBuildingHeal()
    {
        var wait = new WaitForSeconds(0.5f);
        while (berserkerActive)
        {
            var buildings = FindObjectsOfType<BuildingBase>();
            foreach (var b in buildings)
                b.Heal(warStrategyHealPerSec * 0.5f);
            yield return wait;
        }
    }
}
