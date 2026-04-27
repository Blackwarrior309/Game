// PoseidonAvatar.cs
// Ablegen in: Assets/Scripts/Gods/Avatars/PoseidonAvatar.cs
// Poseidon-Avatar: Meeresgott. Spezial = Flutwelle (Radial) + extreme
// Vorwärts-Slow-Wand, die quasi als kurzlebige "Wassermauer" wirkt.

using UnityEngine;

public class PoseidonAvatar : AvatarBase
{
    [Header("Flutwelle (Radial)")]
    [SerializeField] float waveRadius     = 9f;
    [SerializeField] float waveDamage     = 35f;
    [SerializeField] float waveSlowFactor = 0.4f;
    [SerializeField] float waveSlowDur    = 4f;

    [Header("Wassermauer (Vorwärts-Kegel)")]
    [SerializeField] float wallRange      = 12f;
    [SerializeField] float wallHalfAngle  = 35f;     // Kegel-Halbwinkel in °
    [SerializeField] float wallSlowFactor = 0.15f;   // 85 % Slow → effektive Mauer
    [SerializeField] float wallSlowDur    = 6f;

    protected override void Awake()
    {
        GodId           = FavorManager.God.Poseidon;
        moveSpeed       = 6f;
        damage          = 50f;
        attackCooldown  = 0.8f;
        specialCooldown = 5f;
        base.Awake();
    }

    protected override void DoSpecialAttack()
    {
        // 1) Radial-Flutwelle ─────────────────────────────────────────────
        Collider[] radial = Physics.OverlapSphere(transform.position, waveRadius,
            LayerMask.GetMask("Enemy"));
        foreach (var hit in radial)
        {
            var e = hit.GetComponent<EnemyBase>();
            if (e == null) continue;
            e.TakeDamage(waveDamage);
            e.ApplySlow(waveSlowFactor, waveSlowDur);
        }

        // 2) Wassermauer: Vorwärts-Kegel mit extremer Slow ────────────────
        Collider[] cone = Physics.OverlapSphere(transform.position, wallRange,
            LayerMask.GetMask("Enemy"));
        Vector3 fwd = transform.forward;
        float cosLimit = Mathf.Cos(wallHalfAngle * Mathf.Deg2Rad);
        foreach (var hit in cone)
        {
            var e = hit.GetComponent<EnemyBase>();
            if (e == null) continue;
            Vector3 toEnemy = (hit.transform.position - transform.position).normalized;
            if (Vector3.Dot(fwd, toEnemy) >= cosLimit)
                e.ApplySlow(wallSlowFactor, wallSlowDur);
        }
    }
}
