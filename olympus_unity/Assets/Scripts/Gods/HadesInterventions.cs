// HadesInterventions.cs
// Ablegen in: Assets/Scripts/Gods/HadesInterventions.cs
// Anhängen an: Singletons-GameObject (DontDestroyOnLoad)
//
// Hades = Schatten + Unterwelt. Passive (15 % Schatten-Spawn pro Kill)
// liegt bereits in EnemyBase.Die(); Tempel-Auto-Schatten alle 45 s in
// Temple.HadesAutoShadow. Diese Klasse füllt die Interventionen:
//
//   - Intervention 1 (25): Massen-Beschwörung — sofort N Schatten
//     spawnen via ShadowAllySpawner.SummonShadowsFromDeadEnemies.
//   - Intervention 2 (75): Seelen-Sog — alle Feinde im Spieler-Radius
//     verlieren 30 % Max-HP und werden für 4 s zum Spieler gezogen.
//
// Synergien:
//   - underworld_storm (Hades + Zeus):  in ZeusInterventions/Avatar
//   - flood_of_souls   (Hades + Poseidon): Schatten-Speed-Boost in ShadowAlly
//   - soul_forge       (Hades + Hephaistos): Schatten tragen Waffenkopie,
//                                            in ShadowAlly bereits drin

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class HadesInterventions : MonoBehaviour
{
    [Header("Intervention 1: Massen-Beschwörung")]
    [SerializeField] int massSummonCount = 5;

    [Header("Intervention 2: Seelen-Sog")]
    [SerializeField] float vacuumRadius          = 18f;
    [SerializeField] float vacuumPullSpeed       = 8f;
    [SerializeField] float vacuumDuration        = 4f;
    [SerializeField] float vacuumMaxHpFraction   = 0.3f;

    // ── Unity Lifecycle ────────────────────────────────────────────────────
    void OnEnable()  => FavorManager.OnThresholdReached += HandleThreshold;
    void OnDisable() => FavorManager.OnThresholdReached -= HandleThreshold;

    void HandleThreshold(FavorManager.God god, string key)
    {
        if (god != FavorManager.God.Hades) return;
        switch (key)
        {
            case "intervention_1": DoMassSummon();              break;
            case "intervention_2": StartCoroutine(SeelenSog()); break;
        }
    }

    // ── I1: Massen-Beschwörung ─────────────────────────────────────────────
    void DoMassSummon()
    {
        var spawner = FindObjectOfType<ShadowAllySpawner>();
        if (spawner == null)
        {
            Debug.LogWarning("HadesInterventions: kein ShadowAllySpawner in der Szene");
            return;
        }
        spawner.SummonShadowsFromDeadEnemies(massSummonCount);
    }

    // ── I2: Seelen-Sog ─────────────────────────────────────────────────────
    IEnumerator SeelenSog()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) yield break;

        Collider[] hits = Physics.OverlapSphere(player.transform.position, vacuumRadius,
            LayerMask.GetMask("Enemy"));

        var pulled = new List<EnemyBase>();
        foreach (var hit in hits)
        {
            var e = hit.GetComponent<EnemyBase>();
            if (e == null) continue;
            e.TakeDamage(e.maxHp * vacuumMaxHpFraction);
            pulled.Add(e);
        }

        // Pull-Effekt — direkter Position-Move; NavMeshAgent wirkt dagegen,
        // aber für 4 s setzt sich der Sog durch.
        float t = 0f;
        while (t < vacuumDuration)
        {
            t += Time.deltaTime;
            foreach (var e in pulled)
            {
                if (e == null) continue;
                Vector3 dir = (player.transform.position - e.transform.position);
                if (dir.sqrMagnitude < 0.04f) continue;   // angekommen
                dir = dir.normalized;
                e.transform.position += dir * vacuumPullSpeed * Time.deltaTime;
            }
            yield return null;
        }
    }
}
