// SatyrRunner.cs
// Ablegen in: Assets/Scripts/Enemies/SatyrRunner.cs

using UnityEngine;

public class SatyrRunner : EnemyBase
{
    protected override void Awake()
    {
        maxHp          = 20f;
        moveSpeed      = 6f;
        damage         = 8f;
        attackCooldown = 1.0f;
        attackRange    = 1.2f;
        xpReward       = 8f;
        ashDropMin     = 1;
        ashDropMax     = 2;
        oreDropChance  = 0.03f;
        prioritizePyros = true;
        base.Awake();
    }
}
