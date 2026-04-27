// HephaistosInterventions.cs
// Ablegen in: Assets/Scripts/Gods/HephaistosInterventions.cs
// Anhängen an: Singletons-GameObject (DontDestroyOnLoad)
// Hört auf FavorManager.OnThresholdReached und löst die beiden Hephaistos-
// Interventionen aus (Schmiede-Burst bei 25, Vulkan-Zorn bei 75).
//
// Hephaistos hat keinen Avatar — die Schmiede ist die Hauptbelohnung
// (siehe FavorManager.TryActivateAvatar: God.Hephaistos wird abgewiesen).

using UnityEngine;
using System.Collections;

public class HephaistosInterventions : MonoBehaviour
{
    [Header("Intervention 1: Schmiede-Burst")]
    [SerializeField] float burstMultiplier = 1.5f;     // +50 % Waffenschaden
    [SerializeField] float burstDuration   = 20f;

    [Header("Intervention 2: Vulkan-Zorn")]
    [SerializeField] GameObject lavaBoulderPrefab;     // ProjectileBase, aoeRadius>0
    [SerializeField] GameObject lavaPuddlePrefab;      // hat LavaPuddle.cs
    [SerializeField] int   boulderCount  = 6;
    [SerializeField] float throwRadius   = 18f;        // um Spielerposition
    [SerializeField] float dropHeight    = 25f;
    [SerializeField] float boulderDamage = 40f;
    [SerializeField] float spawnInterval = 0.25f;
    [SerializeField] float puddleSpawnChance = 1f;     // 1.0 = jeder Brocken legt eine Pfütze
    // Muss zur ProjectileBase.speed im LavaBoulder-Prefab passen (Default 18).
    [SerializeField] float boulderFallSpeed = 18f;

    bool burstRunning = false;

    // ── Unity Lifecycle ────────────────────────────────────────────────────
    void OnEnable()
    {
        FavorManager.OnThresholdReached += HandleThreshold;
    }

    void OnDisable()
    {
        FavorManager.OnThresholdReached -= HandleThreshold;
    }

    // ── Trigger ────────────────────────────────────────────────────────────
    void HandleThreshold(FavorManager.God god, string key)
    {
        if (god != FavorManager.God.Hephaistos) return;

        switch (key)
        {
            case "intervention_1": StartCoroutine(SchmiedeBurst()); break;
            case "intervention_2": StartCoroutine(VulkanZorn());    break;
        }
    }

    // ── Intervention 1: Schmiede-Burst ─────────────────────────────────────
    // +50 % Waffenschaden für 20 s. Mehrfach-Trigger ignorieren statt stapeln.
    IEnumerator SchmiedeBurst()
    {
        if (burstRunning) yield break;
        burstRunning = true;

        var ps = PlayerState.Instance;
        ps.damageMultiplier *= burstMultiplier;
        yield return new WaitForSeconds(burstDuration);
        ps.damageMultiplier /= burstMultiplier;

        burstRunning = false;
    }

    // ── Intervention 2: Vulkan-Zorn ────────────────────────────────────────
    // Lava-Brocken regnen um den Spieler, jeder Aufprall hinterlässt eine
    // Lavapfütze (Boden-DoT, siehe LavaPuddle.cs).
    IEnumerator VulkanZorn()
    {
        if (lavaBoulderPrefab == null) { Debug.LogWarning("Vulkan-Zorn: lavaBoulderPrefab nicht gesetzt"); yield break; }

        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) yield break;

        for (int i = 0; i < boulderCount; i++)
        {
            Vector2 offset = Random.insideUnitCircle * throwRadius;
            Vector3 target = player.transform.position + new Vector3(offset.x, 0f, offset.y);
            target.y = 0f;
            Vector3 origin = target + Vector3.up * dropHeight;

            var boulder = Instantiate(lavaBoulderPrefab, origin, Quaternion.identity);
            var proj    = boulder.GetComponent<ProjectileBase>();
            proj?.Initialize(Vector3.down, boulderDamage, "player");

            // Pfütze ca. zum Aufprall-Zeitpunkt am Zielpunkt spawnen.
            if (lavaPuddlePrefab != null && Random.value <= puddleSpawnChance)
                StartCoroutine(SpawnPuddleDelayed(target, dropHeight / boulderFallSpeed));

            yield return new WaitForSeconds(spawnInterval);
        }
    }

    IEnumerator SpawnPuddleDelayed(Vector3 pos, float delay)
    {
        yield return new WaitForSeconds(delay);
        Instantiate(lavaPuddlePrefab, pos, Quaternion.identity);
    }
}
