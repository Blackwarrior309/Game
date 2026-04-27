// Kronos.cs
// Ablegen in: Assets/Scripts/Enemies/Kronos.cs
// FINALBOSS — 3 Phasen, reagiert auf Spieler-Build
// Kinder-Nodes:
//   - Transform "ScytheAttackPoint"
//   - Transform "ProjectileSpawnPoint"
//   - ParticleSystem "TimeBubbleFX"
//   - ParticleSystem "TimeWaveFX"
//   - ParticleSystem "PhaseTransitionFX"
//   - AudioSource "BossAudio"

using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

public class Kronos : EnemyBase
{
    // ── Phasen ─────────────────────────────────────────────────────────────
    enum Phase { Phase1, Phase2, Phase3 }
    Phase currentPhase = Phase.Phase1;

    [Header("Phase Thresholds")]
    [SerializeField] float phase2Threshold = 0.60f;
    [SerializeField] float phase3Threshold = 0.30f;

    [Header("Phase 1 — Zeit verlangsamt sich")]
    [SerializeField] float timeSlowAmount    = 0.20f;  // 20% Verlangsamung (Feinde + Türme)
    [SerializeField] float timeBubbleRate    = 5f;     // Sek. zwischen Blasen-Spawns
    [SerializeField] int   timeBubbleCount   = 3;
    [SerializeField] float timeBubbleFreeze  = 3f;     // Sekunden eingefroren bei Treffer
    [SerializeField] GameObject timeBubblePrefab;

    [Header("Phase 2 — Zeit dreht sich zurück")]
    [SerializeField] float rewindHealPercent = 0.10f;  // 10% HP bei 3s ohne Schaden
    [SerializeField] float rewindWindow      = 3f;
    [SerializeField] float timeWaveCooldown  = 4f;
    [SerializeField] GameObject timeWavePrefab;

    [Header("Phase 3 — Der Titan erwacht")]
    [SerializeField] float arenaShrinkInterval = 30f;
    [SerializeField] float arenaShrinkAmount   = 10f;
    [SerializeField] float arenaCurrentRadius  = 80f;
    [SerializeField] GameObject titanServantPrefab_Zeus;
    [SerializeField] GameObject titanServantPrefab_Athena;
    [SerializeField] GameObject titanServantPrefab_Ares;
    [SerializeField] GameObject titanServantPrefab_Poseidon;
    [SerializeField] GameObject titanServantPrefab_Hades;
    [SerializeField] GameObject arenaShrinkWall;  // Visueller Zeitnebel-Ring

    [Header("Finale")]
    [SerializeField] float finalStandDuration = 5f;

    [Header("Audio / FX")]
    [SerializeField] ParticleSystem phaseTransitionFX;
    [SerializeField] ParticleSystem timeBubbleFX;

    // ── Laufzeit-State ─────────────────────────────────────────────────────
    float timeBubbleTimer = 0f;
    float timeWaveTimer   = 0f;
    float rewindTimer     = 0f;        // Zeit seit letztem Schaden
    float shrinkTimer     = 0f;
    bool  finalStandTriggered = false;
    bool  suppressionActive   = false;
    float suppressionTimer    = 0f;
    FavorManager.God suppressedGod;

    // Schaden-Tracking für Rewind
    float lastDamageTaken = 0f;

    // Größe für Phase 3
    bool phase3Entered = false;

    protected override void Awake()
    {
        maxHp          = 2500f;
        moveSpeed      = 3.5f;
        damage         = 35f;
        attackCooldown = 1.8f;
        attackRange    = 4.0f;
        xpReward       = 0f;    // Sieg gibt stattdessen Oboloi
        ashDropMin     = 0;
        ashDropMax     = 0;
        prioritizePyros = false;
        base.Awake();
    }

    protected override void Start()
    {
        base.Start();

        // Boss-Bar einblenden
        HUDManager.Instance?.ShowBossPanel(true);

        // Eintritts-Sequenz
        StartCoroutine(KronosEntrance());
    }

    protected override void Update()
    {
        if (isDead) return;

        UpdateBossHP();
        CheckPhaseTransitions();

        switch (currentPhase)
        {
            case Phase.Phase1: UpdatePhase1(); break;
            case Phase.Phase2: UpdatePhase2(); break;
            case Phase.Phase3: UpdatePhase3(); break;
        }

        // Götter-Unterdrückung Countdown
        if (suppressionActive)
        {
            suppressionTimer -= Time.deltaTime;
            if (suppressionTimer <= 0f) EndSuppression();
        }

        base.Update();
    }

    // ── Eintrittsssequenz ──────────────────────────────────────────────────
    IEnumerator KronosEntrance()
    {
        if (agent != null) agent.isStopped = true;
        isStunned = true;

        // Langsames Anwachsen
        transform.localScale = Vector3.one * 0.3f;
        float t = 0f;
        while (t < 2f)
        {
            t += Time.deltaTime;
            transform.localScale = Vector3.Lerp(Vector3.one * 0.3f, Vector3.one, t / 2f);
            yield return null;
        }

        // Zeit-Slow-Aura aktivieren (Phase 1)
        ApplyTimeSlowAura(true);

        yield return new WaitForSeconds(0.5f);
        isStunned = false;
        if (agent != null) agent.isStopped = false;
    }

    // ══ PHASE 1 ════════════════════════════════════════════════════════════
    void UpdatePhase1()
    {
        // Zeit-Blasen spawnen
        timeBubbleTimer -= Time.deltaTime;
        if (timeBubbleTimer <= 0f)
        {
            timeBubbleTimer = timeBubbleRate;
            SpawnTimeBubbles();
        }
    }

    void SpawnTimeBubbles()
    {
        if (timeBubblePrefab == null) return;

        for (int i = 0; i < timeBubbleCount; i++)
        {
            // Zufällige Position in der Arena
            Vector2 rand2D = Random.insideUnitCircle * 30f;
            Vector3 spawnPos = new Vector3(rand2D.x, 0.5f, rand2D.y);

            var bubble = Instantiate(timeBubblePrefab, spawnPos, Quaternion.identity);
            bubble.GetComponent<TimeBubble>()?.Initialize(timeBubbleFreeze);

            // Blasen despawnen nach 8s
            Destroy(bubble, 8f);
        }
    }

    void ApplyTimeSlowAura(bool active)
    {
        // Alle Türme und Feind-Spawns verlangsamen
        // In echter Implementierung: GlobalSlowManager.SetSlow(active ? (1f - timeSlowAmount) : 1f);
        // Vereinfacht: PlayerState moveSpeed anpassen
        if (active)
            PlayerState.Instance.moveSpeed *= (1f - timeSlowAmount);
        else
            PlayerState.Instance.moveSpeed /= (1f - timeSlowAmount);
    }

    // ══ PHASE 2 ════════════════════════════════════════════════════════════
    void UpdatePhase2()
    {
        // Rewind-Healing: 3s ohne Schaden → 10% HP
        rewindTimer += Time.deltaTime;
        if (rewindTimer >= rewindWindow)
        {
            rewindTimer = 0f;
            float healAmount = maxHp * rewindHealPercent;
            hp = Mathf.Min(maxHp, hp + healAmount);
            // Visuelles Feedback wäre hier gut (Partikel, Shader-Flash)
        }

        // Zeitwellen
        timeWaveTimer -= Time.deltaTime;
        if (timeWaveTimer <= 0f)
        {
            timeWaveTimer = timeWaveCooldown;
            StartCoroutine(LaunchTimeWave());
        }

        // Götter-Unterdrückung (alle 15s)
        if (!suppressionActive && Time.frameCount % (int)(15f * 60f) == 0)
            StartSuppression();
    }

    IEnumerator LaunchTimeWave()
    {
        // Warnung (Windup)
        yield return new WaitForSeconds(0.8f);

        // Lineare Zeitwelle in 3 Richtungen
        for (int i = 0; i < 3; i++)
        {
            float angle = (i * 120f) * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));

            if (timeWavePrefab != null)
            {
                var wave = Instantiate(timeWavePrefab, transform.position, Quaternion.LookRotation(dir));
                var proj = wave.GetComponent<ProjectileBase>();
                proj?.Initialize(dir, damage * 0.8f, "enemy_kronos_wave");
                Destroy(wave, 3f);
            }
        }
    }

    void StartSuppression()
    {
        // Zufälligen aktiven Gott unterdrücken
        var activeGods = new List<FavorManager.God>();
        foreach (FavorManager.God god in System.Enum.GetValues(typeof(FavorManager.God)))
        {
            if (FavorManager.Instance.IsPassiveActive(god))
                activeGods.Add(god);
        }

        if (activeGods.Count == 0) return;

        suppressedGod    = activeGods[Random.Range(0, activeGods.Count)];
        suppressionActive = true;
        suppressionTimer  = 20f;

        // Passiv temporär deaktivieren (Signal ans HUD)
        FavorManager.OnPassiveDeactivated?.Invoke(suppressedGod);
        // TODO: Tatsächlichen Passiv-Effekt pausieren via GodPassiveManager
    }

    void EndSuppression()
    {
        suppressionActive = false;
        FavorManager.OnPassiveActivated?.Invoke(suppressedGod);
    }

    // ══ PHASE 3 ════════════════════════════════════════════════════════════
    void EnterPhase3()
    {
        if (phase3Entered) return;
        phase3Entered = true;

        // Kronos wird größer
        StartCoroutine(ScaleUp(1.4f, 1.5f));

        // Titan-Diener spawnen (1 pro aktiven Tempel)
        SpawnTitanServants();

        // Spieler priorisieren wenn legendäre Waffe vorhanden
        // (PlayerState hat kein LegendaryWeapons-Count in dieser Version — vereinfacht)
        ForcedTarget = playerTransform;

        // Arena-Shrink starten
        shrinkTimer = arenaShrinkInterval;
    }

    void UpdatePhase3()
    {
        // Arena-Shrink
        shrinkTimer -= Time.deltaTime;
        if (shrinkTimer <= 0f)
        {
            shrinkTimer = arenaShrinkInterval;
            ShrinkArena();
        }

        // Zeit-Stillstand bei 10% HP
        if (!finalStandTriggered && hp <= maxHp * 0.10f)
        {
            finalStandTriggered = true;
            StartCoroutine(FinalTimeFreeze());
        }
    }

    void SpawnTitanServants()
    {
        int templeCount = PlayerState.Instance.activeTemples;

        // Servant-Typen basierend auf gebauten Tempeln
        var servantPrefabs = new Dictionary<FavorManager.God, GameObject>
        {
            { FavorManager.God.Zeus,     titanServantPrefab_Zeus },
            { FavorManager.God.Athena,   titanServantPrefab_Athena },
            { FavorManager.God.Ares,     titanServantPrefab_Ares },
            { FavorManager.God.Poseidon, titanServantPrefab_Poseidon },
            { FavorManager.God.Hades,    titanServantPrefab_Hades },
        };

        int spawned = 0;
        foreach (var kvp in servantPrefabs)
        {
            if (spawned >= templeCount) break;
            if (!FavorManager.Instance.IsTempleBuilt(kvp.Key)) continue;
            if (kvp.Value == null) continue;

            // Spawn neben Kronos
            Vector3 offset = Random.insideUnitSphere * 8f;
            offset.y = 0f;
            Instantiate(kvp.Value, transform.position + offset, Quaternion.identity);
            spawned++;
        }
    }

    void ShrinkArena()
    {
        arenaCurrentRadius = Mathf.Max(20f, arenaCurrentRadius - arenaShrinkAmount);

        // Zeitwand-Ring skalieren
        if (arenaShrinkWall != null)
            arenaShrinkWall.transform.localScale = Vector3.one * (arenaCurrentRadius * 2f / 80f);

        // Spieler zurückdrängen wenn außerhalb
        if (playerTransform != null)
        {
            float playerDist = Vector3.Distance(Vector3.zero, playerTransform.position);
            if (playerDist > arenaCurrentRadius)
            {
                Vector3 pushDir = (playerTransform.position - Vector3.zero).normalized;
                playerTransform.position = pushDir * (arenaCurrentRadius - 1f);
                PlayerState.Instance.TakeDamage(10f); // Zeitwand-Schaden
            }
        }
    }

    // ── Zeit-Stillstand (Finaler Angriff) ──────────────────────────────────
    IEnumerator FinalTimeFreeze()
    {
        // Ankündigung
        yield return new WaitForSeconds(0.5f);

        // Alle Verbündeten und Türme einfrieren (5s)
        // GlobalFreezeManager.FreezeFriendlies(finalStandDuration); // TODO

        // Massiver Angriff auf Spieler
        if (playerTransform != null)
        {
            float dist = Vector3.Distance(transform.position, playerTransform.position);
            if (dist <= attackRange * 3f)
                playerTransform.GetComponent<PlayerController>()?.TakeDamage(damage * 2.5f);
        }

        yield return new WaitForSeconds(finalStandDuration);
        // Einfrieren aufheben — Spieler hatte 5s Zeit zu dodgen
    }

    IEnumerator ScaleUp(float targetScale, float duration)
    {
        Vector3 startScale = transform.localScale;
        Vector3 endScale   = Vector3.one * targetScale;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            transform.localScale = Vector3.Lerp(startScale, endScale, t / duration);
            yield return null;
        }
        transform.localScale = endScale;
    }

    // ── Phasen-Übergänge ───────────────────────────────────────────────────
    void CheckPhaseTransitions()
    {
        float ratio = hp / maxHp;

        if (currentPhase == Phase.Phase1 && ratio <= phase2Threshold)
        {
            currentPhase = Phase.Phase2;
            StartCoroutine(PhaseTransition(2));
        }
        else if (currentPhase == Phase.Phase2 && ratio <= phase3Threshold)
        {
            currentPhase = Phase.Phase3;
            StartCoroutine(PhaseTransition(3));
        }
    }

    IEnumerator PhaseTransition(int phase)
    {
        isStunned = true;
        if (agent != null) agent.isStopped = true;

        if (phaseTransitionFX != null) phaseTransitionFX.Play();

        yield return new WaitForSeconds(1.5f);

        if (phase == 3)
        {
            EnterPhase3();
            ApplyTimeSlowAura(false);  // Phase-1-Slow aufheben, Phase-3 ist anders
        }

        isStunned = false;
        if (agent != null) agent.isStopped = false;
    }

    // ── Schaden-Override ───────────────────────────────────────────────────
    public override void TakeDamage(float amount)
    {
        rewindTimer = 0f;   // Rewind-Healing zurücksetzen
        base.TakeDamage(amount);
    }

    // ── HUD ────────────────────────────────────────────────────────────────
    void UpdateBossHP()
    {
        // HUDManager.Instance?.wavePanel?.UpdateBossHP(hp, maxHp);
        // Direkt via statischem Event:
        KronosHPChanged?.Invoke(hp, maxHp);
    }

    public static event System.Action<float, float> KronosHPChanged;

    // ── Tod ────────────────────────────────────────────────────────────────
    protected override void Die()
    {
        if (isDead) return;
        isDead = true;

        ApplyTimeSlowAura(false);
        HUDManager.Instance?.ShowBossPanel(false);
        FavorManager.Instance.OnBossKill();
        GameEvents.RaiseGameWon();

        StartCoroutine(DeathSequence());
    }

    IEnumerator DeathSequence()
    {
        if (agent != null) agent.isStopped = true;

        // Dramatische Explosion-Sequenz
        for (int i = 0; i < 5; i++)
        {
            if (phaseTransitionFX != null) phaseTransitionFX.Play();
            yield return new WaitForSeconds(0.4f);
        }

        yield return new WaitForSeconds(0.5f);
        Destroy(gameObject);
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// TimeBubble.cs — Kronos Phase 1 Zeitblase
// Ablegen in: Assets/Scripts/Enemies/TimeBubble.cs
// ─────────────────────────────────────────────────────────────────────────────

public class TimeBubble : MonoBehaviour
{
    float freezeDuration;

    public void Initialize(float freezeDur)
    {
        freezeDuration = freezeDur;
        // Langsam auf Spieler zubewegen
        StartCoroutine(MoveTowardsPlayer());
    }

    System.Collections.IEnumerator MoveTowardsPlayer()
    {
        var playerGO = GameObject.FindGameObjectWithTag("Player");
        if (playerGO == null) yield break;

        float speed = 2.5f;
        while (this != null && gameObject != null)
        {
            Vector3 dir = (playerGO.transform.position - transform.position).normalized;
            transform.position += dir * speed * Time.deltaTime;
            yield return null;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        // Spieler einfrieren
        StartCoroutine(FreezePlayer(other.GetComponent<PlayerController>()));
        Destroy(gameObject);
    }

    System.Collections.IEnumerator FreezePlayer(PlayerController player)
    {
        if (player == null) yield break;

        float origSpeed = PlayerState.Instance.moveSpeed;
        PlayerState.Instance.moveSpeed = 0f;
        // CharacterController auch stoppen:
        player.enabled = false;

        yield return new WaitForSeconds(freezeDuration);

        if (player != null)
        {
            player.enabled = true;
            PlayerState.Instance.moveSpeed = origSpeed;
        }
    }
}
