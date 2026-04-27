// AvatarBase.cs
// Ablegen in: Assets/Scripts/Gods/Avatars/AvatarBase.cs
// Basis für alle Götter-Avatare (außer Hephaistos — der hat keinen Avatar).
// Wird vom AvatarSpawnSystem instanziiert, wenn FavorManager.OnAvatarStarted
// feuert; bekommt StartDespawn() vom System sobald die 30 s ablaufen
// (FavorManager.OnAvatarEnded).
//
// Subklassen pro Gott setzen GodId in Awake() und überschreiben DoAttack()
// bzw. DoSpecialAttack() für die gott-spezifische KI (P3-05..P3-09).
//
// Avatare sind unverwundbar (Timer-basiert, kein TakeDamage). Layer "Ally" (11).

using UnityEngine;
using UnityEngine.AI;
using System.Collections;

[RequireComponent(typeof(NavMeshAgent))]
public abstract class AvatarBase : MonoBehaviour
{
    [Header("Combat")]
    [SerializeField] protected float moveSpeed       = 6f;
    [SerializeField] protected float damage          = 50f;
    [SerializeField] protected float attackCooldown  = 0.8f;
    [SerializeField] protected float attackRange     = 4f;
    [SerializeField] protected float searchRadius    = 25f;

    [Header("Special-Attack")]
    [SerializeField] protected float specialCooldown = 5f;

    [Header("Despawn")]
    [SerializeField] protected float fadeOutDuration = 1.0f;

    public FavorManager.God GodId { get; protected set; }

    protected NavMeshAgent agent;
    protected EnemyBase    currentTarget;
    protected float        attackTimer;
    protected float        specialTimer;
    bool                   despawning;

    // ── Unity Lifecycle ────────────────────────────────────────────────────
    protected virtual void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        tag   = "Ally";
        gameObject.layer = LayerMask.NameToLayer("Ally");
    }

    protected virtual void Start()
    {
        if (agent != null) agent.speed = moveSpeed;
        specialTimer = specialCooldown;  // erster Special erst nach Cooldown
    }

    protected virtual void Update()
    {
        if (despawning) return;

        attackTimer  -= Time.deltaTime;
        specialTimer -= Time.deltaTime;

        UpdateTarget();
        if (currentTarget == null) return;

        if (agent != null && agent.isOnNavMesh)
            agent.SetDestination(currentTarget.transform.position);

        float dist = Vector3.Distance(transform.position, currentTarget.transform.position);

        if (specialTimer <= 0f)
        {
            specialTimer = specialCooldown;
            DoSpecialAttack();
        }
        else if (dist <= attackRange && attackTimer <= 0f)
        {
            attackTimer = attackCooldown;
            DoAttack();
        }
    }

    // ── Ziel-Suche ─────────────────────────────────────────────────────────
    void UpdateTarget()
    {
        if (currentTarget != null && !currentTarget.isDead) return;

        Collider[] hits = Physics.OverlapSphere(transform.position, searchRadius,
            LayerMask.GetMask("Enemy"));
        EnemyBase closest = null;
        float closestDist = float.MaxValue;
        foreach (var hit in hits)
        {
            var e = hit.GetComponent<EnemyBase>();
            if (e == null || e.isDead) continue;
            float d = Vector3.Distance(transform.position, e.transform.position);
            if (d < closestDist) { closestDist = d; closest = e; }
        }
        currentTarget = closest;
    }

    // ── Standard-Angriff (überschreibbar) ──────────────────────────────────
    protected virtual void DoAttack()
    {
        currentTarget?.TakeDamage(damage);
    }

    // ── Gott-spezifischer Spezialangriff (Pflicht-Override) ────────────────
    // Subklassen: Blitz-AoE, Schutzschild, Berserker-Schwung, Flutwelle,
    // Schatten-Massenbeschwörung, …
    protected abstract void DoSpecialAttack();

    // ── Despawn (vom AvatarSpawnSystem aufgerufen) ─────────────────────────
    public void StartDespawn()
    {
        if (despawning) return;
        StartCoroutine(DespawnCoroutine());
    }

    IEnumerator DespawnCoroutine()
    {
        despawning = true;
        if (agent != null) agent.isStopped = true;

        var renderers = GetComponentsInChildren<Renderer>();
        float t = 0f;
        while (t < fadeOutDuration)
        {
            t += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, t / fadeOutDuration);
            foreach (var r in renderers)
                FadeMaterial(r.material, alpha);
            yield return null;
        }
        Destroy(gameObject);
    }

    // Versucht beide gängigen Color-Properties (Standard- vs URP-Shader).
    static void FadeMaterial(Material m, float alpha)
    {
        if (m.HasProperty("_Color"))
        {
            var c = m.color;
            m.color = new Color(c.r, c.g, c.b, alpha);
        }
        if (m.HasProperty("_BaseColor"))
        {
            var c = m.GetColor("_BaseColor");
            m.SetColor("_BaseColor", new Color(c.r, c.g, c.b, alpha));
        }
    }
}
