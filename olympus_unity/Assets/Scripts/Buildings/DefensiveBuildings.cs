// DefensiveBuildings.cs
// Ablegen in: Assets/Scripts/Buildings/DefensiveBuildings.cs
// Enthält: Palisade, StoneWall

using UnityEngine;
using UnityEngine.AI;

// ─────────────────────────────────────────────────────────────────────────────
// Palisade — günstiges Holzhindernis, blockiert Feind-Pathfinding
// ─────────────────────────────────────────────────────────────────────────────
public class Palisade : BuildingBase
{
    [Header("Palisade")]
    [SerializeField] NavMeshObstacle navObstacle;   // Blockiert NavMesh dynamisch

    protected override void Awake()
    {
        buildingType = "palisade";
        maxHp        = 80f;
        ashCost      = 20;
        buildTime    = 2f;
        base.Awake();
    }

    protected override void ApplyEffects()
    {
        // NavMesh-Hindernis aktivieren wenn Bau fertig
        if (navObstacle != null)
        {
            navObstacle.enabled    = true;
            navObstacle.carving    = true;   // Schneidet NavMesh aus (Godot: dynamisches Rebuild)
            navObstacle.carveOnlyStationary = false;
        }
    }

    // Palisade brennt leichter — Feuerturm-DoT macht +50% Schaden an Palisaden
    public override void TakeDamage(float amount)
    {
        base.TakeDamage(amount);
    }

    void OnDestroy()
    {
        // NavMesh-Hindernis deaktivieren damit Feinde wieder hindurchlaufen können
        if (navObstacle != null)
            navObstacle.enabled = false;
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// StoneWall — starke Steinmauer, höhere HP, teurerer
// ─────────────────────────────────────────────────────────────────────────────
public class StoneWall : BuildingBase
{
    [Header("StoneWall")]
    [SerializeField] NavMeshObstacle navObstacle;

    protected override void Awake()
    {
        buildingType = "stone_wall";
        maxHp        = 300f;
        ashCost      = 50;
        buildTime    = 3.5f;
        base.Awake();
    }

    protected override void ApplyEffects()
    {
        if (navObstacle != null)
        {
            navObstacle.enabled = true;
            navObstacle.carving = true;
            navObstacle.carveOnlyStationary = false;
        }

        // Athena-Tempel-Effekt: alle Gebäude +30% HP
        if (FavorManager.Instance.IsTempleBuilt(FavorManager.God.Athena))
        {
            maxHp *= 1.3f;
            hp     = maxHp;
        }
    }

    void OnDestroy()
    {
        if (navObstacle != null) navObstacle.enabled = false;
    }
}
