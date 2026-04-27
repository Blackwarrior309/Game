// PlayerController.cs
// Ablegen in: Assets/Scripts/Player/PlayerController.cs
// Anhängen an: GameObject mit CharacterController-Komponente
// Kinder-Objekte:
//   - "PickupTrigger"  SphereCollider (isTrigger, Radius 3) + PickupDetector.cs
//   - "AttackTrigger"  SphereCollider (isTrigger, Radius 3) + AttackRangeDetector.cs

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    // ── Exports / Inspector ────────────────────────────────────────────────
    [Header("Dash")]
    [SerializeField] float dashSpeedMultiplier = 4f;
    [SerializeField] float dashDuration        = 0.2f;
    [SerializeField] float dashCooldown        = 2.5f;

    [Header("Combat")]
    [SerializeField] float attackRange = 3f;

    // ── Refs ───────────────────────────────────────────────────────────────
    CharacterController cc;

    // ── State ──────────────────────────────────────────────────────────────
    bool isDashing;
    bool isInvincible;
    bool dashOnCooldown;
    float dashTimer;
    float dashCooldownTimer;
    float attackTimer;
    Vector3 dashDirection;

    // Feind-Tracking (befüllt von AttackRangeDetector)
    public List<EnemyBase> NearbyEnemies { get; } = new();
    EnemyBase currentTarget;

    // Zeus-Passiv: Blitz-Counter
    int attackCount = 0;

    const float Gravity = -20f;
    float verticalVelocity;

    // ── Unity Lifecycle ────────────────────────────────────────────────────
    void Awake()
    {
        cc = GetComponent<CharacterController>();
        tag = "Player";
    }

    void OnEnable()
    {
        PlayerState.OnPlayerDied += OnDied;
    }

    void OnDisable()
    {
        PlayerState.OnPlayerDied -= OnDied;
    }

    void Update()
    {
        HandleGravity();
        if (isDashing) HandleDash(); else HandleMovement();
        HandleDashCooldown();
        HandleAutoAttack();
        HandleInput();
    }

    // ── Bewegung ───────────────────────────────────────────────────────────
    void HandleMovement()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector3 dir = new Vector3(h, 0f, v).normalized;

        Vector3 move = dir * PlayerState.Instance.moveSpeed;
        move.y = verticalVelocity;
        cc.Move(move * Time.deltaTime);

        if (dir.magnitude > 0.01f)
            transform.rotation = Quaternion.LookRotation(dir);
    }

    void HandleGravity()
    {
        if (cc.isGrounded) verticalVelocity = -1f;
        else verticalVelocity += Gravity * Time.deltaTime;
    }

    // ── Dash ───────────────────────────────────────────────────────────────
    void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.Space) && !dashOnCooldown && !isDashing)
            StartDash();

        if (Input.GetKeyDown(KeyCode.B))
            GameEvents.RaiseBuildMenuToggle();

        if (Input.GetKeyDown(KeyCode.F) && PlayerState.Instance.hasForge)
            GameEvents.RaiseSmithyMenuToggle();

        if (Input.GetKeyDown(KeyCode.G))
            TryActivateAvatar();
    }

    void StartDash()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector3 inputDir = new Vector3(h, 0f, v);
        dashDirection = inputDir.magnitude > 0.01f ? inputDir.normalized : -transform.forward;

        isDashing   = true;
        isInvincible = true;
        dashTimer   = dashDuration;
    }

    void HandleDash()
    {
        dashTimer -= Time.deltaTime;
        Vector3 move = dashDirection * PlayerState.Instance.moveSpeed * dashSpeedMultiplier;
        move.y = verticalVelocity;
        cc.Move(move * Time.deltaTime);

        if (dashTimer <= 0f)
        {
            isDashing    = false;
            isInvincible = false;
            dashOnCooldown = true;
            dashCooldownTimer = dashCooldown;
        }
    }

    void HandleDashCooldown()
    {
        if (!dashOnCooldown) return;
        dashCooldownTimer -= Time.deltaTime;
        if (dashCooldownTimer <= 0f) dashOnCooldown = false;
    }

    // ── Auto-Angriff ───────────────────────────────────────────────────────
    void HandleAutoAttack()
    {
        float attackInterval = 1f / PlayerState.Instance.attackSpeed;
        attackTimer += Time.deltaTime;

        UpdateTarget();

        if (attackTimer >= attackInterval && currentTarget != null)
        {
            attackTimer = 0f;
            DoAttack();
        }
    }

    void UpdateTarget()
    {
        currentTarget = null;
        float closest = float.MaxValue;
        for (int i = NearbyEnemies.Count - 1; i >= 0; i--)
        {
            if (NearbyEnemies[i] == null) { NearbyEnemies.RemoveAt(i); continue; }
            float d = Vector3.Distance(transform.position, NearbyEnemies[i].transform.position);
            if (d < closest) { closest = d; currentTarget = NearbyEnemies[i]; }
        }
    }

    void DoAttack()
    {
        if (currentTarget == null) return;
        Vector3 lookDir = currentTarget.transform.position - transform.position;
        lookDir.y = 0f;
        if (lookDir.magnitude > 0.01f) transform.rotation = Quaternion.LookRotation(lookDir);

        float finalDamage = PlayerState.Instance.damage;
        currentTarget.TakeDamage(finalDamage);

        // Zeus-Passiv: jeder 10. Angriff = Blitzschlag AoE
        if (FavorManager.Instance.IsPassiveActive(FavorManager.God.Zeus))
        {
            attackCount++;
            if (attackCount >= 10) { attackCount = 0; TriggerZeusLightning(currentTarget.transform.position); }
        }

        GameEvents.RaisePlayerAttacked(currentTarget.gameObject);
    }

    void TriggerZeusLightning(Vector3 center)
    {
        float multiplier = SynergySystem.Instance.IsActive("wargod_wrath") ? 2f : 1f;
        Collider[] hits = Physics.OverlapSphere(center, 3f, LayerMask.GetMask("Enemy"));
        foreach (var hit in hits)
        {
            var enemy = hit.GetComponent<EnemyBase>();
            if (enemy != null)
            {
                enemy.TakeDamage(PlayerState.Instance.damage * 1.5f * multiplier);
                enemy.SetMeta("killed_by_lightning", true);
                // Gewitterflut-Synergie: Blitze verlangsamen
                if (SynergySystem.Instance.IsActive("storm_flood"))
                    enemy.ApplySlow(0.7f, 3f);
            }
        }
    }

    // ── Avatar aktivieren ─────────────────────────────────────────────────
    void TryActivateAvatar()
    {
        // Versuche Hauptgott zuerst, dann alle anderen
        FavorManager.Instance.TryActivateAvatar(FavorManager.Instance.MainGod);
    }

    // ── Schaden nehmen ─────────────────────────────────────────────────────
    public void TakeDamage(float amount)
    {
        if (isInvincible) return;
        PlayerState.Instance.TakeDamage(amount);
    }

    void OnDied()
    {
        // Zur Pyros-Position teleportieren
        var pyros = GameObject.FindGameObjectWithTag("Pyros");
        if (pyros != null)
            transform.position = pyros.transform.position + Vector3.right * 2f;
    }
}
