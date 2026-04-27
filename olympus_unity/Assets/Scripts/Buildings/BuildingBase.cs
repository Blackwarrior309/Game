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

    [Header("Upgrade-Interaktion (P3-12)")]
    [SerializeField] float interactRadius   = 4f;
    [SerializeField] UnityEngine.KeyCode upgradeKey = UnityEngine.KeyCode.E;

    Transform playerTransform;

    // Live-Lookup auf FavorManager — vermeidet Doppelhaltung von templeLevel.
    public int Level => FavorManager.Instance != null
        ? FavorManager.Instance.GetTempleLevel(godId)
        : 1;

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

        // Gott-spezifische Auto-Effekte (lesen Level live in der Coroutine)
        switch (godId)
        {
            case FavorManager.God.Zeus:    StartCoroutine(ZeusAutoLightning()); break;
            case FavorManager.God.Hades:   StartCoroutine(HadesAutoShadow());   break;
        }
    }

    // ── Upgrade-API ────────────────────────────────────────────────────────
    public int GetUpgradeCost()
    {
        // L1→2 = 200 Asche · L2→3 = 300 Asche · danach kein Upgrade mehr.
        switch (Level)
        {
            case 1: return 200;
            case 2: return 300;
            default: return 0;
        }
    }

    public bool CanUpgrade()
    {
        if (Level >= 3) return false;
        return PlayerState.Instance != null && PlayerState.Instance.ash >= GetUpgradeCost();
    }

    public bool TryUpgrade()
    {
        if (!CanUpgrade()) return false;
        int cost = GetUpgradeCost();
        if (!PlayerState.Instance.SpendAsh(cost)) return false;
        FavorManager.Instance.UpgradeTemple(godId);
        return true;
    }

    // ── Spieler-Interaktion (E im Radius) ──────────────────────────────────
    void Update()
    {
        if (!isBuilt) return;
        if (playerTransform == null)
        {
            var p = UnityEngine.GameObject.FindGameObjectWithTag("Player");
            if (p != null) playerTransform = p.transform;
            if (playerTransform == null) return;
        }

        if (UnityEngine.Vector3.Distance(transform.position, playerTransform.position) > interactRadius)
            return;

        if (UnityEngine.Input.GetKeyDown(upgradeKey))
            TryUpgrade();
    }

    // ── Auto-Effekte mit Level-Skalierung ──────────────────────────────────
    // Zeus: L1: 20 s / 15 dmg · L2: 15 s / 25 dmg · L3: 10 s / 40 dmg
    System.Collections.IEnumerator ZeusAutoLightning()
    {
        while (isBuilt)
        {
            float interval = ZeusInterval(Level);
            float dmg      = ZeusDamage(Level);
            yield return new UnityEngine.WaitForSeconds(interval);
            if (!isBuilt) yield break;

            var hits = UnityEngine.Physics.OverlapSphere(transform.position, 8f,
                UnityEngine.LayerMask.GetMask("Enemy"));
            foreach (var hit in hits)
                hit.GetComponent<EnemyBase>()?.TakeDamage(dmg);
        }
    }

    static float ZeusInterval(int level) => level == 1 ? 20f : (level == 2 ? 15f : 10f);
    static float ZeusDamage(int level)   => level == 1 ? 15f : (level == 2 ? 25f : 40f);

    // Hades: L1: 45 s · L2: 35 s · L3: 25 s
    System.Collections.IEnumerator HadesAutoShadow()
    {
        while (isBuilt)
        {
            float interval = HadesInterval(Level);
            yield return new UnityEngine.WaitForSeconds(interval);
            if (!isBuilt) yield break;
            GameEvents.RaiseSpawnShadowAlly(transform.position);
        }
    }

    static float HadesInterval(int level) => level == 1 ? 45f : (level == 2 ? 35f : 25f);
}
