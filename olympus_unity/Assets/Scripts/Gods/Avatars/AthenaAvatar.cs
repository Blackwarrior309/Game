// AthenaAvatar.cs
// Ablegen in: Assets/Scripts/Gods/Avatars/AthenaAvatar.cs
// Athena-Avatar: strategischer Beschützer — heilt den Pyros und schwächt
// Feinde in der Nähe. Stub-Verhalten; volle KI in P3-06.

using UnityEngine;

public class AthenaAvatar : AvatarBase
{
    [Header("Athena-Special: Pyros-Schutz")]
    [SerializeField] float pyrosHealAmount = 25f;
    [SerializeField] float wisdomBurstRadius = 5f;
    [SerializeField] float wisdomBurstDamage = 40f;

    protected override void Awake()
    {
        GodId          = FavorManager.God.Athena;
        moveSpeed      = 5f;
        damage         = 40f;
        attackCooldown = 0.9f;
        specialCooldown = 6f;
        base.Awake();
    }

    protected override void DoSpecialAttack()
    {
        // Platzhalter: Pyros heilen + sanfte AoE auf Feinde im Umkreis.
        // P3-06 ergänzt Pyros-Barriere, Turm-Buff, Strategos-Schild.
        var pyros = GameObject.FindGameObjectWithTag("Pyros")?.GetComponent<Pyros>();
        pyros?.Heal(pyrosHealAmount);

        if (currentTarget != null)
        {
            Collider[] hits = Physics.OverlapSphere(currentTarget.transform.position,
                wisdomBurstRadius, LayerMask.GetMask("Enemy"));
            foreach (var hit in hits)
                hit.GetComponent<EnemyBase>()?.TakeDamage(wisdomBurstDamage);
        }
    }
}
