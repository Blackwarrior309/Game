// ZeusInterventions.cs
// Ablegen in: Assets/Scripts/Gods/ZeusInterventions.cs
// Anhängen an: Singletons-GameObject (DontDestroyOnLoad)
//
// Hört auf FavorManager.OnThresholdReached und löst die beiden Zeus-
// Interventionen aus:
//   - intervention_1 (Favor 25): Blitzeinschlag — 3 große Blitze auf die
//     stärksten Feinde im Spieler-Radius
//   - intervention_2 (Favor 75): Donnersturm — 10 s lang regelmäßige
//     Blitz-AoEs auf zufällige Feinde um den Spieler
//
// Synergien:
//   - storm_flood     (Zeus + Poseidon): Blitze verlangsamen Feinde
//   - underworld_storm (Zeus + Hades):    25 % Schatten-Spawn pro Blitz-Kill
//   - sky_fire        (Zeus + Hephaistos): Einschlag hinterlässt Feuerpfütze
//   - wargod_wrath    (Zeus + Ares):      Donnersturm-Schaden ×2

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ZeusInterventions : MonoBehaviour
{
    [Header("Intervention 1: Blitzeinschlag")]
    [SerializeField] int   strikeCount       = 3;
    [SerializeField] float strikeRadius      = 4f;
    [SerializeField] float strikeDamage      = 100f;
    [SerializeField] float strikeSearchRange = 40f;

    [Header("Intervention 2: Donnersturm")]
    [SerializeField] float stormDuration     = 10f;
    [SerializeField] float stormInterval     = 0.8f;
    [SerializeField] float stormStrikeRadius = 3f;
    [SerializeField] float stormStrikeDamage = 60f;
    [SerializeField] float stormPlayerRange  = 20f;

    [Header("Sky-Fire-Synergie")]
    [SerializeField] GameObject firePuddlePrefab;        // optional, nur für sky_fire

    bool stormRunning = false;

    // ── Unity Lifecycle ────────────────────────────────────────────────────
    void OnEnable()  => FavorManager.OnThresholdReached += HandleThreshold;
    void OnDisable() => FavorManager.OnThresholdReached -= HandleThreshold;

    void HandleThreshold(FavorManager.God god, string key)
    {
        if (god != FavorManager.God.Zeus) return;
        switch (key)
        {
            case "intervention_1": DoBlitzeinschlag();              break;
            case "intervention_2": StartCoroutine(Donnersturm());   break;
        }
    }

    // ── I1: Blitzeinschlag ─────────────────────────────────────────────────
    void DoBlitzeinschlag()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        // Top-N stärkste lebende Feinde im Suchradius
        var candidates = new List<EnemyBase>();
        Collider[] hits = Physics.OverlapSphere(player.transform.position, strikeSearchRange,
            LayerMask.GetMask("Enemy"));
        foreach (var hit in hits)
        {
            var e = hit.GetComponent<EnemyBase>();
            if (e != null && !e.isDead) candidates.Add(e);
        }
        candidates.Sort((a, b) => b.maxHp.CompareTo(a.maxHp));

        int count = Mathf.Min(strikeCount, candidates.Count);
        for (int i = 0; i < count; i++)
            StrikeAt(candidates[i].transform.position);
    }

    // ── I2: Donnersturm ────────────────────────────────────────────────────
    IEnumerator Donnersturm()
    {
        if (stormRunning) yield break;
        stormRunning = true;

        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) { stormRunning = false; yield break; }

        float damageMult = SynergySystem.Instance.IsActive("wargod_wrath") ? 2f : 1f;
        float elapsed = 0f;

        while (elapsed < stormDuration)
        {
            Collider[] hits = Physics.OverlapSphere(player.transform.position, stormPlayerRange,
                LayerMask.GetMask("Enemy"));

            if (hits.Length > 0)
            {
                var target = hits[Random.Range(0, hits.Length)];
                StrikeAt(target.transform.position, stormStrikeRadius,
                         stormStrikeDamage * damageMult);
            }

            yield return new WaitForSeconds(stormInterval);
            elapsed += stormInterval;
        }

        stormRunning = false;
    }

    // ── Gemeinsame Einschlags-Routine ──────────────────────────────────────
    void StrikeAt(Vector3 center) => StrikeAt(center, strikeRadius, strikeDamage);

    void StrikeAt(Vector3 center, float radius, float damage)
    {
        Collider[] hits = Physics.OverlapSphere(center, radius, LayerMask.GetMask("Enemy"));
        foreach (var hit in hits)
        {
            var e = hit.GetComponent<EnemyBase>();
            if (e == null) continue;

            e.TakeDamage(damage);
            e.SetMeta("killed_by_lightning", true);

            if (SynergySystem.Instance.IsActive("storm_flood"))
                e.ApplySlow(0.7f, 3f);

            if (SynergySystem.Instance.IsActive("underworld_storm") && Random.value < 0.25f)
                GameEvents.RaiseSpawnShadowAlly(e.transform.position);
        }

        // Sky-Fire-Synergie: Feuerpfütze am Einschlagsort
        if (SynergySystem.Instance.IsActive("sky_fire") && firePuddlePrefab != null)
            Instantiate(firePuddlePrefab, center, Quaternion.identity);
    }
}
