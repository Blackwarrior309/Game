// BuildingBase.cs
// Ablegen in: Assets/Scripts/Buildings/BuildingBase.cs

using UnityEngine;
using System;
using System.Collections;

public class BuildingBase : MonoBehaviour
{
    [Header("Building Stats")]
    public string buildingType = "generic";
    public float  maxHp        = 100f;
    public float  buildTime    = 3f;
    public int    ashCost      = 50;
    public int    oreCost      = 0;

    [HideInInspector] public bool isBuilt   = false;
    [HideInInspector] public bool isTemple  = false;
    [HideInInspector] public FavorManager.God godId;

    protected float hp;

    public static event Action<BuildingBase> OnBuildingCompleted;
    public static event Action<BuildingBase> OnBuildingDestroyed;

    protected virtual void Awake()
    {
        hp = maxHp;
        tag = "Building";
    }

    public void StartBuilding() => StartCoroutine(BuildCoroutine());

    IEnumerator BuildCoroutine()
    {
        isBuilt = false;
        yield return new WaitForSeconds(buildTime);
        isBuilt = true;
        OnBuildingCompleted?.Invoke(this);
        ApplyEffects();
    }

    protected virtual void ApplyEffects() {}

    public virtual void TakeDamage(float amount)
    {
        hp = Mathf.Max(0f, hp - amount);
        if (hp <= 0f) Destroy_();
    }

    // Heilen — kappt sauber bei maxHp. Genutzt z.B. von der war_strategy-
    // Synergie (Berserker heilt Gebäude statt sie unzerstörbar zu machen).
    public virtual void Heal(float amount)
    {
        hp = Mathf.Min(maxHp, hp + Mathf.Max(0f, amount));
    }

    void Destroy_()
    {
        OnBuildingDestroyed?.Invoke(this);

        if (isTemple)
        {
            FavorManager.Instance.OnTempleDestroyed(godId);
            GameEvents.RaiseBuildingPlaced("temple_destroyed", transform.position);
        }
        else if (buildingType == "forge")
        {
            FavorManager.Instance.OnForgeDestroyed();
        }
        Destroy(gameObject);
    }
}

// ─────────────────────────────────────────────────────────────────────────────

// Temple.cs — anhängen zusätzlich zu BuildingBase oder als eigenständige Klasse
// Ablegen in: Assets/Scripts/Buildings/Temple.cs

public class Temple : BuildingBase
{
    [Header("Temple")]
    [SerializeField] FavorManager.God templeGodId;

    protected override void Awake()
    {
        buildingType = "temple";
        isTemple     = true;
        godId        = templeGodId;
        maxHp        = 200f;
        ashCost      = 150;
        buildTime    = 4f;
        base.Awake();
    }

    protected override void ApplyEffects()
    {
        FavorManager.Instance.OnTempleBuilt(godId);
        PlayerState.Instance.activeTemples++;
        GameEvents.RaiseBuildingPlaced("temple_" + godId.ToString().ToLower(), transform.position);

        // Gott-spezifische Auto-Effekte
        switch (godId)
        {
            case FavorManager.God.Zeus:    StartCoroutine(ZeusAutoLightning()); break;
            case FavorManager.God.Hades:   StartCoroutine(HadesAutoShadow());   break;
        }
    }

    System.Collections.IEnumerator ZeusAutoLightning()
    {
        while (isBuilt)
        {
            yield return new UnityEngine.WaitForSeconds(20f);
            if (!isBuilt) yield break;
            // AoE-Blitz auf alle Feinde in 8m
            var hits = UnityEngine.Physics.OverlapSphere(transform.position, 8f,
                UnityEngine.LayerMask.GetMask("Enemy"));
            foreach (var hit in hits)
                hit.GetComponent<EnemyBase>()?.TakeDamage(15f);
        }
    }

    System.Collections.IEnumerator HadesAutoShadow()
    {
        while (isBuilt)
        {
            yield return new UnityEngine.WaitForSeconds(45f);
            if (!isBuilt) yield break;
            GameEvents.RaiseSpawnShadowAlly(transform.position);
        }
    }
}
