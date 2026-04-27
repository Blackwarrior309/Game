// ShadowAlly.cs
// Ablegen in: Assets/Scripts/Allies/ShadowAlly.cs
// Hades-Passiv: Verbündeter Schatten aus getötetem Feind
// Spawnt via GameEvents.OnSpawnShadowAlly
// Kinder: MeshInstance (dunkel/transparent), NavMeshAgent

using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class ShadowAlly : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] float maxHp       = 40f;
    [SerializeField] float moveSpeed   = 5f;
    [SerializeField] float damage      = 8f;
    [SerializeField] float attackRange = 1.8f;
    [SerializeField] float attackCooldown = 1.2f;
    [SerializeField] float lifetime    = 20f;  // Wird von außen gesetzt

    float hp;
    float attackTimer = 0f;
    float lifetimeTimer;
    bool isDead = false;
    bool isPermanent = false;  // Hades Avatar: permanent bis Run-Ende

    NavMeshAgent agent;
    EnemyBase currentTarget;

    // Seelenschmiede-Synergie: trägt Kopie der Hauptwaffe
    bool hasSoulForgeWeapon = false;
    float weaponDamageMultiplier = 0.10f; // 10% des Spielerschadens

    const float Gravity = -20f;
    float verticalVelocity;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        tag   = "Ally";
        gameObject.layer = LayerMask.NameToLayer("Ally");
    }

    void Start()
    {
        hp = maxHp;
        lifetimeTimer = lifetime;

        if (agent != null)
        {
            agent.speed = moveSpeed;
            // Seelenschmiede-Synergie: Geschwindigkeitsbonus auf verlangsamte Feinde
            if (SynergySystem.Instance.IsActive("flood_of_souls"))
                agent.speed = moveSpeed * 1.5f;
        }

        // Seelenschmiede: Waffen-Kopie
        if (SynergySystem.Instance.IsActive("soul_forge"))
        {
            hasSoulForgeWeapon = true;
            weaponDamageMultiplier = 0.10f;
        }

        // Flut der Seelen: Schatten haben Speed-Boost auf verlangsamte Feinde
        // (wird im Update() dynamisch überprüft)
    }

    void Update()
    {
        if (isDead) return;

        // Lifetime
        if (!isPermanent)
        {
            lifetimeTimer -= Time.deltaTime;
            if (lifetimeTimer <= 0f) { Die(); return; }
        }

        FindAndAttack();
    }

    // ── Gegner suchen und angreifen ────────────────────────────────────────
    void FindAndAttack()
    {
        // Nächsten Feind suchen
        if (currentTarget == null || !currentTarget.isActiveAndEnabled)
            currentTarget = FindNearestEnemy();

        if (currentTarget == null) return;

        // Bewegen
        if (agent != null && agent.isOnNavMesh)
        {
            float speed = moveSpeed;
            // Flut der Seelen: Geschwindigkeitsbonus auf verlangsamten Feind
            if (SynergySystem.Instance.IsActive("flood_of_souls") && currentTarget.isStunned)
                speed = moveSpeed * 1.5f;

            agent.speed = speed;
            agent.SetDestination(currentTarget.transform.position);
        }

        // Angriff
        attackTimer -= Time.deltaTime;
        float dist = Vector3.Distance(transform.position, currentTarget.transform.position);
        if (attackTimer <= 0f && dist <= attackRange)
        {
            attackTimer = attackCooldown;
            DoAttack();
        }
    }

    EnemyBase FindNearestEnemy()
    {
        EnemyBase[] enemies = FindObjectsOfType<EnemyBase>();
        EnemyBase closest = null;
        float closestDist = float.MaxValue;

        foreach (var e in enemies)
        {
            if (e.isDead) continue;
            float d = Vector3.Distance(transform.position, e.transform.position);
            if (d < closestDist) { closestDist = d; closest = e; }
        }
        return closest;
    }

    void DoAttack()
    {
        if (currentTarget == null) return;

        float finalDamage = damage;

        // Seelenschmiede: Waffenschaden addieren
        if (hasSoulForgeWeapon)
            finalDamage += PlayerState.Instance.damage * weaponDamageMultiplier;

        currentTarget.TakeDamage(finalDamage);

        // Flut der Seelen: Unterwelt-Tor tötet alle verlangsamten Feinde gleichzeitig
        // (wird separat via Intervention getriggert, nicht hier)
    }

    // ── Öffentliche Methoden ───────────────────────────────────────────────
    public void SetLifetime(float duration)
    {
        lifetime = duration;
        lifetimeTimer = duration;
    }

    public void SetPermanent(bool permanent)
    {
        isPermanent = permanent;
        lifetimeTimer = float.MaxValue;
    }

    public void TakeDamage(float amount)
    {
        hp -= amount;
        if (hp <= 0f) Die();
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;
        Destroy(gameObject, 0.1f);
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// ShadowAllySpawner.cs — Verwaltet das Spawnen via GameEvents
// Ablegen in: Assets/Scripts/Allies/ShadowAllySpawner.cs
// Auf dem Singletons-GameObject platzieren
// ─────────────────────────────────────────────────────────────────────────────

public class ShadowAllySpawner : MonoBehaviour
{
    [SerializeField] GameObject shadowAllyPrefab;
    [SerializeField] int        maxShadows = 10;

    int activeShadowCount = 0;

    void OnEnable()  => GameEvents.OnSpawnShadowAlly += SpawnShadow;
    void OnDisable() => GameEvents.OnSpawnShadowAlly -= SpawnShadow;

    void SpawnShadow(Vector3 position)
    {
        if (shadowAllyPrefab == null) return;
        if (activeShadowCount >= maxShadows) return;

        var shadow = Instantiate(shadowAllyPrefab, position, Quaternion.identity);
        var ally   = shadow.GetComponent<ShadowAlly>();

        if (ally != null)
        {
            // Standard-Lifetime
            ally.SetLifetime(20f);
        }

        activeShadowCount++;
        // Wenn Shadow stirbt, Counter dekrementieren
        var tracker = shadow.AddComponent<ShadowDeathTracker>();
        tracker.spawner = this;
    }

    public void OnShadowDied() => activeShadowCount = Mathf.Max(0, activeShadowCount - 1);

    // Hades-Intervention 1: Sofortige Beschwörung vieler Schatten
    public void SummonShadowsFromDeadEnemies(int maxCount)
    {
        // Alle aktuell toten Feinde in der Szene suchen
        // (vereinfacht: random Positionen im Radius)
        for (int i = 0; i < Mathf.Min(maxCount, maxShadows - activeShadowCount); i++)
        {
            Vector2 rand = Random.insideUnitCircle * 20f;
            SpawnShadow(new Vector3(rand.x, 0f, rand.y));
        }
    }

    // Hades-Avatar: alle Schatten permanent machen
    public void MakeShadowsPermanent()
    {
        var allies = FindObjectsOfType<ShadowAlly>();
        foreach (var ally in allies)
            ally.SetPermanent(true);
    }
}

// Hilfsklasse für Death-Tracking
public class ShadowDeathTracker : MonoBehaviour
{
    public ShadowAllySpawner spawner;

    void OnDestroy()
    {
        spawner?.OnShadowDied();
    }
}
