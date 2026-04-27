// Cyclops.cs
// Ablegen in: Assets/Scripts/Enemies/Cyclops.cs
// Mini-Boss — AoE-Stampf, zerstört Gebäude sofort im Nahbereich
// Kinder-Nodes:
//   - Transform "StompCenter"
//   - ParticleSystem "StompFX"
//   - ParticleSystem "RoarFX"

using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class Cyclops : EnemyBase
{
    [Header("Cyclops Specific")]
    [SerializeField] float stompRadius        = 4.5f;
    [SerializeField] float stompDamage        = 60f;
    [SerializeField] float stompCooldown      = 6f;
    [SerializeField] float buildingKillRadius = 3f;  // Gebäude in diesem Radius sofort zerstört
    [SerializeField] float roarRadius         = 12f;
    [SerializeField] float roarSlowFactor     = 0.6f;
    [SerializeField] float roarDuration       = 3f;
    [SerializeField] ParticleSystem stompFX;
    [SerializeField] ParticleSystem roarFX;

    float stompTimer = 3f;  // Erster Stomp nach 3s
    float roarTimer  = 8f;
    bool  isStomping = false;

    // Boss-HP-Leiste im HUD (via WaveHUDPanel)
    bool hudRegistered = false;

    protected override void Awake()
    {
        maxHp          = 400f;
        moveSpeed      = 2.8f;
        damage         = 25f;
        attackCooldown = 2.0f;
        attackRange    = 3.0f;
        xpReward       = 80f;
        ashDropMin     = 15;
        ashDropMax     = 25;
        oreDropChance  = 0.40f;
        prioritizePyros = false;  // Mischziel
        base.Awake();
    }

    protected override void Start()
    {
        base.Start();

        // HP-Leiste im HUD registrieren
        HUDManager.Instance?.ShowBossPanel(true);
        hudRegistered = true;

        // Eingangs-Roar
        StartCoroutine(EntranceRoar());
    }

    protected override void Update()
    {
        if (isDead || isStunned || isStomping) return;

        // Stomp-Timer
        stompTimer -= Time.deltaTime;
        if (stompTimer <= 0f)
        {
            stompTimer = stompCooldown;
            StartCoroutine(DoStomp());
            return;
        }

        // Roar-Timer
        roarTimer -= Time.deltaTime;
        if (roarTimer <= 0f)
        {
            roarTimer = roarCooldown();
            StartCoroutine(DoRoar());
        }

        base.Update();
        UpdateBossHP();
    }

    float roarCooldown() => Random.Range(10f, 15f);

    // ── HUD-Update ─────────────────────────────────────────────────────────
    void UpdateBossHP()
    {
        HUDManager.Instance?.GetComponent<WaveHUDPanel>()?.UpdateBossHP(hp, maxHp);
        // Vereinfacht: via WaveManager-Panel; in echter Implementierung
        // WaveHUDPanel.Instance?.UpdateBossHP(hp, maxHp);
    }

    // ── Eingangs-Roar ──────────────────────────────────────────────────────
    IEnumerator EntranceRoar()
    {
        isStomping = true;  // Bewegung kurz blockieren
        if (agent != null) agent.isStopped = true;

        if (roarFX != null) roarFX.Play();

        // Kurzes Einfrieren + Kamera-Shake wäre hier gut (CameraShake.cs TODO)
        yield return new WaitForSeconds(1.5f);

        isStomping = false;
        if (agent != null) agent.isStopped = false;
    }

    // ── Stomp-Angriff ──────────────────────────────────────────────────────
    IEnumerator DoStomp()
    {
        isStomping = true;
        if (agent != null) agent.isStopped = true;

        // Ankündigungs-Phase (Warnung für Spieler)
        yield return new WaitForSeconds(0.6f);  // Visuelles Tell

        if (stompFX != null) stompFX.Play();

        // ── Gebäude sofort zerstören (innerer Radius) ──────────────────────
        Collider[] buildingHits = Physics.OverlapSphere(transform.position, buildingKillRadius,
            LayerMask.GetMask("Building"));
        foreach (var hit in buildingHits)
        {
            var building = hit.GetComponent<BuildingBase>();
            if (building != null)
                building.TakeDamage(building.maxHp * 10f); // Instant-Kill
        }

        // ── AoE-Stampf-Schaden (äußerer Radius) ───────────────────────────
        Collider[] playerHits = Physics.OverlapSphere(transform.position, stompRadius,
            LayerMask.GetMask("Player"));
        foreach (var hit in playerHits)
            hit.GetComponent<PlayerController>()?.TakeDamage(stompDamage);

        // Gebäude im Außen-Radius: normale Schaden
        Collider[] outerBuildings = Physics.OverlapSphere(transform.position, stompRadius,
            LayerMask.GetMask("Building"));
        foreach (var hit in outerBuildings)
            hit.GetComponent<BuildingBase>()?.TakeDamage(stompDamage * 1.5f);

        yield return new WaitForSeconds(0.3f);

        isStomping = false;
        if (agent != null) agent.isStopped = false;
    }

    // ── Roar-Angriff ───────────────────────────────────────────────────────
    IEnumerator DoRoar()
    {
        if (roarFX != null) roarFX.Play();

        // Alle Feinde in Radius verlangsamen
        Collider[] hits = Physics.OverlapSphere(transform.position, roarRadius,
            LayerMask.GetMask("Player"));

        foreach (var hit in hits)
        {
            // Spieler verlangsamen
            PlayerState.Instance.moveSpeed *= roarSlowFactor;
            yield return new WaitForSeconds(roarDuration);
            PlayerState.Instance.moveSpeed /= roarSlowFactor;
        }

        yield return null;
    }

    // ── Phase-Übergang (ab 50% HP: aggressiver) ───────────────────────────
    public override void TakeDamage(float amount)
    {
        float hpBefore = hp;
        base.TakeDamage(amount);

        // Phase-2 bei 50%
        if (hpBefore > maxHp * 0.5f && hp <= maxHp * 0.5f)
            EnterPhase2();
    }

    void EnterPhase2()
    {
        stompCooldown  /= 1.5f;     // Stampf häufiger
        moveSpeed      *= 1.3f;     // Schneller
        damage         *= 1.2f;
        if (agent != null) agent.speed = moveSpeed;
    }

    protected override void Die()
    {
        if (hudRegistered)
            HUDManager.Instance?.ShowBossPanel(false);

        FavorManager.Instance.OnBossKill();
        base.Die();
    }
}
