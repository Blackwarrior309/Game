// FavorHUDPanel.cs
// Ablegen in: Assets/Scripts/UI/HUD/FavorHUDPanel.cs
// Unten links — 6 Favor-Leisten (das visuelle Herzstück des HUDs)
//
// Hierarchy:
//   FavorPanel
//   └── FavorGrid (VerticalLayoutGroup)
//       ├── FavorRow_Zeus      (FavorRowUI prefab)
//       ├── FavorRow_Athena
//       ├── FavorRow_Ares
//       ├── FavorRow_Poseidon
//       ├── FavorRow_Hades
//       └── FavorRow_Hephaistos
//
// FavorRowUI Hierarchy:
//   FavorRow
//   ├── GodIcon (Image, 32×32)
//   ├── GodNameText (TextMeshProUGUI)
//   ├── FavorBarBG (Image, dunkel)
//   │   └── FavorBarFill (Image, fillMethod=Horizontal)
//   ├── FavorValueText (TextMeshProUGUI) "75"
//   ├── ThresholdMarkers (Canvas)
//   │   ├── Marker25 (Image, schmaler Strich)
//   │   ├── Marker50 (Image)
//   │   └── Marker75 (Image)
//   ├── StatusIcon (Image) — Passiv/Avatar-Icon
//   └── AvatarTimerRing (Image, fillMethod=Radial360)

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using static FavorManager;

public class FavorHUDPanel : MonoBehaviour
{
    [Header("Row Prefab")]
    [SerializeField] GameObject favorRowPrefab;

    [Header("God Icons (Reihenfolge: Zeus,Athena,Ares,Poseidon,Hades,Hephaistos)")]
    [SerializeField] Sprite[] godIcons;

    // Laufzeit-Tracking
    Dictionary<God, FavorRowUI> rows = new();

    public void Initialize()
    {
        // Rows erstellen
        foreach (God god in System.Enum.GetValues(typeof(God)))
        {
            var rowGO  = Instantiate(favorRowPrefab, transform);
            var rowUI  = rowGO.GetComponent<FavorRowUI>();
            if (rowUI == null) continue;

            int idx = (int)god;
            rowUI.Setup(god, GodNames[idx],
                         godIcons != null && idx < godIcons.Length ? godIcons[idx] : null);
            rows[god] = rowUI;
        }

        // Events
        FavorManager.OnFavorChanged       += OnFavorChanged;
        FavorManager.OnPassiveActivated   += OnPassiveActivated;
        FavorManager.OnPassiveDeactivated += OnPassiveDeactivated;
        FavorManager.OnAvatarStarted      += OnAvatarStarted;
        FavorManager.OnAvatarEnded        += OnAvatarEnded;
        FavorManager.OnThresholdReached   += OnThresholdReached;
        SynergySystem.OnSynergyActivated  += OnSynergyActivated;
    }

    void OnDestroy()
    {
        FavorManager.OnFavorChanged       -= OnFavorChanged;
        FavorManager.OnPassiveActivated   -= OnPassiveActivated;
        FavorManager.OnPassiveDeactivated -= OnPassiveDeactivated;
        FavorManager.OnAvatarStarted      -= OnAvatarStarted;
        FavorManager.OnAvatarEnded        -= OnAvatarEnded;
        FavorManager.OnThresholdReached   -= OnThresholdReached;
        SynergySystem.OnSynergyActivated  -= OnSynergyActivated;
    }

    void OnFavorChanged(God god, float value)
    {
        if (rows.TryGetValue(god, out var row)) row.UpdateFavor(value);
    }

    void OnPassiveActivated(God god)
    {
        if (rows.TryGetValue(god, out var row)) row.SetPassiveActive(true);
    }

    void OnPassiveDeactivated(God god)
    {
        if (rows.TryGetValue(god, out var row)) row.SetPassiveActive(false);
    }

    void OnAvatarStarted(God god)
    {
        if (rows.TryGetValue(god, out var row)) row.StartAvatarTimer(FavorManager.AvatarDuration);
    }

    void OnAvatarEnded(God god)
    {
        if (rows.TryGetValue(god, out var row)) row.StopAvatarTimer();
    }

    void OnThresholdReached(God god, string threshold)
    {
        if (rows.TryGetValue(god, out var row)) row.FlashThreshold(threshold);
    }

    void OnSynergyActivated(string id, string displayName)
    {
        // Beide beteiligten Götter kurz aufleuchten
        // (Synergie-IDs enthalten Gott-Namen indirekt — vereinfachte Lösung:
        //  alle aktiven Synergien prüfen und beteiligte Rows markieren)
        foreach (var syn in SynergySystem.Instance.GetActiveSynergies())
        {
            if (rows.TryGetValue(syn.GodA, out var rA)) rA.PulseGold();
            if (rows.TryGetValue(syn.GodB, out var rB)) rB.PulseGold();
        }
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// FavorRowUI.cs — eine einzelne Gott-Leiste
// ─────────────────────────────────────────────────────────────────────────────

public class FavorRowUI : MonoBehaviour
{
    [Header("References")]
    public Image           godIcon;
    public TextMeshProUGUI godNameText;
    public Image           favorBarFill;
    public TextMeshProUGUI favorValueText;
    public Image           statusIcon;
    public Image           avatarTimerRing;

    // Threshold-Marker (25 / 50 / 75)
    public RectTransform marker25, marker50, marker75;

    // Farben pro Gott (im Inspector oder via Code)
    static readonly Color[] GodColors = new Color[]
    {
        new Color(0.95f, 0.88f, 0.20f),  // Zeus   — Blitzgelb
        new Color(0.40f, 0.72f, 0.95f),  // Athena — Himmelblau
        new Color(0.90f, 0.20f, 0.15f),  // Ares   — Blutrot
        new Color(0.25f, 0.65f, 0.90f),  // Poseidon — Meeresblau
        new Color(0.55f, 0.20f, 0.80f),  // Hades  — Dunkelviolett
        new Color(0.95f, 0.55f, 0.10f),  // Hephaistos — Feuer-Orange
    };

    God  myGod;
    bool passiveActive  = false;
    bool avatarRunning  = false;
    float avatarDuration, avatarRemaining;

    public void Setup(God god, string name, Sprite icon)
    {
        myGod = god;
        if (godNameText != null) godNameText.text = name.ToUpper();
        if (godIcon     != null && icon != null) godIcon.sprite = icon;

        // Bar-Farbe setzen
        Color c = GodColors[(int)god];
        if (favorBarFill != null) favorBarFill.color = c;

        // Threshold-Marker positionieren (RectTransform.anchorMin.x)
        PositionMarker(marker25, 0.25f);
        PositionMarker(marker50, 0.50f);
        PositionMarker(marker75, 0.75f);

        if (avatarTimerRing != null) { avatarTimerRing.fillAmount = 0f; avatarTimerRing.enabled = false; }
        UpdateFavor(0f);
    }

    void PositionMarker(RectTransform marker, float t)
    {
        if (marker == null) return;
        var anch = marker.anchorMin;
        anch.x = t;
        marker.anchorMin = anch;
        var anchMax = marker.anchorMax;
        anchMax.x = t;
        marker.anchorMax = anchMax;
    }

    public void UpdateFavor(float value)
    {
        float ratio = value / 100f;
        if (favorBarFill  != null) favorBarFill.fillAmount  = ratio;
        if (favorValueText != null) favorValueText.text      = Mathf.FloorToInt(value).ToString();
    }

    public void SetPassiveActive(bool active)
    {
        passiveActive = active;
        if (statusIcon != null)
        {
            statusIcon.enabled = active;
            if (active) StartCoroutine(PulseStatusIcon());
        }
    }

    public void StartAvatarTimer(float duration)
    {
        avatarDuration  = duration;
        avatarRemaining = duration;
        avatarRunning   = true;
        if (avatarTimerRing != null) { avatarTimerRing.enabled = true; avatarTimerRing.fillAmount = 1f; }
    }

    public void StopAvatarTimer()
    {
        avatarRunning = false;
        if (avatarTimerRing != null) { avatarTimerRing.enabled = false; avatarTimerRing.fillAmount = 0f; }
    }

    void Update()
    {
        if (!avatarRunning) return;
        avatarRemaining -= Time.deltaTime;
        float ratio = Mathf.Clamp01(avatarRemaining / avatarDuration);
        if (avatarTimerRing != null) avatarTimerRing.fillAmount = ratio;
        if (avatarRemaining <= 0f) StopAvatarTimer();
    }

    public void FlashThreshold(string threshold)
    {
        StartCoroutine(ThresholdFlash());
    }

    IEnumerator ThresholdFlash()
    {
        if (favorBarFill == null) yield break;
        Color orig = favorBarFill.color;
        for (int i = 0; i < 3; i++)
        {
            favorBarFill.color = Color.white;
            yield return new WaitForSeconds(0.07f);
            favorBarFill.color = orig;
            yield return new WaitForSeconds(0.07f);
        }
    }

    public void PulseGold()
    {
        StartCoroutine(GoldPulse());
    }

    IEnumerator GoldPulse()
    {
        if (favorBarFill == null) yield break;
        Color orig = favorBarFill.color;
        Color gold = new Color(1f, 0.85f, 0.2f);
        float t = 0f;
        while (t < 0.5f)
        {
            t += Time.deltaTime;
            favorBarFill.color = Color.Lerp(gold, orig, t / 0.5f);
            yield return null;
        }
        favorBarFill.color = orig;
    }

    IEnumerator PulseStatusIcon()
    {
        if (statusIcon == null) yield break;
        Vector3 baseScale = Vector3.one;
        float t = 0f;
        while (t < 0.3f)
        {
            t += Time.deltaTime;
            float s = 1f + Mathf.Sin(t * Mathf.PI / 0.3f) * 0.4f;
            statusIcon.transform.localScale = baseScale * s;
            yield return null;
        }
        statusIcon.transform.localScale = baseScale;
    }

    IEnumerator PulseStatusIcon_co() => PulseStatusIcon();
}
