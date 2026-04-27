// PoseidonInterventions.cs
// Ablegen in: Assets/Scripts/Gods/PoseidonInterventions.cs
// Anhängen an: Singletons-GameObject (DontDestroyOnLoad)
//
// Poseidon = Wasser, Erdspaltungen, Slows. Diese Klasse deckt nur die
// beiden Interventionen ab — die Passive (Slow on Hit) liegt bereits
// in EnemyBase.DealDamage und der Tempel-Effekt im Temple-Setup.
//
//   - Intervention 1 (25): Flutwelle — Radial-Welle 8 m um Spieler,
//     40 dmg + 0.5× Slow für 4 s
//   - Intervention 2 (75): Erdspaltung — alle Feinde im 15-m-Radius
//     verlieren 30 % ihrer Max-HP sofort + 0.3× Slow für 5 s
//
// Synergien:
//   - storm_flood    (Poseidon + Zeus):    in ZeusInterventions/Avatar
//   - flood_of_souls (Poseidon + Hades):   Schatten bekommen Speed-Boost
//                                          (in ShadowAlly bereits drin)
//   - lava_sea       (Poseidon + Hephaistos): Erz-Drop +20 %, in EnemyBase
//                                          und PickupBase bereits drin

using UnityEngine;

public class PoseidonInterventions : MonoBehaviour
{
    [Header("Intervention 1: Flutwelle")]
    [SerializeField] float floodRadius   = 8f;
    [SerializeField] float floodDamage   = 40f;
    [SerializeField] float floodSlow     = 0.5f;
    [SerializeField] float floodSlowDur  = 4f;

    [Header("Intervention 2: Erdspaltung")]
    [SerializeField] float quakeRadius           = 15f;
    [SerializeField] float quakeMaxHpFraction    = 0.3f;   // 30 % Max-HP-Schaden
    [SerializeField] float quakeSlow             = 0.3f;
    [SerializeField] float quakeSlowDur          = 5f;

    // ── Unity Lifecycle ────────────────────────────────────────────────────
    void OnEnable()  => FavorManager.OnThresholdReached += HandleThreshold;
    void OnDisable() => FavorManager.OnThresholdReached -= HandleThreshold;

    void HandleThreshold(FavorManager.God god, string key)
    {
        if (god != FavorManager.God.Poseidon) return;
        switch (key)
        {
            case "intervention_1": DoFlutwelle();   break;
            case "intervention_2": DoErdspaltung(); break;
        }
    }

    // ── I1: Flutwelle ──────────────────────────────────────────────────────
    void DoFlutwelle()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        Collider[] hits = Physics.OverlapSphere(player.transform.position, floodRadius,
            LayerMask.GetMask("Enemy"));
        foreach (var hit in hits)
        {
            var e = hit.GetComponent<EnemyBase>();
            if (e == null) continue;
            e.TakeDamage(floodDamage);
            e.ApplySlow(floodSlow, floodSlowDur);
        }
    }

    // ── I2: Erdspaltung ────────────────────────────────────────────────────
    void DoErdspaltung()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        Collider[] hits = Physics.OverlapSphere(player.transform.position, quakeRadius,
            LayerMask.GetMask("Enemy"));
        foreach (var hit in hits)
        {
            var e = hit.GetComponent<EnemyBase>();
            if (e == null) continue;
            // Prozentualer Max-HP-Schaden — Bosse spüren das genauso wie Satyrn.
            e.TakeDamage(e.maxHp * quakeMaxHpFraction);
            e.ApplySlow(quakeSlow, quakeSlowDur);
        }
    }
}
