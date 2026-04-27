// ShadowWraith.cs
// Ablegen in: Assets/Scripts/Enemies/ShadowWraith.cs
// Teleportiert sich — ignoriert Mauern — greift direkt Spieler/Pyros an
// Kinder-Nodes: ParticleSystem "TeleportFX" (optional)

using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class ShadowWraith : EnemyBase
{
    [Header("Wraith Specific")]
    [SerializeField] float teleportCooldown  = 5f;
    [SerializeField] float teleportRange     = 12f;   // Max-Teleport-Distanz
    [SerializeField] float teleportMinDist   = 4f;    // Min-Distanz zum Ziel nach Teleport
    [SerializeField] float teleportMaxDist   = 6f;    // Max-Distanz zum Ziel nach Teleport
    [SerializeField] float phaseInDuration   = 0.4f;  // Sichtbarkeits-Übergang
    [SerializeField] ParticleSystem teleportFX;

    float teleportTimer = 0f;
    bool isPhasing      = false;
    Renderer[] renderers;

    protected override void Awake()
    {
        maxHp          = 45f;
        moveSpeed      = 4.5f;
        damage         = 12f;
        attackCooldown = 1.5f;
        attackRange    = 1.5f;
        xpReward       = 25f;
        ashDropMin     = 2;
        ashDropMax     = 5;
        oreDropChance  = 0.08f;
        prioritizePyros = true;
        base.Awake();
    }

    protected override void Start()
    {
        base.Start();
        renderers = GetComponentsInChildren<Renderer>();

        // NavMeshAgent deaktivieren — Wraith teleportiert statt normal zu laufen
        // Agent wird nur für kurze Strecken genutzt, Mauern werden ignoriert
        if (agent != null)
        {
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
            // Wraith ignoriert NavMesh-Hindernisse durch direkten Positionssprung
        }

        // Kurze Startphase: unsichtbar erscheinen
        StartCoroutine(PhaseIn(transform.position));
    }

    protected override void Update()
    {
        if (isDead || isStunned || isPhasing) return;

        // Teleport-Cooldown
        teleportTimer -= Time.deltaTime;
        if (teleportTimer <= 0f && target != null)
        {
            float distToTarget = Vector3.Distance(transform.position, target.position);
            // Teleportiere wenn weit weg, oder zufällig
            if (distToTarget > teleportRange || Random.value < 0.01f)
            {
                StartCoroutine(TeleportTowardsTarget());
                return;
            }
        }

        base.Update();
    }

    // ── Teleport-Mechanik ──────────────────────────────────────────────────
    IEnumerator TeleportTowardsTarget()
    {
        if (target == null || isPhasing) yield break;

        teleportTimer = teleportCooldown + Random.Range(-1f, 1f);
        isPhasing     = true;

        // Phase OUT
        yield return StartCoroutine(PhaseOut());

        // Neue Position: direkt neben Ziel, zufälliger Winkel
        Vector3 targetPos = target.position;
        float angle       = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        float dist        = Random.Range(teleportMinDist, teleportMaxDist);
        Vector3 offset    = new Vector3(Mathf.Cos(angle) * dist, 0f, Mathf.Sin(angle) * dist);
        Vector3 newPos    = targetPos + offset;

        // Auf NavMesh samplen (falls verfügbar), sonst direkt setzen
        if (NavMesh.SamplePosition(newPos, out NavMeshHit hit, 3f, NavMesh.AllAreas))
            newPos = hit.position;

        transform.position = newPos;

        // Phase IN
        yield return StartCoroutine(PhaseIn(newPos));
        isPhasing = false;
    }

    IEnumerator PhaseOut()
    {
        if (teleportFX != null) teleportFX.Play();

        float t = 0f;
        while (t < phaseInDuration)
        {
            t += Time.deltaTime;
            SetAlpha(1f - t / phaseInDuration);
            yield return null;
        }
        SetAlpha(0f);
    }

    IEnumerator PhaseIn(Vector3 pos)
    {
        if (teleportFX != null) teleportFX.Play();

        SetAlpha(0f);
        float t = 0f;
        while (t < phaseInDuration)
        {
            t += Time.deltaTime;
            SetAlpha(t / phaseInDuration);
            yield return null;
        }
        SetAlpha(1f);
    }

    void SetAlpha(float alpha)
    {
        foreach (var r in renderers)
        {
            if (r == null) continue;
            foreach (var mat in r.materials)
            {
                Color c = mat.color;
                c.a = alpha;
                mat.color = c;
                // Für URP: mat.SetFloat("_Surface", 1); // Transparent-Modus
            }
        }
    }

    // ── Wraith ignoriert Mauern beim Angriff ──────────────────────────────
    protected override void DealDamage()
    {
        // Wraith kann direkt durch Mauern angreifen — kein Kollisions-Check nötig
        base.DealDamage();
    }

    protected override void Die()
    {
        // Sterbe-Effekt: kurzes Aufblitzen dann verschwinden
        StartCoroutine(DeathFade());
    }

    IEnumerator DeathFade()
    {
        isDead = true;
        if (agent != null) agent.isStopped = true;

        // XP und Drops
        PlayerState.Instance.AddXP(xpReward);

        float t = 0f;
        while (t < 0.5f)
        {
            t += Time.deltaTime;
            // Aufflackern
            SetAlpha(Mathf.PingPong(t * 8f, 1f));
            yield return null;
        }
        SetAlpha(0f);

        // Base-Die-Logik (Drops, Favor etc.) manuell aufrufen
        FavorManager.Instance.OnEnemyKill();
        GameEvents.RaiseEnemyKilled(gameObject, transform.position);

        // Hades-Passiv
        if (FavorManager.Instance.IsPassiveActive(FavorManager.God.Hades) && Random.value < 0.15f)
            GameEvents.RaiseSpawnShadowAlly(transform.position);

        Destroy(gameObject);
    }
}
