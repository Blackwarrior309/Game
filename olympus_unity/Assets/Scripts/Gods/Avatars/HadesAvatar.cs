// HadesAvatar.cs
// Ablegen in: Assets/Scripts/Gods/Avatars/HadesAvatar.cs
// Hades-Avatar: Herr der Schatten — beschwört Massen-Schattenkrieger und
// kämpft selbst zwischen ihnen. Stub-Verhalten; volle KI (alle Schatten
// permanent für die Avatar-Dauer, Seelen-Tor) in P3-09.

using UnityEngine;

public class HadesAvatar : AvatarBase
{
    [Header("Hades-Special: Schatten-Beschwörung")]
    [SerializeField] int   shadowsPerCast    = 3;
    [SerializeField] float shadowSpawnRadius = 6f;

    protected override void Awake()
    {
        GodId          = FavorManager.God.Hades;
        moveSpeed      = 5.5f;
        damage         = 45f;
        attackCooldown = 0.8f;
        specialCooldown = 5f;
        base.Awake();
    }

    protected override void DoSpecialAttack()
    {
        // Platzhalter: Schatten via GameEvents.RaiseSpawnShadowAlly um den
        // Avatar herum spawnen. P3-09 ergänzt: alle aktiven Schatten werden
        // für die Avatar-Dauer permanent (siehe ShadowAllySpawner.MakeShadowsPermanent).
        for (int i = 0; i < shadowsPerCast; i++)
        {
            Vector2 r = Random.insideUnitCircle * shadowSpawnRadius;
            Vector3 spawnPos = transform.position + new Vector3(r.x, 0f, r.y);
            GameEvents.RaiseSpawnShadowAlly(spawnPos);
        }
    }
}
