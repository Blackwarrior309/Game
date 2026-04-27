// AttackBuildings.cs
// Ablegen in: Assets/Scripts/Buildings/AttackBuildings.cs
// Enthält: ArcherTower, Catapult, FireTower

using UnityEngine;
using System.Collections;

// ─────────────────────────────────────────────────────────────────────────────
// TurretBase — gemeinsame Logik für alle Angriffs-Türme
// ─────────────────────────────────────────────────────────────────────────────
public abstract class TurretBase : BuildingBase
{
    [Header("Turret")]
    [SerializeField] protected Transform  turretHead;       // Dreht sich zum Ziel
    [SerializeField] protected Transform  shootPoint;
    [SerializeField] protected float      detectionRadius = 12f;
    [SerializeField] protected float      fireRate        = 1f;   // Schüsse pro Sekunde
    [SerializeField] protected float      turretDamage    = 15f;
    [SerializeField] protected LayerMask  enemyLayer;

    protected EnemyBase  currentTarget;
    protected float      fireTimer = 0f;
    protected bool       isFiring  = false;

    // Athena-Intervention: doppelte Feuerrate für 15s
    float fireRateMultiplier = 1f;

    protected override void ApplyEffects()
    {
        // Athena-Tempel-Effekt: +30% HP + automatisch Stufe 1 Upgrade
        if (FavorManager.Instance.IsTempleBuilt(FavorManager.God.Athena))
        {
            maxHp *= 1.3f;
            hp     = maxHp;
            turretDamage *= 1.15f;  // Stufe-1-Upgrade-Äquivalent
        }

        // Prometheus-Feuer-Artefakt: Türme +20% Schaden
        // (über PlayerState-Meta oder separaten ArtifactManager)
        StartCoroutine(TurretLoop());
    }

    // ── Haupt-Loop ──────────────────────────────────────────────────────────
    IEnumerator TurretLoop()
    {
        while (isBuilt && !isDestroyed)
        {
            UpdateTarget();

            if (currentTarget != null)
            {
                // Turret-Kopf drehen
                if (turretHead != null)
                {
                    Vector3 dir = (currentTarget.transform.position - turretHead.position).normalized;
                    dir.y = 0f;
                    if (dir != Vector3.zero)
                        turretHead.rotation = Quaternion.Slerp(
                            turretHead.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 8f);
                }

                fireTimer -= Time.deltaTime;
                if (fireTimer <= 0f)
                {
                    fireTimer = 1f / (fireRate * fireRateMultiplier);
                    Fire();
                }
            }

            yield return null;
        }
    }

    bool isDestroyed = false;

    void OnDestroy() => isDestroyed = true;

    void UpdateTarget()
    {
        // Toter oder zu weit entfernter Feind → neu suchen
        if (currentTarget == null || currentTarget.isDead ||
            Vector3.Distance(transform.position, currentTarget.transform.position) > detectionRadius + 2f)
        {
            currentTarget = FindNearestEnemy();
        }
    }

    EnemyBase FindNearestEnemy()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, detectionRadius, enemyLayer);
        EnemyBase closest  = null;
        float     closeDist = float.MaxValue;

        foreach (var hit in hits)
        {
            var e = hit.GetComponent<EnemyBase>();
            if (e == null || e.isDead) continue;
            float d = Vector3.Distance(transform.position, e.transform.position);
            if (d < closeDist) { closeDist = d; closest = e; }
        }
        return closest;
    }

    // Athena-Intervention: doppelte Feuerrate
    public void SetFireRateMultiplier(float mult, float duration)
    {
        StartCoroutine(FireRateBoost(mult, duration));
    }

    IEnumerator FireRateBoost(float mult, float duration)
    {
        fireRateMultiplier = mult;
        yield return new WaitForSeconds(duration);
        fireRateMultiplier = 1f;
    }

    protected abstract void Fire();
}

// ─────────────────────────────────────────────────────────────────────────────
// ArcherTower — Schneller Einzelziel-Turm
// ─────────────────────────────────────────────────────────────────────────────
public class ArcherTower : TurretBase
{
    [Header("Archer Tower")]
    [SerializeField] GameObject arrowPrefab;

    protected override void Awake()
    {
        buildingType    = "archer_tower";
        maxHp           = 120f;
        ashCost         = 60;
        buildTime       = 3f;
        detectionRadius = 14f;
        fireRate        = 1.5f;   // 1.5 Schüsse/s
        turretDamage    = 12f;
        base.Awake();
    }

    protected override void Fire()
    {
        if (currentTarget == null || arrowPrefab == null) return;

        Vector3 origin = shootPoint != null ? shootPoint.position
                                            : transform.position + Vector3.up * 1.5f;
        Vector3 dir    = (currentTarget.transform.position + Vector3.up * 0.5f - origin).normalized;

        var arrow = Instantiate(arrowPrefab, origin, Quaternion.LookRotation(dir));
        var proj  = arrow.GetComponent<ProjectileBase>();

        float finalDamage = turretDamage;

        // Prometheus-Feuer-Artefakt: +20% Turm-Schaden
        // finalDamage *= ArtifactManager.Instance.TurretDamageMultiplier;

        proj?.Initialize(dir, finalDamage, "player");  // "player" = schadet Feinden

        Destroy(arrow, 4f);
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// Catapult — Langsamer AoE-Turm (Steinschleuder)
// ─────────────────────────────────────────────────────────────────────────────
public class Catapult : TurretBase
{
    [Header("Catapult")]
    [SerializeField] GameObject boulderPrefab;
    [SerializeField] float      aoeRadius = 4f;
    [SerializeField] float      boulderArcHeight = 8f;

    protected override void Awake()
    {
        buildingType    = "catapult";
        maxHp           = 150f;
        ashCost         = 90;
        buildTime       = 4f;
        detectionRadius = 18f;   // Große Reichweite
        fireRate        = 0.4f;  // Langsam: 1 Schuss alle 2.5s
        turretDamage    = 35f;   // Aber hoher AoE-Schaden
        base.Awake();
    }

    protected override void Fire()
    {
        if (currentTarget == null) return;
        StartCoroutine(LaunchBoulder());
    }

    IEnumerator LaunchBoulder()
    {
        if (boulderPrefab == null)
        {
            // Fallback: direkte AoE ohne Projektil
            yield return new WaitForSeconds(0.8f);
            DealAoEAtPosition(currentTarget != null ? currentTarget.transform.position : transform.position);
            yield break;
        }

        Vector3 startPos  = shootPoint != null ? shootPoint.position : transform.position + Vector3.up * 2f;
        Vector3 targetPos = currentTarget.transform.position;

        var boulder = Instantiate(boulderPrefab, startPos, Quaternion.identity);
        float t = 0f;
        float duration = 1.2f;

        while (t < duration && boulder != null)
        {
            t += Time.deltaTime;
            float ratio = t / duration;
            // Parabel-Bogen
            Vector3 pos = Vector3.Lerp(startPos, targetPos, ratio);
            pos.y += Mathf.Sin(ratio * Mathf.PI) * boulderArcHeight;
            boulder.transform.position = pos;
            yield return null;
        }

        if (boulder != null)
        {
            DealAoEAtPosition(targetPos);
            Destroy(boulder);
        }
    }

    void DealAoEAtPosition(Vector3 pos)
    {
        Collider[] hits = Physics.OverlapSphere(pos, aoeRadius, Physics.AllLayers);
        foreach (var hit in hits)
        {
            var enemy = hit.GetComponent<EnemyBase>();
            if (enemy != null) enemy.TakeDamage(turretDamage);
        }

        // Visuellen Impact-Effekt spawnen (Partikel)
        // Instantiate(impactFXPrefab, pos, Quaternion.identity);
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// FireTower — Feuer-Turm, Flächenschaden + DoT
// ─────────────────────────────────────────────────────────────────────────────
public class FireTower : TurretBase
{
    [Header("Fire Tower")]
    [SerializeField] float fireAoeRadius = 3f;
    [SerializeField] float dotDamage     = 4f;   // Schaden pro Sekunde
    [SerializeField] float dotDuration   = 3f;
    [SerializeField] ParticleSystem flameFX;

    protected override void Awake()
    {
        buildingType    = "fire_tower";
        maxHp           = 100f;
        ashCost         = 80;
        buildTime       = 3.5f;
        detectionRadius = 10f;   // Kürzere Reichweite als Archer
        fireRate        = 0.8f;
        turretDamage    = 8f;    // Wenig Direktschaden, dafür DoT
        base.Awake();
    }

    protected override void ApplyEffects()
    {
        base.ApplyEffects();
        if (flameFX != null) flameFX.Play();
    }

    protected override void Fire()
    {
        if (currentTarget == null) return;

        // AoE um Ziel
        Collider[] hits = Physics.OverlapSphere(
            currentTarget.transform.position, fireAoeRadius, LayerMask.GetMask("Enemy"));

        foreach (var hit in hits)
        {
            var enemy = hit.GetComponent<EnemyBase>();
            if (enemy == null) continue;

            // Direktschaden
            enemy.TakeDamage(turretDamage);

            // Feuer-DoT
            enemy.StartCoroutine(FireDoT(enemy));

            // Hephaistos-Passiv: +10% Feuerschaden als Bonus-DoT
            if (FavorManager.Instance.IsPassiveActive(FavorManager.God.Hephaistos))
                enemy.StartCoroutine(FireDoT(enemy, dotDamage * 0.1f, 2f));
        }
    }

    IEnumerator FireDoT(EnemyBase enemy, float dmgPerSec = -1f, float dur = -1f)
    {
        float dps      = dmgPerSec < 0 ? dotDamage    : dmgPerSec;
        float duration = dur       < 0 ? dotDuration  : dur;
        float elapsed  = 0f;

        while (elapsed < duration && enemy != null && !enemy.isDead)
        {
            elapsed += Time.deltaTime;
            enemy.TakeDamage(dps * Time.deltaTime);
            yield return null;
        }
    }
}
