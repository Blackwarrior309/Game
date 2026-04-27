// GiantPrecursor.cs
// Ablegen in: Assets/Scripts/Enemies/GiantPrecursor.cs
// Welle-9-Mini-Boss — Vorbote des Kronos. Trägt eine Zeit-Slow-Aura,
// die Spieler und Türme im Radius verlangsamt (Vorschau auf Kronos Phase 1).
// Keine Zeit-Blasen, keine Stomp-AoE — der Gigant ist ein laufender
// Tankbrecher mit Debuff-Aura.
//
// Kinder-Nodes:
//   - ParticleSystem "AuraFX" (lila/violetter Staub-Effekt um den Gigant)

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GiantPrecursor : EnemyBase
{
    [Header("Slow-Aura")]
    [SerializeField] float auraRadius           = 12f;
    [SerializeField] float auraSlowFactor       = 0.75f;   // Spieler + Türme × 0.75
    [SerializeField] float auraTickInterval     = 0.4f;
    [SerializeField] float auraRefreshDuration  = 0.55f;   // Slow-Refresh > Tick → bleibt aktiv
    [SerializeField] ParticleSystem auraFX;

    [Header("Combat")]
    [SerializeField] float buildingDamageMultiplier = 2f;

    bool playerInAura = false;
    bool hudRegistered = false;

    // ── Stats ──────────────────────────────────────────────────────────────
    protected override void Awake()
    {
        maxHp           = 600f;
        moveSpeed       = 2.5f;
        damage          = 30f;
        attackCooldown  = 2.5f;
        attackRange     = 3.5f;
        xpReward        = 60f;
        ashDropMin      = 12;
        ashDropMax      = 20;
        oreDropChance   = 0.30f;
        prioritizePyros = false;   // Mischziel — Spieler bevorzugt
        base.Awake();
    }

    protected override void Start()
    {
        base.Start();

        // Mini-Boss-HP-Leiste im HUD
        HUDManager.Instance?.ShowBossPanel(true);
        hudRegistered = true;

        if (auraFX != null) auraFX.Play();
        StartCoroutine(AuraLoop());
    }

    // ── Slow-Aura-Loop ─────────────────────────────────────────────────────
    IEnumerator AuraLoop()
    {
        while (!isDead)
        {
            UpdatePlayerSlow();
            RefreshTurretSlow();
            yield return new WaitForSeconds(auraTickInterval);
        }
        ClearPlayerSlow();   // Aufräumen falls über Die() umgangen
    }

    void UpdatePlayerSlow()
    {
        var player = playerTransform != null
            ? playerTransform
            : GameObject.FindGameObjectWithTag("Player")?.transform;
        if (player == null) return;

        bool inRange = Vector3.Distance(transform.position, player.position) <= auraRadius;

        if (inRange && !playerInAura)
        {
            PlayerState.Instance.moveSpeed *= auraSlowFactor;
            playerInAura = true;
        }
        else if (!inRange && playerInAura)
        {
            ClearPlayerSlow();
        }
    }

    void ClearPlayerSlow()
    {
        if (!playerInAura) return;
        PlayerState.Instance.moveSpeed /= auraSlowFactor;
        playerInAura = false;
    }

    void RefreshTurretSlow()
    {
        // Türme im Radius bekommen kurze Fire-Rate-Drosselung. Da der Tick
        // schneller läuft als die Slow-Dauer, bleibt der Effekt aktiv solange
        // der Turm im Aura-Radius steht; verlässt der Gigant den Bereich
        // (oder stirbt), läuft die Slow-Dauer aus und der Turm normalisiert.
        Collider[] hits = Physics.OverlapSphere(transform.position, auraRadius,
            LayerMask.GetMask("Building"));
        foreach (var hit in hits)
            hit.GetComponent<TurretBase>()?.SetFireRateMultiplier(auraSlowFactor, auraRefreshDuration);
    }

    // ── Schaden gegen Gebäude ──────────────────────────────────────────────
    protected override void DealDamage()
    {
        if (target != null && target.CompareTag("Building"))
        {
            target.GetComponent<BuildingBase>()?.TakeDamage(damage * buildingDamageMultiplier);
            return;
        }
        base.DealDamage();
    }

    // ── Tod ────────────────────────────────────────────────────────────────
    protected override void Die()
    {
        ClearPlayerSlow();

        if (hudRegistered)
            HUDManager.Instance?.ShowBossPanel(false);

        FavorManager.Instance.OnBossKill();   // Mini-Boss zählt wie Cyclops
        base.Die();
    }
}
