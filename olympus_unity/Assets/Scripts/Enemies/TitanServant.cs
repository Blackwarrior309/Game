// TitanServant.cs
// Ablegen in: Assets/Scripts/Enemies/TitanServant.cs
// Kronos Phase 3 — ein Servant pro aktivem Tempel
// Jeder Servant hat Eigenschaften des entsprechenden Gottes

using UnityEngine;

public class TitanServant : EnemyBase
{
    [Header("Servant Type")]
    public FavorManager.God servantGod;

    protected override void Awake()
    {
        // Basis-Stats (werden je nach Gott angepasst)
        maxHp          = 250f;
        moveSpeed      = 3.8f;
        damage         = 22f;
        attackCooldown = 1.5f;
        attackRange    = 2.5f;
        xpReward       = 50f;
        ashDropMin     = 8;
        ashDropMax     = 15;
        oreDropChance  = 0.20f;
        prioritizePyros = false;
        base.Awake();
    }

    protected override void Start()
    {
        base.Start();
        ApplyGodModifiers();
    }

    void ApplyGodModifiers()
    {
        switch (servantGod)
        {
            case FavorManager.God.Zeus:
                // Zeus-Servant: Blitz-Angriff
                damage      *= 1.3f;
                moveSpeed   *= 1.1f;
                InvokeRepeating(nameof(LightningStrike), 3f, 6f);
                break;

            case FavorManager.God.Athena:
                // Athena-Servant: Hohe HP, Schild
                maxHp   *= 1.5f;
                hp       = maxHp;
                armor    = 8f;   // Flache Schadensreduktion
                break;

            case FavorManager.God.Ares:
                // Ares-Servant: Schnell, hoher Schaden
                moveSpeed   *= 1.4f;
                damage       *= 1.4f;
                attackCooldown *= 0.7f;
                break;

            case FavorManager.God.Poseidon:
                // Poseidon-Servant: Verlangsamt Spieler bei Treffer
                damage *= 0.9f;
                moveSpeed *= 1.1f;
                // Slow-Effekt bei Angriff wird in DealDamage angewendet
                break;

            case FavorManager.God.Hades:
                // Hades-Servant: Lebensraub
                maxHp *= 0.85f;
                hp     = maxHp;
                InvokeRepeating(nameof(LifeDrain), 2f, 4f);
                break;
        }
    }

    protected override void DealDamage()
    {
        base.DealDamage();

        // Poseidon-Servant: Spieler verlangsamen bei Treffer
        if (servantGod == FavorManager.God.Poseidon && playerTransform != null)
        {
            float dist = Vector3.Distance(transform.position, playerTransform.position);
            if (dist <= attackRange)
            {
                // Verlangsamung via EnemyBase-Methode... aber auf Spieler
                PlayerState.Instance.moveSpeed *= 0.65f;
                StartCoroutine(ResetPlayerSpeed(2f));
            }
        }
    }

    System.Collections.IEnumerator ResetPlayerSpeed(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (PlayerState.Instance != null)
            PlayerState.Instance.moveSpeed /= 0.65f;
    }

    // Zeus-Servant: Blitzschlag
    void LightningStrike()
    {
        if (isDead || playerTransform == null) return;
        float dist = Vector3.Distance(transform.position, playerTransform.position);
        if (dist <= 15f)
        {
            Collider[] hits = Physics.OverlapSphere(playerTransform.position, 3f,
                LayerMask.GetMask("Player"));
            foreach (var hit in hits)
                hit.GetComponent<PlayerController>()?.TakeDamage(damage * 0.8f);
        }
    }

    // Hades-Servant: Lebensraub in Nahbereich
    void LifeDrain()
    {
        if (isDead || playerTransform == null) return;
        float dist = Vector3.Distance(transform.position, playerTransform.position);
        if (dist <= 5f)
        {
            float drainAmount = 10f;
            PlayerState.Instance.TakeDamage(drainAmount);
            hp = Mathf.Min(maxHp, hp + drainAmount * 0.5f);
        }
    }

    // ── Titan-Servant-Besonderheit: stirbt wenn Kronos stirbt ─────────────
    void OnEnable()
    {
        Kronos.KronosHPChanged += OnKronosHPChanged;
    }

    void OnDisable()
    {
        Kronos.KronosHPChanged -= OnKronosHPChanged;
    }

    void OnKronosHPChanged(float current, float max)
    {
        // Wenn Kronos stirbt (HP = 0), Servants auch töten
        if (current <= 0f && !isDead)
            TakeDamage(maxHp * 10f);
    }

    // Armor-Field (nicht in EnemyBase)
    float armor = 0f;

    public override void TakeDamage(float amount)
    {
        amount = Mathf.Max(0f, amount - armor);
        base.TakeDamage(amount);
    }
}
