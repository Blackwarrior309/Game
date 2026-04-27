// AresAvatar.cs
// Ablegen in: Assets/Scripts/Gods/Avatars/AresAvatar.cs
// Ares-Avatar: Berserker — extrem hoher Schaden und Geschwindigkeit, lebt
// für Massaker. Stub-Verhalten; volle KI (Kill-Streak-Speed-Boost,
// Kriegsschrei) in P3-07.

using UnityEngine;

public class AresAvatar : AvatarBase
{
    [Header("Ares-Special: Berserker-Schwung")]
    [SerializeField] float swingRadius = 5f;
    [SerializeField] float swingDamage = 70f;

    protected override void Awake()
    {
        GodId          = FavorManager.God.Ares;
        moveSpeed      = 9f;     // schnell wie der Krieg
        damage         = 75f;
        attackCooldown = 0.5f;
        specialCooldown = 3f;
        base.Awake();
    }

    protected override void DoSpecialAttack()
    {
        // Platzhalter: 360°-Schwung um den Avatar.
        // P3-07 ergänzt Kill-Streak, Kriegsschrei, Forced-Target-Aggro.
        Collider[] hits = Physics.OverlapSphere(transform.position, swingRadius,
            LayerMask.GetMask("Enemy"));
        foreach (var hit in hits)
            hit.GetComponent<EnemyBase>()?.TakeDamage(swingDamage);
    }
}
