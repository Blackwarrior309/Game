// ZeusAvatar.cs
// Ablegen in: Assets/Scripts/Gods/Avatars/ZeusAvatar.cs
// Zeus-Avatar: schneller Donnergott mit Kettenblitz-Spezial.
// Der Spezialangriff springt vom Hauptziel auf bis zu N weitere Feinde im
// Sprungradius, mit fallendem Schaden pro Stufe. Markiert alle Treffer als
// "killed_by_lightning" für Underworld-Storm-Synergie und triggert
// Storm-Flood-Slow falls aktiv.

using UnityEngine;
using System.Collections.Generic;

public class ZeusAvatar : AvatarBase
{
    [Header("Zeus-Special: Kettenblitz")]
    [SerializeField] int   chainBounces = 4;
    [SerializeField] float chainRange   = 5f;
    [SerializeField] float chainDamage  = 80f;
    [SerializeField] float chainFalloff = 0.6f;     // Schaden × Falloff pro Sprung

    protected override void Awake()
    {
        GodId           = FavorManager.God.Zeus;
        moveSpeed       = 7f;
        damage          = 60f;
        attackCooldown  = 0.7f;
        specialCooldown = 4f;
        base.Awake();
    }

    protected override void DoSpecialAttack()
    {
        if (currentTarget == null) return;

        var visited = new HashSet<EnemyBase>();
        EnemyBase node = currentTarget;
        float dmg = chainDamage;

        for (int i = 0; i <= chainBounces && node != null; i++)
        {
            visited.Add(node);
            node.TakeDamage(dmg);
            node.SetMeta("killed_by_lightning", true);

            if (SynergySystem.Instance.IsActive("storm_flood"))
                node.ApplySlow(0.7f, 3f);

            node = FindNextChainTarget(node.transform.position, visited);
            dmg *= chainFalloff;
        }
    }

    EnemyBase FindNextChainTarget(Vector3 origin, HashSet<EnemyBase> visited)
    {
        Collider[] hits = Physics.OverlapSphere(origin, chainRange, LayerMask.GetMask("Enemy"));
        EnemyBase closest = null;
        float closestDist = float.MaxValue;
        foreach (var hit in hits)
        {
            var e = hit.GetComponent<EnemyBase>();
            if (e == null || e.isDead || visited.Contains(e)) continue;
            float d = Vector3.Distance(origin, e.transform.position);
            if (d < closestDist) { closestDist = d; closest = e; }
        }
        return closest;
    }
}
