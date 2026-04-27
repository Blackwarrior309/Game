// AresAvatar.cs
// Ablegen in: Assets/Scripts/Gods/Avatars/AresAvatar.cs
// Ares-Avatar: Berserker-Form. Bei Spawn werden alle aktuellen Feinde im
// 25 m-Radius auf den Avatar aggro'd (ForcedTarget) — der Avatar zieht sie
// vom Spieler weg. Spezial = 360°-Schwung; jeder Avatar-Kill stackt einen
// kurzlebigen Eigenschaden-Boost (intern, separat von der Passiv-Kette).

using UnityEngine;
using System.Collections.Generic;

public class AresAvatar : AvatarBase
{
    [Header("Berserker-Schwung")]
    [SerializeField] float swingRadius = 5f;
    [SerializeField] float swingDamage = 70f;

    [Header("Kill-Streak (Avatar-intern)")]
    [SerializeField] float killStreakBonus  = 0.10f;   // pro Kill +10 % bis next Special
    [SerializeField] int   killStreakMax    = 5;

    [Header("Aggro-Pull")]
    [SerializeField] float aggroPullRadius  = 25f;
    [SerializeField] float aggroPullDuration = 30f;    // ~Avatar-Lebenszeit

    int killStreak = 0;
    HashSet<EnemyBase> aggroedEnemies = new();

    protected override void Awake()
    {
        GodId           = FavorManager.God.Ares;
        moveSpeed       = 9f;
        damage          = 75f;
        attackCooldown  = 0.5f;
        specialCooldown = 3f;
        base.Awake();
    }

    protected override void Start()
    {
        base.Start();
        PullAggroOnSpawn();
        GameEvents.OnEnemyKilled += OnAnyEnemyKilled;
    }

    void OnDestroy()
    {
        GameEvents.OnEnemyKilled -= OnAnyEnemyKilled;
        // Aggro freigeben falls noch Feinde übrig
        foreach (var e in aggroedEnemies)
            if (e != null) e.ForcedTarget = null;
    }

    // ── Aggro auf den Avatar ziehen ────────────────────────────────────────
    void PullAggroOnSpawn()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, aggroPullRadius,
            LayerMask.GetMask("Enemy"));
        foreach (var hit in hits)
        {
            var e = hit.GetComponent<EnemyBase>();
            if (e == null) continue;
            e.ForcedTarget = transform;
            aggroedEnemies.Add(e);
        }
    }

    // ── Kill-Streak im Avatar ──────────────────────────────────────────────
    // Wir können nicht eindeutig sagen, wer den Kill gemacht hat — wir nehmen
    // an: jeder Kill in der Avatar-Lebenszeit zählt für den Streak. Decay nicht
    // nötig, weil der Avatar nach 30 s sowieso despawnt und das Counter-Reset
    // mitnimmt.
    void OnAnyEnemyKilled(GameObject _, Vector3 __)
    {
        killStreak = Mathf.Min(killStreakMax, killStreak + 1);
    }

    // ── Standard-Schaden mit Streak-Boost ──────────────────────────────────
    protected override void DoAttack()
    {
        if (currentTarget == null) return;
        float final = damage * (1f + killStreak * killStreakBonus);
        currentTarget.TakeDamage(final);
    }

    // ── Spezial: 360°-Schwung mit Streak-Boost ────────────────────────────
    protected override void DoSpecialAttack()
    {
        float final = swingDamage * (1f + killStreak * killStreakBonus);
        Collider[] hits = Physics.OverlapSphere(transform.position, swingRadius,
            LayerMask.GetMask("Enemy"));
        foreach (var hit in hits)
            hit.GetComponent<EnemyBase>()?.TakeDamage(final);
    }
}
