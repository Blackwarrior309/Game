// StoneGolem.cs
// Ablegen in: Assets/Scripts/Enemies/StoneGolem.cs
// Langsamer, tankiger Feind — greift Gebäude bevorzugt an
// Zerstört Gebäude sofort im Nahbereich NICHT (das ist der Kyklop)
// Aber hat hohen Gebäudeschaden

using UnityEngine;
using UnityEngine.AI;

public class StoneGolem : EnemyBase
{
    [Header("Golem Specific")]
    [SerializeField] float buildingDetectRadius = 6f;
    [SerializeField] float buildingDamageMultiplier = 2.5f;

    // Gebäude-Targeting
    BuildingBase currentBuildingTarget;
    float buildingScanTimer = 0f;
    const float BuildingScanInterval = 1.5f;

    protected override void Awake()
    {
        maxHp           = 180f;
        moveSpeed       = 1.8f;   // Sehr langsam
        damage          = 18f;
        attackCooldown  = 2.2f;
        attackRange     = 2.0f;
        xpReward        = 30f;
        ashDropMin      = 3;
        ashDropMax      = 7;
        oreDropChance   = 0.12f;
        prioritizePyros = false;  // Gebäude zuerst
        base.Awake();
    }

    protected override void Update()
    {
        if (isDead || isStunned) return;

        buildingScanTimer -= Time.deltaTime;
        if (buildingScanTimer <= 0f)
        {
            buildingScanTimer = BuildingScanInterval;
            ScanForBuildings();
        }

        base.Update();
    }

    // ── Gebäude-Targeting ──────────────────────────────────────────────────
    void ScanForBuildings()
    {
        // Nächstes Gebäude in Radius finden
        Collider[] hits = Physics.OverlapSphere(transform.position, buildingDetectRadius,
            LayerMask.GetMask("Building"));

        BuildingBase closest = null;
        float closestDist = float.MaxValue;

        foreach (var hit in hits)
        {
            var building = hit.GetComponent<BuildingBase>();
            if (building == null || !building.isBuilt) continue;

            float d = Vector3.Distance(transform.position, hit.transform.position);
            if (d < closestDist)
            {
                closestDist = d;
                closest = building;
            }
        }

        currentBuildingTarget = closest;

        // ForcedTarget auf Gebäude setzen wenn vorhanden
        if (currentBuildingTarget != null)
            ForcedTarget = currentBuildingTarget.transform;
        else
            ForcedTarget = null;
    }

    protected override void DealDamage()
    {
        // Gebäude-Schaden
        if (currentBuildingTarget != null &&
            Vector3.Distance(transform.position, currentBuildingTarget.transform.position) <= attackRange)
        {
            currentBuildingTarget.TakeDamage(damage * buildingDamageMultiplier);

            // Stomp-Effekt: kleine Gebäude in Nähe auch treffen
            StompNearbyBuildings();
            return;
        }

        // Fallback: normaler Schaden
        base.DealDamage();
    }

    void StompNearbyBuildings()
    {
        // Erderschütterung — kleine AoE auf Gebäude
        Collider[] hits = Physics.OverlapSphere(transform.position, 2.5f, LayerMask.GetMask("Building"));
        foreach (var hit in hits)
        {
            var b = hit.GetComponent<BuildingBase>();
            if (b != null && b != currentBuildingTarget)
                b.TakeDamage(damage * 0.5f);
        }
    }

    // ── Visuelles: Boden-Staub bei Schritt ─────────────────────────────────
    // (Particle-Effekt hier als Platzhalter-Kommentar)
    // Für echten Effekt: ParticleSystem "DustEffect" als Child hinzufügen
    // und in AnimateStep() triggern
}
