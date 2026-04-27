// ProjectileBase.cs
// Ablegen in: Assets/Scripts/Combat/ProjectileBase.cs
// Wiederverwendbares Projektil-Script für Pfeile, Würfe, Boss-Attacken
// Anhängen an: GameObject mit Collider (isTrigger) + Rigidbody (isKinematic)

using UnityEngine;

public class ProjectileBase : MonoBehaviour
{
    [Header("Base Settings")]
    [SerializeField] float speed        = 18f;
    [SerializeField] float lifetime     = 4f;
    [SerializeField] float aoeRadius    = 0f;    // 0 = kein AoE
    [SerializeField] bool  returnToOwner = false; // Dreizack des Meeresgottes

    // Laufzeit-Werte (gesetzt via Initialize)
    float  damage;
    string ownerTag;   // "player" oder "enemy_arrow"
    Vector3 direction;
    bool initialized = false;
    float lifetimeTimer;

    // Für Return-Projekile
    Transform owner;
    bool returning = false;

    void Update()
    {
        if (!initialized) return;

        lifetimeTimer -= Time.deltaTime;
        if (lifetimeTimer <= 0f) { Destroy(gameObject); return; }

        // Rückflug (Dreizack)
        if (returnToOwner && returning && owner != null)
        {
            direction = (owner.position - transform.position).normalized;
            float dist = Vector3.Distance(transform.position, owner.position);
            if (dist < 0.5f) { Destroy(gameObject); return; }
        }

        transform.position += direction * speed * Time.deltaTime;
        if (direction != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(direction);
    }

    // ── Initialisierung ────────────────────────────────────────────────────
    public void Initialize(Vector3 dir, float dmg, string tag, Transform ownerTransform = null)
    {
        direction   = dir.normalized;
        damage      = dmg;
        ownerTag    = tag;
        owner       = ownerTransform;
        lifetimeTimer = lifetime;
        initialized = true;

        // Return-Mechanik: nach halber Lifetime umkehren
        if (returnToOwner && owner != null)
            Invoke(nameof(StartReturn), lifetime * 0.45f);
    }

    void StartReturn()
    {
        returning = true;
    }

    // ── Kollision ─────────────────────────────────────────────────────────
    void OnTriggerEnter(Collider other)
    {
        if (!initialized) return;

        bool hitEnemy  = other.CompareTag("Enemy")    && ownerTag == "player";
        bool hitPlayer = other.CompareTag("Player")   && ownerTag.StartsWith("enemy");
        bool hitPyros  = other.CompareTag("Pyros")    && ownerTag.StartsWith("enemy");
        bool hitBuilding = other.CompareTag("Building") && ownerTag.StartsWith("enemy");

        if (!hitEnemy && !hitPlayer && !hitPyros && !hitBuilding) return;

        if (aoeRadius > 0f)
            DealAoeDamage();
        else
            DealDirectDamage(other);

        // Return-Projektile zerstören sich nicht beim ersten Treffer (optional)
        if (!returnToOwner)
            Destroy(gameObject);
    }

    void DealDirectDamage(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            var enemy = other.GetComponent<EnemyBase>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage);

                // Poseidon-Passiv: Pfeile verlangsamen (via Synergie oder Waffe)
                if (ownerTag == "player_trident")
                    enemy.ApplySlow(0.6f, 2f);
            }
        }
        else if (other.CompareTag("Player"))
        {
            other.GetComponent<PlayerController>()?.TakeDamage(damage);
        }
        else if (other.CompareTag("Pyros"))
        {
            other.GetComponent<Pyros>()?.TakeDamage(damage);
        }
        else if (other.CompareTag("Building"))
        {
            other.GetComponent<BuildingBase>()?.TakeDamage(damage);
        }
    }

    void DealAoeDamage()
    {
        // AoE-Schaden (z.B. Vulkanhammer, Wurfspeer-Aufprall)
        string targetLayer = ownerTag == "player"
            ? "Enemy"
            : "Player";

        Collider[] hits = Physics.OverlapSphere(transform.position, aoeRadius,
            LayerMask.GetMask(targetLayer, "Pyros", "Building"));

        foreach (var hit in hits)
        {
            if (hit.CompareTag("Enemy"))
                hit.GetComponent<EnemyBase>()?.TakeDamage(damage);
            else if (hit.CompareTag("Pyros"))
                hit.GetComponent<Pyros>()?.TakeDamage(damage * 0.5f);
            else if (hit.CompareTag("Building"))
                hit.GetComponent<BuildingBase>()?.TakeDamage(damage * 0.7f);
        }

        // Himmelsfeuer-Synergie: Blitz-Treffer hinterlässt Feuerpfütze
        if (SynergySystem.Instance.IsActive("sky_fire") && ownerTag == "zeus_lightning")
            SpawnFirePuddle(transform.position);
    }

    void SpawnFirePuddle(Vector3 pos)
    {
        // FirePuddle-Prefab instanziieren (aus Resources oder via GameManager)
        var puddle = Resources.Load<GameObject>("Prefabs/Effects/FirePuddle");
        if (puddle != null)
            Instantiate(puddle, pos, Quaternion.identity);
    }

    // ── Getter für externe Systeme ─────────────────────────────────────────
    public void SetReturning(bool val) => returning = val;
    public float Damage => damage;
}
