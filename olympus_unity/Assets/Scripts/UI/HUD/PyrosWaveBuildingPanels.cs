// PyrosHUDPanel.cs
// Ablegen in: Assets/Scripts/UI/HUD/PyrosHUDPanel.cs
// Oben rechts — zeigt Pyros HP + Ressourcen (Asche, Erz)
//
// Hierarchy:
//   PyrosPanel
//   ├── PyrosHPBar (Slider) + FlameIcon (Image)
//   │   └── HPText (TextMeshProUGUI)
//   ├── AshRow
//   │   ├── AshIcon (Image)
//   │   └── AshText (TextMeshProUGUI)
//   └── OreRow
//       ├── OreIcon (Image)
//       └── OreText (TextMeshProUGUI)

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class PyrosHUDPanel : MonoBehaviour
{
    [Header("Pyros HP")]
    [SerializeField] Slider          pyrosHPBar;
    [SerializeField] Image           pyrosHPFill;
    [SerializeField] TextMeshProUGUI pyrosHPText;

    [Header("Resources")]
    [SerializeField] TextMeshProUGUI ashText;
    [SerializeField] TextMeshProUGUI oreText;

    [Header("Colors")]
    [SerializeField] Color pyrosColorHigh = new Color(0.85f, 0.72f, 0.30f);
    [SerializeField] Color pyrosColorLow  = new Color(0.85f, 0.25f, 0.10f);

    public void Initialize()
    {
        Pyros.OnHpChanged       += UpdatePyrosHP;
        PlayerState.OnAshChanged += UpdateAsh;
        PlayerState.OnOreChanged += UpdateOre;

        UpdateAsh(PlayerState.Instance.ash);
        UpdateOre(PlayerState.Instance.ore);
    }

    void OnDestroy()
    {
        Pyros.OnHpChanged       -= UpdatePyrosHP;
        PlayerState.OnAshChanged -= UpdateAsh;
        PlayerState.OnOreChanged -= UpdateOre;
    }

    void UpdatePyrosHP(float current, float max)
    {
        if (pyrosHPBar == null) return;
        float ratio = max > 0 ? current / max : 0f;
        pyrosHPBar.value = ratio;
        if (pyrosHPFill != null)
            pyrosHPFill.color = Color.Lerp(pyrosColorLow, pyrosColorHigh, ratio);
        if (pyrosHPText != null)
            pyrosHPText.text = $"{Mathf.CeilToInt(current)}";

        // Pulse wenn Pyros in Gefahr
        if (ratio < 0.3f) StartCoroutine(PulsePyros());
    }

    IEnumerator PulsePyros()
    {
        if (pyrosHPFill == null) yield break;
        Color orig = pyrosHPFill.color;
        pyrosHPFill.color = Color.white;
        yield return new WaitForSeconds(0.08f);
        pyrosHPFill.color = orig;
    }

    void UpdateAsh(int amount)
    {
        if (ashText != null) ashText.text = amount.ToString();
    }

    void UpdateOre(int amount)
    {
        if (oreText != null) oreText.text = amount.ToString();
    }
}

// ─────────────────────────────────────────────────────────────────────────────

// WaveHUDPanel.cs
// Ablegen in: Assets/Scripts/UI/HUD/WaveHUDPanel.cs
// Oben Mitte — Wellennummer, Timer, Boss-HP
//
// Hierarchy:
//   WavePanel
//   ├── WaveText (TextMeshProUGUI)    "WELLE 3 / 10"
//   ├── TimerText (TextMeshProUGUI)   "Nächste Welle in 8s"
//   ├── BossPanel (GameObject, aktiv nur während Boss)
//   │   ├── BossNameText (TextMeshProUGUI)
//   │   ├── BossHPBar (Slider)
//   │   └── BossPhaseIndicator (HorizontalLayoutGroup mit 3 Icons)
//   └── EnemiesAliveText (TextMeshProUGUI)

using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WaveHUDPanel : MonoBehaviour
{
    [Header("Wave Info")]
    [SerializeField] TextMeshProUGUI waveText;
    [SerializeField] TextMeshProUGUI timerText;
    [SerializeField] TextMeshProUGUI enemiesAliveText;

    [Header("Boss")]
    [SerializeField] GameObject      bossPanel;
    [SerializeField] TextMeshProUGUI bossNameText;
    [SerializeField] Slider          bossHPBar;
    [SerializeField] Image           bossHPFill;

    float countdownTimer = 0f;
    bool  counting       = false;

    public void Initialize()
    {
        WaveManager.OnWaveStarted    += OnWaveStarted;
        WaveManager.OnWaveCompleted  += OnWaveCompleted;
        WaveManager.OnBossWaveStarted += OnBossWaveStarted;
        GameEvents.OnEnemyKilled     += (_, __) => UpdateEnemiesAlive();

        if (bossPanel != null) bossPanel.SetActive(false);
        UpdateWaveText();
    }

    void OnDestroy()
    {
        WaveManager.OnWaveStarted    -= OnWaveStarted;
        WaveManager.OnWaveCompleted  -= OnWaveCompleted;
        WaveManager.OnBossWaveStarted -= OnBossWaveStarted;
    }

    void Update()
    {
        if (!counting) return;
        countdownTimer -= Time.deltaTime;
        if (timerText != null)
        {
            if (countdownTimer > 0f)
                timerText.text = $"Nächste Welle in {Mathf.CeilToInt(countdownTimer)}s";
            else
            {
                timerText.text = "";
                counting = false;
            }
        }
    }

    void OnWaveStarted(int wave)
    {
        counting = false;
        UpdateWaveText();
        UpdateEnemiesAlive();
        if (timerText != null) timerText.text = "";
    }

    void OnWaveCompleted(int wave)
    {
        countdownTimer = 5f;
        counting       = true;
    }

    void OnBossWaveStarted()
    {
        SetBossMode(true);
        if (bossNameText != null) bossNameText.text = "KRONOS — HERR DER ZEIT";
    }

    public void SetBossMode(bool active)
    {
        if (bossPanel != null) bossPanel.SetActive(active);
    }

    public void UpdateBossHP(float current, float max)
    {
        if (bossHPBar == null) return;
        float ratio = max > 0 ? current / max : 0f;
        bossHPBar.value = ratio;
        // Phasenwechsel-Farben
        Color c = ratio > 0.6f ? new Color(0.6f, 0.1f, 0.7f) :
                  ratio > 0.3f ? new Color(0.9f, 0.3f, 0.1f) :
                                 new Color(1f,   0.1f, 0.1f);
        if (bossHPFill != null) bossHPFill.color = c;
    }

    void UpdateWaveText()
    {
        if (waveText == null) return;
        int cur   = WaveManager.Instance ? WaveManager.Instance.CurrentWave : 0;
        int total = WaveManager.Instance ? WaveManager.Instance.TotalWaves  : 10;
        waveText.text = $"WELLE  {cur} / {total}";
    }

    void UpdateEnemiesAlive()
    {
        if (enemiesAliveText == null || WaveManager.Instance == null) return;
        enemiesAliveText.text = $"Feinde: {WaveManager.Instance.EnemiesAlive}";
    }
}

// ─────────────────────────────────────────────────────────────────────────────

// BuildingHUDPanel.cs
// Ablegen in: Assets/Scripts/UI/HUD/BuildingHUDPanel.cs
// Unten rechts — Schnell-Build-Slots + Hinweise
//
// Hierarchy:
//   BuildingPanel
//   ├── BuildHintText  (TextMeshProUGUI)  "[B] Bauen"
//   └── SmithyHintText (TextMeshProUGUI) "[F] Schmiede"  (ausgegraut wenn keine Schmiede)

using UnityEngine;
using TMPro;

public class BuildingHUDPanel : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI buildHintText;
    [SerializeField] TextMeshProUGUI smithyHintText;
    [SerializeField] UnityEngine.UI.Image smithyIcon;

    [SerializeField] Color activeColor   = new Color(0.85f, 0.72f, 0.30f);
    [SerializeField] Color inactiveColor = new Color(0.4f, 0.4f, 0.4f);

    public void Initialize()
    {
        UpdateSmithyHint();
        // Auf Forge-Events reagieren
        GameEvents.OnBuildingPlaced += (type, _) =>
        {
            if (type.Contains("forge")) UpdateSmithyHint();
        };
    }

    void Update()
    {
        // Jedes Frame prüfen (lightweight)
        bool hasForge = PlayerState.Instance && PlayerState.Instance.hasForge;
        Color c = hasForge ? activeColor : inactiveColor;
        if (smithyHintText != null) smithyHintText.color = c;
        if (smithyIcon     != null) smithyIcon.color     = c;
    }

    void UpdateSmithyHint()
    {
        bool hasForge = PlayerState.Instance && PlayerState.Instance.hasForge;
        if (smithyHintText != null)
        {
            smithyHintText.text  = hasForge ? "[F] Schmiede" : "[F] Schmiede (nicht gebaut)";
            smithyHintText.color = hasForge ? activeColor : inactiveColor;
        }
    }
}
