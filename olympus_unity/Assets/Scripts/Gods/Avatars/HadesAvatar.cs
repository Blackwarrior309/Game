// HadesAvatar.cs
// Ablegen in: Assets/Scripts/Gods/Avatars/HadesAvatar.cs
// Hades-Avatar: Herr der Schatten. Beim Spawn werden alle aktiven Schatten
// permanent (über ShadowAllySpawner.MakeShadowsPermanent) — sie sterben
// nicht mehr von alleine, solange der Avatar lebt. Im Spezial werden
// pro Cast neue Schatten beschworen, sodass die Schatten-Armee anwächst.

using UnityEngine;

public class HadesAvatar : AvatarBase
{
    [Header("Hades-Special: Schatten-Beschwörung")]
    [SerializeField] int   shadowsPerCast    = 3;
    [SerializeField] float shadowSpawnRadius = 6f;

    protected override void Awake()
    {
        GodId           = FavorManager.God.Hades;
        moveSpeed       = 5.5f;
        damage          = 45f;
        attackCooldown  = 0.8f;
        specialCooldown = 5f;
        base.Awake();
    }

    protected override void Start()
    {
        base.Start();

        // Alle bereits aktiven Schatten werden für die Avatar-Lebenszeit
        // permanent. Neue Schatten, die der Avatar beschwört, sind ebenfalls
        // dauerhaft (siehe SetPermanent unten).
        var spawner = FindObjectOfType<ShadowAllySpawner>();
        spawner?.MakeShadowsPermanent();
    }

    protected override void DoSpecialAttack()
    {
        // Beschwöre neue Schatten um den Avatar — gehen über GameEvents an
        // den ShadowAllySpawner, der dann die normale Lifetime setzt. Danach
        // erneut alle als permanent markieren, damit auch die Neuen bleiben.
        for (int i = 0; i < shadowsPerCast; i++)
        {
            Vector2 r = Random.insideUnitCircle * shadowSpawnRadius;
            Vector3 spawnPos = transform.position + new Vector3(r.x, 0f, r.y);
            GameEvents.RaiseSpawnShadowAlly(spawnPos);
        }

        var spawner = FindObjectOfType<ShadowAllySpawner>();
        spawner?.MakeShadowsPermanent();
    }
}
