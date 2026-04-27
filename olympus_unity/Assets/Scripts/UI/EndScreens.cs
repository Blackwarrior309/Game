// GameOverScreen.cs
// Ablegen in: Assets/Scripts/UI/GameOverScreen.cs
// Auf einem Canvas mit Sort Order 50 — erscheint über allem
//
// Hierarchy:
//   GameOverCanvas (CanvasGroup)
//   ├── DimBG (Image, schwarz 80%)
//   ├── TitleText (TextMeshProUGUI) — "PYROS GEFALLEN" oder "DU BIST GEFALLEN"
//   ├── SubtitleText (TextMeshProUGUI) — Welche Welle, wie viele Kills
//   ├── StatsPanel
//   │   ├── WaveReachedText
//   │   ├── EnemiesKilledText
//   │   └── TimeText
//   ├── RestartButton (Button)
//   └── MainMenuButton (Button)

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

public class GameOverScreen : MonoBehaviour
{
    [SerializeField] CanvasGroup     canvasGroup;
    [SerializeField] TextMeshProUGUI titleText;
    [SerializeField] TextMeshProUGUI subtitleText;
    [SerializeField] TextMeshProUGUI waveReachedText;
    [SerializeField] TextMeshProUGUI timeText;
    [SerializeField] Button          restartButton;
    [SerializeField] Button          mainMenuButton;

    [SerializeField] string mainMenuScene = "MainMenu";
    [SerializeField] string gameScene     = "GameScene";

    float runStartTime;
    int   enemiesKilled = 0;

    void OnEnable()
    {
        GameEvents.OnGameOver    += Show;
        GameEvents.OnEnemyKilled += (_, __) => enemiesKilled++;
    }

    void OnDisable()
    {
        GameEvents.OnGameOver    -= Show;
        GameEvents.OnEnemyKilled -= (_, __) => enemiesKilled++;
    }

    void Start()
    {
        if (canvasGroup != null) { canvasGroup.alpha = 0f; canvasGroup.blocksRaycasts = false; }
        restartButton?.onClick.AddListener(OnRestart);
        mainMenuButton?.onClick.AddListener(OnMainMenu);
        runStartTime = Time.time;
    }

    void Show(string reason)
    {
        string title = reason switch
        {
            "pyros_destroyed" => "DIE FLAMME ERLOSCH",
            _                 => "DU FIELST",
        };

        if (titleText    != null) titleText.text    = title;
        if (subtitleText != null) subtitleText.text  =
            $"Kronos triumphiert... diesmal.";

        int   wave    = WaveManager.Instance ? WaveManager.Instance.CurrentWave : 0;
        float elapsed = Time.time - runStartTime;
        int   mins    = Mathf.FloorToInt(elapsed / 60f);
        int   secs    = Mathf.FloorToInt(elapsed % 60f);

        if (waveReachedText != null) waveReachedText.text = $"Welle {wave} erreicht";
        if (timeText        != null) timeText.text        = $"{mins:00}:{secs:00}";

        StartCoroutine(FadeIn());
    }

    IEnumerator FadeIn()
    {
        if (canvasGroup == null) yield break;
        canvasGroup.blocksRaycasts = true;
        float t = 0f;
        while (t < 0.5f)
        {
            t += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Clamp01(t / 0.5f);
            yield return null;
        }
        canvasGroup.alpha = 1f;
    }

    void OnRestart()   => SceneManager.LoadScene(gameScene);
    void OnMainMenu()  => SceneManager.LoadScene(mainMenuScene);
}

// ─────────────────────────────────────────────────────────────────────────────

// VictoryScreen.cs
// Ablegen in: Assets/Scripts/UI/VictoryScreen.cs
//
// Hierarchy:
//   VictoryCanvas (CanvasGroup, Sort Order 50)
//   ├── GoldBackground (Image — leuchtend, animiert)
//   ├── TitleText "KRONOS BESIEGT"
//   ├── SubtitleText "Die Götter feiern deinen Sieg"
//   ├── StatsPanel
//   │   ├── PyrosHPRemainingText
//   │   ├── ActiveSynergiesText
//   │   └── OboloiEarnedText
//   ├── ContinueButton
//   └── MainMenuButton

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

public class VictoryScreen : MonoBehaviour
{
    [SerializeField] CanvasGroup     canvasGroup;
    [SerializeField] TextMeshProUGUI titleText;
    [SerializeField] TextMeshProUGUI subtitleText;
    [SerializeField] TextMeshProUGUI pyrosHPText;
    [SerializeField] TextMeshProUGUI synergiesText;
    [SerializeField] TextMeshProUGUI oboloiText;
    [SerializeField] Button          mainMenuButton;
    [SerializeField] string          mainMenuScene = "MainMenu";

    void OnEnable()  => GameEvents.OnGameWon += Show;
    void OnDisable() => GameEvents.OnGameWon -= Show;

    void Start()
    {
        if (canvasGroup != null) { canvasGroup.alpha = 0f; canvasGroup.blocksRaycasts = false; }
        mainMenuButton?.onClick.AddListener(() => SceneManager.LoadScene(mainMenuScene));
    }

    void Show()
    {
        // Oboloi berechnen (vereinfacht)
        var pyros      = FindObjectOfType<Pyros>();
        int activeSyns = SynergySystem.Instance ? SynergySystem.Instance.GetActiveSynergies().Count : 0;
        int oboloi     = 100 + activeSyns * 20;

        if (pyrosHPText  != null) pyrosHPText.text  = pyros ? $"Pyros: {Mathf.CeilToInt(pyros.GetComponent<Pyros>() ? 0 : 0)} HP" : "";
        if (synergiesText != null) synergiesText.text = $"{activeSyns} Synergien aktiv";
        if (oboloiText    != null) oboloiText.text    = $"+ {oboloi} Oboloi";

        StartCoroutine(FadeIn());
    }

    IEnumerator FadeIn()
    {
        if (canvasGroup == null) yield break;
        canvasGroup.blocksRaycasts = true;
        float t = 0f;
        while (t < 0.6f)
        {
            t += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Clamp01(t / 0.6f);
            yield return null;
        }
        canvasGroup.alpha = 1f;
    }
}
