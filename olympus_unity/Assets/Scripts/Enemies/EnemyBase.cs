// EnemyBase.cs
// Basisklasse für alle Feinde — von Unterklassen geerbt
// Ablegen in: Assets/Scripts/Enemies/EnemyBase.cs
// Benötigt: NavMeshAgent auf dem GameObject

using UnityEngine;
using UnityEngine.AI;
using System.Collections;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyBase : MonoBehaviour
{
    // ── Inspector (überschreiben in Unterklassen per new oder SerializeField) ─
    [Header("Stats")]
    public float maxHp        = 30f;
    public float moveSpeed    = 3f;
    public float damage       = 5f;
    public float attackCooldown = 1.2f;
    public float attackRange  = 1.5f;

    [Header("Rewards")]
    public float xpReward    = 10f;
    public int   ashDropMin  = 1;
    public int   ashDropMax  = 3;
    public float oreDropChance = 0.05f;

    [Header("AI")]
    public bool prioritizePyros = true;  // false = Spieler bevorzugen

    // ── Prefab-Referenzen ─────────────────────────────────────────────────
    [Header("Drop Prefabs")]
    [SerializeField] protected GameObject ashDropPrefab;
    [SerializeField] protected GameObject oreDropPrefab;
    [SerializeField] protected GameObject xpDropPrefab;

    // ── State ──────────────────────────────────────────────────────────────
    protected float      hp;
    protected bool       isDead;
    protected bool       isStunned;
    protected float      slowFactor = 1f;
    protected float      attackTimer;
    protected NavMeshAgent agent;
    protected Transform  target;
    protected Transform  playerTransform;
    protected Transform  pyrosTransform;
    // Für Ares-Intervention: erzwungenes Ziel
    [HideInInspector] public Transform ForcedTarget;
    // Meta-Flags (für Synergien)
    bool killedByLightning;

    // ── Unity Lifecycle ────────────────────────────────────────────────────
    protected virtual void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        tag   = "Enemy";
        gameObject.layer = LayerMask.NameToLayer("Enemy");
    }

    protected virtual void Start()
    {
        hp = maxHp;
        agent.speed = moveSpeed;

        var pyrosGO  = GameObject.FindGameObjectWithTag("Pyros");
        var playerGO = GameObject.FindGameObjectWithTag("Player");
        if (pyrosGO)  pyrosTransform  = pyrosGO.transform;
        if (playerGO) playerTransform = playerGO.transform;

        ChooseTarget();
    }

    protected virtual void Update()
    {
        if (isDead || isStunned) return;
        ChooseTarget();
        Navigate();
        HandleAttack();
    }

    // ── Target-Wahl ────────────────────────────────────────────────────────
    void ChooseTarget()
    {
        if (ForcedTarget != null) { target = ForcedTarget; return; }
        target = prioritizePyros
            ? (pyrosTransform  != null ? pyrosTransform  : playerTransform)
            : (playerTransform != null ? playerTransform : pyrosTransform);
    }

    // ── Navigation ─────────────────────────────────────────────────────────
    void Navigate()
    {
        if (target == null) return;
        agent.speed = moveSpeed * slowFactor;
        agent.SetDestination(target.position);
    }

    // ── Angriff ────────────────────────────────────────────────────────────
    void HandleAttack()
    {
        attackTimer -= Time.deltaTime;
        if (attackTimer > 0f || target == null) return;
        float dist = Vector3.Distance(transform.position, target.position);
        if (dist <= attackRange)
        {
            attackTimer = attackCooldown;
            DealDamage();
        }
    }

    protected virtual void DealDamage()
    {
        if (target.CompareTag("Pyros"))
        {
            var pyros = target.GetComponent<Pyros>();
            pyros?.TakeDamage(damage);
        }
        else if (target.CompareTag("Player"))
        {
            var player = target.GetComponent<PlayerController>();
            player?.TakeDamage(damage);

            // Poseidon-Passiv: Spieler-Treffer → Verlangsamung
            if (FavorManager.Instance.IsPassiveActive(FavorManager.God.Poseidon))
                ApplySlow(0.6f, 2f);
        }
    }

    // ── Schaden nehmen ─────────────────────────────────────────────────────
    public virtual void TakeDamage(float amount)
    {
        if (isDead) return;
        // Fluch-Modifikator
        if (killedByLightning) amount *= 1.1f;  // Vereinfacht für "cursed" check
        hp -= amount;
        if (hp <= 0f) Die();
    }

    // ── Status-Effekte ─────────────────────────────────────────────────────
    public void ApplySlow(float factor, float duration)
        => StartCoroutine(SlowCoroutine(factor, duration));

    IEnumerator SlowCoroutine(float factor, float duration)
    {
        slowFactor = factor;
        yield return new WaitForSeconds(duration);
        slowFactor = 1f;
    }

    public void ApplyStun(float duration)
        => StartCoroutine(StunCoroutine(duration));

    IEnumerator StunCoroutine(float duration)
    {
        isStunned = true;
        agent.isStopped = true;
        yield return new WaitForSeconds(duration);
        isStunned = false;
        agent.isStopped = false;
    }

    public void SetMeta(string key, bool value)
    {
        if (key == "killed_by_lightning") killedByLightning = value;
    }

    // ── Tod ────────────────────────────────────────────────────────────────
    protected virtual void Die()
    {
        if (isDead) return;
        isDead = true;
        agent.isStopped = true;

        // XP-Drop
        PlayerState.Instance.AddXP(xpReward);

        // Asche-Drop
        int ashAmount = Random.Range(ashDropMin, ashDropMax + 1);
        SpawnDrop(ashDropPrefab, ashAmount);

        // Erz-Drop (Gefahrenzone > 40m vom Pyros)
        if (pyrosTransform != null &&
            Vector3.Distance(transform.position, pyrosTransform.position) > 40f &&
            Random.value < oreDropChance)
        {
            int oreBonus = SynergySystem.Instance.IsActive("lava_sea") ? 2 : 1;
            SpawnDrop(oreDropPrefab, oreBonus);
        }

        // Favor-Gewinn
        FavorManager.Instance.OnEnemyKill();

        // Hades-Passiv: 15% Schatten-Spawn
        if (FavorManager.Instance.IsPassiveActive(FavorManager.God.Hades) && Random.value < 0.15f)
            GameEvents.RaiseSpawnShadowAlly(transform.position);

        // Synergie: Unterwelt-Sturm
        if (SynergySystem.Instance.IsActive("underworld_storm") && killedByLightning && Random.value < 0.25f)
            GameEvents.RaiseSpawnShadowAlly(transform.position);

        GameEvents.RaiseEnemyKilled(gameObject, transform.position);
        Destroy(gameObject, 0.05f);
    }

    void SpawnDrop(GameObject prefab, int amount)
    {
        if (prefab == null) return;
        var drop = Instantiate(prefab, transform.position, Quaternion.identity);
        drop.GetComponent<PickupBase>()?.SetAmount(amount);
    }
}
