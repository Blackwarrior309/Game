// PoseidonAvatar.cs
// Ablegen in: Assets/Scripts/Gods/Avatars/PoseidonAvatar.cs
// Poseidon-Avatar: kontrolliert Wasser und Erdspaltungen — Flutwellen
// fegen Feinde fort, alles in der Nähe wird verlangsamt.
// Stub-Verhalten; volle KI (Flutwelle-Linie, Wasserwände) in P3-08.

using UnityEngine;

public class PoseidonAvatar : AvatarBase
{
    [Header("Poseidon-Special: Flutwelle")]
    [SerializeField] float waveRadius     = 9f;
    [SerializeField] float waveDamage     = 35f;
    [SerializeField] float waveSlowFactor = 0.4f;
    [SerializeField] float waveSlowDur    = 4f;

    protected override void Awake()
    {
        GodId          = FavorManager.God.Poseidon;
        moveSpeed      = 6f;
        damage         = 50f;
        attackCooldown = 0.8f;
        specialCooldown = 5f;
        base.Awake();
    }

    protected override void DoSpecialAttack()
    {
        // Platzhalter: Radial-Flutwelle um den Avatar — Schaden + starker Slow.
        // P3-08 ergänzt gerichtete Linien-Welle und Wassermauern.
        Collider[] hits = Physics.OverlapSphere(transform.position, waveRadius,
            LayerMask.GetMask("Enemy"));
        foreach (var hit in hits)
        {
            var e = hit.GetComponent<EnemyBase>();
            if (e == null) continue;
            e.TakeDamage(waveDamage);
            e.ApplySlow(waveSlowFactor, waveSlowDur);
        }
    }
}
