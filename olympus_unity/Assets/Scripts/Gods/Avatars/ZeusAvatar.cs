// ZeusAvatar.cs
// Ablegen in: Assets/Scripts/Gods/Avatars/ZeusAvatar.cs
// Zeus-Avatar: schneller, blitzschleudernder Donnergott.
// Stub-Verhalten — die volle KI (Kettenblitze, Donnerschlag, Aufstieg in
// die Wolken etc.) wird in P3-05 (Zeus voll) ergänzt.

using UnityEngine;

public class ZeusAvatar : AvatarBase
{
    [Header("Zeus-Special: Blitz-AoE")]
    [SerializeField] float lightningRadius = 6f;
    [SerializeField] float lightningDamage = 80f;

    protected override void Awake()
    {
        GodId          = FavorManager.God.Zeus;
        moveSpeed      = 7f;
        damage         = 60f;
        attackCooldown = 0.7f;
        specialCooldown = 4f;
        base.Awake();
    }

    protected override void DoSpecialAttack()
    {
        // Platzhalter: AoE-Blitz auf alle Feinde im Umkreis.
        // P3-05 ergänzt Kettenreaktion, Sky-Fire-Synergie etc.
        if (currentTarget == null) return;
        Vector3 center = currentTarget.transform.position;

        Collider[] hits = Physics.OverlapSphere(center, lightningRadius,
            LayerMask.GetMask("Enemy"));
        foreach (var hit in hits)
        {
            var e = hit.GetComponent<EnemyBase>();
            if (e == null) continue;
            e.TakeDamage(lightningDamage);
            e.SetMeta("killed_by_lightning", true);
        }
    }
}
