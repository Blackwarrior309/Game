// PlayerHUDPanel.cs
// Ablegen in: Assets/Scripts/UI/HUD/PlayerHUDPanel.cs
// Anhängen an: Panel GameObject "PlayerPanel" (oben links)
//
// Hierarchy:
//   PlayerPanel
//   ├── HPBar (Slider)
//   │   ├── Background (Image, dunkel)
//   │   ├── Fill (Image, rot→gold Gradient)
//   │   └── HPText (TextMeshProUGUI)
//   ├── DashIcon (Image)
//   │   └── DashCooldownOverlay (Image, fillMethod=Radial360)
//   └── LevelText (TextMeshProUGUI)

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class PlayerHUDPanel : MonoBehaviour
{
    [Header("HP")]
    [SerializeField] Slider        hpBar;
    [SerializeField] Image         hpFill;
    [SerializeField] TextMeshProUGUI hpText;

    [Header("XP")]
    [SerializeField] Slider        xpBar;

    [Header("Dash")]
    [SerializeField] Image         dashCooldownOverlay;  // Image.fillMethod = Radial360
    [SerializeField] Image         dashIcon;

    [Header("Level")]
    [SerializeField] TextMeshProUGUI levelText;

    [Header("Colors")]
    [SerializeField] Color colorHigh   = new Color(0.85f, 0.72f, 0.30f); // Gold
    [SerializeField] Color colorMid    = new Color(0.85f, 0.50f, 0.15f); // Orange
    [SerializeField] Color colorLow    = new Color(0.75f, 0.15f, 0.15f); // Rot

    // Dash-Cooldown Tracking
    float dashCooldownDuration = 2.5f;
    float dashCooldownRemaining = 0f;
    bool  dashOnCooldown = false;

    public void Initialize()
    {
        PlayerState.OnHpChanged  += UpdateHP;
        PlayerState.OnXpChanged  += UpdateXP;
        PlayerState.OnLevelUp    += UpdateLevel;
        PlayerState.OnPlayerDied += OnPlayerDied;

        // Startwerte setzen
        UpdateHP(PlayerState.Instance.hp, PlayerState.Instance.maxHp);
        UpdateXP(0f, PlayerState.Instance.xpToNextLevel);
        UpdateLevel(PlayerState.Instance.level);
    }

    void OnDestroy()
    {
        PlayerState.OnHpChanged  -= UpdateHP;
        PlayerState.OnXpChanged  -= UpdateXP;
        PlayerState.OnLevelUp    -= UpdateLevel;
        PlayerState.OnPlayerDied -= OnPlayerDied;
    }

    // ── HP ─────────────────────────────────────────────────────────────────
    void UpdateHP(float current, float max)
    {
        if (hpBar == null) return;
        float ratio = max > 0 ? current / max : 0f;
        hpBar.value = ratio;

        // Farbe nach Prozent
        Color targetColor = ratio > 0.6f ? colorHigh : ratio > 0.3f ? colorMid : colorLow;
        if (hpFill != null) hpFill.color = targetColor;
        if (hpText  != null) hpText.text  = $"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";

        // Shake bei niedrigem HP
        if (ratio < 0.25f) StartCoroutine(ShakePanel());
    }

    IEnumerator ShakePanel()
    {
        Vector3 origin = transform.localPosition;
        for (int i = 0; i < 4; i++)
        {
            transform.localPosition = origin + new Vector3(Random.Range(-4f, 4f), 0f, 0f);
            yield return new WaitForSeconds(0.04f);
        }
        transform.localPosition = origin;
    }

    // ── XP ─────────────────────────────────────────────────────────────────
    void UpdateXP(float current, float required)
    {
        if (xpBar == null) return;
        xpBar.value = required > 0 ? current / required : 0f;
    }

    // ── Level ──────────────────────────────────────────────────────────────
    void UpdateLevel(int level)
    {
        if (levelText != null) levelText.text = $"LVL {level}";
        StartCoroutine(PulseLevel());
    }

    IEnumerator PulseLevel()
    {
        if (levelText == null) yield break;
        float t = 0f;
        Vector3 baseScale = Vector3.one;
        while (t < 0.4f)
        {
            t += Time.deltaTime;
            float s = 1f + Mathf.Sin(t * Mathf.PI / 0.4f) * 0.3f;
            levelText.transform.localScale = baseScale * s;
            yield return null;
        }
        levelText.transform.localScale = baseScale;
    }

    // ── Dash-Cooldown ──────────────────────────────────────────────────────
    public void StartDashCooldown(float duration)
    {
        dashCooldownDuration  = duration;
        dashCooldownRemaining = duration;
        dashOnCooldown        = true;
        if (dashIcon != null) dashIcon.color = new Color(0.4f, 0.4f, 0.4f);
    }

    void Update()
    {
        if (!dashOnCooldown) return;
        dashCooldownRemaining -= Time.deltaTime;
        float ratio = Mathf.Clamp01(dashCooldownRemaining / dashCooldownDuration);

        if (dashCooldownOverlay != null) dashCooldownOverlay.fillAmount = ratio;

        if (dashCooldownRemaining <= 0f)
        {
            dashOnCooldown = false;
            if (dashCooldownOverlay != null) dashCooldownOverlay.fillAmount = 0f;
            if (dashIcon != null)            dashIcon.color = Color.white;
        }
    }

    void OnPlayerDied()
    {
        StartCoroutine(DeathFlash());
    }

    IEnumerator DeathFlash()
    {
        if (hpFill == null) yield break;
        for (int i = 0; i < 3; i++)
        {
            hpFill.color = Color.white;
            yield return new WaitForSeconds(0.1f);
            hpFill.color = colorLow;
            yield return new WaitForSeconds(0.1f);
        }
    }
}
