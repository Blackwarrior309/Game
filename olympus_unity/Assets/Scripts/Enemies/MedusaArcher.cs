// MedusaArcher.cs
// Ablegen in: Assets/Scripts/Enemies/MedusaArcher.cs
// Fernkampf-Feind — schießt auf Spieler UND Pyros abwechselnd
// Kinder-Nodes:
//   - Transform "ShootPoint" (Schussursprung, vor dem Modell)
//   - GameObject "ArrowPrefab" (Projektil-Prefab, im Inspector zuweisen)

using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class MedusaArcher : EnemyBase
{
    [Header("Archer Specific")]
    [SerializeField] Transform     shootPoint;
    [SerializeField] GameObject    arrowPrefab;
    [SerializeField] float         preferredRange       = 14f;   // Lieblingsabstand
    [SerializeField] float         minRange             = 6f;    // Kommt nicht näher
    [SerializeField] float         pyrosTargetChance    = 0.35f; // 35% Chance auf Pyros
    [SerializeField] float         petrifyChance        = 0.08f; // 8% Chance Spieler kurz zu verlangsamen

    bool isRetreating = false;
    int  shotCount    = 0;

    protected override void Awake()
    {
        maxHp          = 55f;
        moveSpeed      = 3.2f;
        damage         = 14f;
        attackCooldown = 2.0f;
        attackRange    = 15f;   // Sehr hohe Angriffs-Reichweite
        xpReward       = 22f;
        ashDropMin     = 2;
        ashDropMax     = 4;
        oreDropChance  = 0.07f;
        prioritizePyros = false; // Eigene Target-Logik
        base.Awake();
    }

    protected override void Update()
    {
        if (isDead || isStunned) return;

        // Abstand zum Spieler halten
        if (playerTransform != null)
        {
            float distToPlayer = Vector3.Distance(transform.position, playerTransform.position);
            isRetreating = distToPlayer < minRange;

            if (isRetreating)
            {
                // Rückwärtsbewegen vom Spieler
                Vector3 retreatDir = (transform.position - playerTransform.position).normalized;
                retreatDir.y = 0f;
                if (agent != null && agent.isOnNavMesh)
                    agent.SetDestination(transform.position + retreatDir * 4f);
            }
        }

        // Target-Wahl: Pyros oder Spieler
        ChooseArcherTarget();
        HandleAttack();

        // Normale Bewegung wenn nicht am Rückzug
        if (!isRetreating && target != null)
        {
            float distToTarget = Vector3.Distance(transform.position, target.position);

            if (distToTarget > preferredRange + 2f)
            {
                // Näher rangehen
                if (agent != null && agent.isOnNavMesh)
                    agent.SetDestination(target.position);
            }
            else if (distToTarget < preferredRange - 2f)
            {
                // Zurückweichen
                Vector3 backDir = (transform.position - target.position).normalized;
                if (agent != null && agent.isOnNavMesh)
                    agent.SetDestination(transform.position + backDir * 3f);
            }
            else
            {
                // Stehen bleiben, zielen
                if (agent != null) agent.ResetPath();
                FaceTarget();
            }
        }

        // Gravity via CharacterController (in EnemyBase via move_and_slide equivalent)
        if (!agent.isOnNavMesh) return;
    }

    void ChooseArcherTarget()
    {
        // Abwechselnd Pyros und Spieler anvisieren
        if (pyrosTransform != null && playerTransform != null)
        {
            // Alle 3 Schüsse wechselt das Ziel
            if (shotCount % 3 == 0 && Random.value < pyrosTargetChance)
                target = pyrosTransform;
            else
                target = playerTransform;
        }
        else
        {
            target = pyrosTransform ?? playerTransform;
        }
    }

    void FaceTarget()
    {
        if (target == null) return;
        Vector3 dir = (target.position - transform.position);
        dir.y = 0f;
        if (dir.magnitude > 0.01f)
            transform.rotation = Quaternion.LookRotation(dir);
    }

    // ── Schuss-Logik ───────────────────────────────────────────────────────
    void HandleAttack()
    {
        // attackTimer kommt aus EnemyBase via Update() → HandleAttack()
        // Hier überschreiben wir die Attack-Logik komplett
    }

    protected override void DealDamage()
    {
        if (target == null) return;
        FaceTarget();

        shotCount++;
        StartCoroutine(ShootArrow());

        // Petrify-Chance (Verlangsamung als "Medusa-Blick")
        if (target == playerTransform && Random.value < petrifyChance)
        {
            var player = playerTransform.GetComponent<PlayerController>();
            // Kurze Verlangsamung via PlayerState
            StartCoroutine(PetrifyPlayer());
        }
    }

    IEnumerator ShootArrow()
    {
        if (arrowPrefab == null || target == null) yield break;

        Vector3 origin = shootPoint != null ? shootPoint.position : transform.position + Vector3.up;
        Vector3 targetPos = target.position + Vector3.up * 0.5f;

        var arrow = Instantiate(arrowPrefab, origin, Quaternion.identity);
        var proj  = arrow.GetComponent<ProjectileBase>();

        if (proj != null)
        {
            proj.Initialize(targetPos - origin, damage, "enemy_arrow");
        }
        else
        {
            // Fallback: direkter Schaden ohne Projektil-Physik
            yield return new WaitForSeconds(0.3f);
            if (target != null)
            {
                if (target.CompareTag("Player"))
                    target.GetComponent<PlayerController>()?.TakeDamage(damage);
                else if (target.CompareTag("Pyros"))
                    target.GetComponent<Pyros>()?.TakeDamage(damage);
            }
            Destroy(arrow);
        }
    }

    IEnumerator PetrifyPlayer()
    {
        var ps = PlayerState.Instance;
        float origSpeed = ps.moveSpeed;
        ps.moveSpeed = origSpeed * 0.4f;
        yield return new WaitForSeconds(1.5f);
        if (ps != null) ps.moveSpeed = origSpeed;
    }
}
