// SupportBuildings.cs
// Ablegen in: Assets/Scripts/Buildings/SupportBuildings.cs
// Enthält: HealingShrine, ResourceAltar, SacrificeAltar

using UnityEngine;
using System.Collections;

// ─────────────────────────────────────────────────────────────────────────────
// HealingShrine — regeneriert Pyros HP passiv
// ─────────────────────────────────────────────────────────────────────────────
public class HealingShrine : BuildingBase
{
    [Header("Healing Shrine")]
    [SerializeField] float healPerSecond  = 2f;
    [SerializeField] float healRadius     = 0f;   // 0 = nur Pyros
    [SerializeField] ParticleSystem healFX;

    Pyros pyros;

    protected override void Awake()
    {
        buildingType = "healing_shrine";
        maxHp        = 80f;
        ashCost      = 70;
        buildTime    = 3f;
        base.Awake();
    }

    protected override void ApplyEffects()
    {
        pyros = FindObjectOfType<Pyros>();
        if (healFX != null) healFX.Play();
        StartCoroutine(HealLoop());
    }

    IEnumerator HealLoop()
    {
        while (isBuilt)
        {
            yield return new WaitForSeconds(1f);

            if (pyros != null)
                pyros.Heal(healPerSecond);
        }
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// ResourceAltar — generiert Asche passiv
// ─────────────────────────────────────────────────────────────────────────────
public class ResourceAltar : BuildingBase
{
    [Header("Resource Altar")]
    [SerializeField] float ashPerInterval  = 5f;
    [SerializeField] float intervalSeconds = 10f;
    [SerializeField] ParticleSystem ashFX;

    protected override void Awake()
    {
        buildingType = "resource_altar";
        maxHp        = 60f;
        ashCost      = 40;
        buildTime    = 2.5f;
        base.Awake();
    }

    protected override void ApplyEffects()
    {
        if (ashFX != null) ashFX.Play();
        StartCoroutine(GenerateAsh());
    }

    IEnumerator GenerateAsh()
    {
        while (isBuilt)
        {
            yield return new WaitForSeconds(intervalSeconds);
            PlayerState.Instance.AddAsh(Mathf.RoundToInt(ashPerInterval));
        }
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// SacrificeAltar — Spieler opfert Asche für Favor
// ─────────────────────────────────────────────────────────────────────────────
public class SacrificeAltar : BuildingBase
{
    [Header("Sacrifice Altar")]
    [SerializeField] int   ashCostPerSacrifice    = 30;
    [SerializeField] float interactRadius         = 2.5f;
    [SerializeField] float sacrificeCooldown      = 3f;

    // Opferaltar-Effizienz-Bonus aus Meta-Progression (Standard: 1.0)
    float efficiencyMultiplier = 1f;

    bool playerInRange   = false;
    bool onCooldown      = false;
    float cooldownTimer  = 0f;

    // Aktuelle Gott-Auswahl (wird über UI gesetzt)
    FavorManager.God selectedGod = FavorManager.God.Zeus;

    // ── Events für UI ────────────────────────────────────────────────────
    public static event System.Action<SacrificeAltar> OnPlayerEnterRange;
    public static event System.Action<SacrificeAltar> OnPlayerExitRange;

    protected override void Awake()
    {
        buildingType = "sacrifice_altar";
        maxHp        = 100f;
        ashCost      = 50;
        buildTime    = 2f;
        base.Awake();
    }

    protected override void ApplyEffects()
    {
        // Meta-Progression: Opferaltar-Effizienz aus persistenten Upgrades
        // efficiencyMultiplier = MetaProgression.Instance.AltarEfficiency;
    }

    void Update()
    {
        if (!isBuilt) return;

        // Cooldown
        if (onCooldown)
        {
            cooldownTimer -= Time.deltaTime;
            if (cooldownTimer <= 0f) onCooldown = false;
        }

        // Spieler-Nähe prüfen
        var player = FindObjectOfType<PlayerController>();
        if (player == null) return;

        float dist = Vector3.Distance(transform.position, player.transform.position);
        bool inRange = dist <= interactRadius;

        if (inRange && !playerInRange)
        {
            playerInRange = true;
            OnPlayerEnterRange?.Invoke(this);
        }
        else if (!inRange && playerInRange)
        {
            playerInRange = false;
            OnPlayerExitRange?.Invoke(this);
        }

        // Interaktion via Taste E
        if (playerInRange && !onCooldown && Input.GetKeyDown(KeyCode.E))
            TrySacrifice();
    }

    // ── Opferung ──────────────────────────────────────────────────────────
    public void SetSelectedGod(FavorManager.God god) => selectedGod = god;

    public bool TrySacrifice()
    {
        if (onCooldown) return false;

        // Opferaltar-Effizienz-Bonus anwenden
        int cost = Mathf.RoundToInt(ashCostPerSacrifice / efficiencyMultiplier);

        bool success = FavorManager.Instance.OnSacrifice(selectedGod, cost);
        if (success)
        {
            onCooldown    = true;
            cooldownTimer = sacrificeCooldown;

            // Visuelles Feedback
            StartCoroutine(SacrificeEffect());
        }
        return success;
    }

    IEnumerator SacrificeEffect()
    {
        // Kurzes Aufleuchten (Farbe des gewählten Gottes)
        var renderer = GetComponentInChildren<Renderer>();
        if (renderer != null)
        {
            Color godColor = GetGodColor(selectedGod);
            Color orig     = renderer.material.color;
            renderer.material.color = godColor;
            yield return new WaitForSeconds(0.3f);
            renderer.material.color = orig;
        }
        yield return null;
    }

    Color GetGodColor(FavorManager.God god) => god switch
    {
        FavorManager.God.Zeus       => new Color(0.95f, 0.88f, 0.20f),
        FavorManager.God.Athena     => new Color(0.40f, 0.72f, 0.95f),
        FavorManager.God.Ares       => new Color(0.90f, 0.20f, 0.15f),
        FavorManager.God.Poseidon   => new Color(0.25f, 0.65f, 0.90f),
        FavorManager.God.Hades      => new Color(0.55f, 0.20f, 0.80f),
        FavorManager.God.Hephaistos => new Color(0.95f, 0.55f, 0.10f),
        _ => Color.white
    };

    // Getter für UI
    public int      AshCost     => Mathf.RoundToInt(ashCostPerSacrifice / efficiencyMultiplier);
    public bool     OnCooldown  => onCooldown;
    public float    CooldownPct => onCooldown ? cooldownTimer / sacrificeCooldown : 0f;
}
