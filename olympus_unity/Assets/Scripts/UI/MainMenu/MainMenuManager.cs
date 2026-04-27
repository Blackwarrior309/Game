// MainMenuManager.cs
// Ablegen in: Assets/Scripts/UI/MainMenu/MainMenuManager.cs
// Anhängen an: Canvas "MainMenu_Canvas"
//
// Szenen-Hierarchy:
//   MainMenuCanvas
//   ├── TitlePanel         (Logo + Subtitle)
//   ├── MainButtonPanel    (Starten / Upgrades / Beenden)
//   ├── GodSelectPanel     (6 Gott-Karten)
//   └── MetaUpgradePanel   (Meta-Progression, optional)

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

public class MainMenuManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] CanvasGroup titlePanel;
    [SerializeField] CanvasGroup mainButtonPanel;
    [SerializeField] CanvasGroup godSelectPanel;
    [SerializeField] CanvasGroup metaUpgradePanel;

    [Header("Title")]
    [SerializeField] TextMeshProUGUI titleText;       // "OLYMPUS SURVIVORS"
    [SerializeField] TextMeshProUGUI subtitleText;    // "Verteidige die heilige Flamme"

    [Header("Buttons")]
    [SerializeField] Button startButton;
    [SerializeField] Button upgradesButton;
    [SerializeField] Button quitButton;
    [SerializeField] Button backFromGodSelect;

    [Header("God Select")]
    [SerializeField] Transform       godCardContainer;
    [SerializeField] GameObject      godCardPrefab;
    [SerializeField] TextMeshProUGUI selectedGodNameText;
    [SerializeField] TextMeshProUGUI selectedGodDescText;
    [SerializeField] Button          confirmGodButton;

    [Header("Settings")]
    [SerializeField] string gameSceneName = "GameScene";

    FavorManager.God selectedGod = FavorManager.God.Zeus;

    // ── God Data ───────────────────────────────────────────────────────────
    static readonly GodSelectData[] GodData = new GodSelectData[]
    {
        new(FavorManager.God.Zeus,
            "ZEUS",
            "Gott des Donners",
            "Blitze vom Himmel. Jeder 10. Angriff trifft alle Feinde\nim Umkreis. Avatar: Unaufhaltsam.",
            new Color(0.95f, 0.88f, 0.20f)),

        new(FavorManager.God.Athena,
            "ATHENA",
            "Göttin der Weisheit",
            "Strategische Überlegenheit. Schutzschild regeneriert\nsich passiv. Barriere um den Pyros.",
            new Color(0.40f, 0.72f, 0.95f)),

        new(FavorManager.God.Ares,
            "ARES",
            "Gott des Krieges",
            "Kills geben Schadens-Boost. Berserker-Modus:\n2× Schaden, 2× Speed. Töte. Töte. Töte.",
            new Color(0.90f, 0.20f, 0.15f)),

        new(FavorManager.God.Poseidon,
            "POSEIDON",
            "Gott des Meeres",
            "Feinde die dich treffen werden verlangsamt.\nFlutwellen und Erdspaltungen erschüttern die Arena.",
            new Color(0.25f, 0.65f, 0.90f)),

        new(FavorManager.God.Hades,
            "HADES",
            "Herr der Unterwelt",
            "Getötete Feinde kehren als Schattenkrieger zurück.\nDu kämpfst nie allein.",
            new Color(0.55f, 0.20f, 0.80f)),

        new(FavorManager.God.Hephaistos,
            "HEPHAISTOS",
            "Gott der Schmiede",
            "Meistere die Schmiede. Schmiedet legendäre Waffen\nund entfessle Vulkane auf deine Feinde.",
            new Color(0.95f, 0.55f, 0.10f)),
    };

    // ── Unity Lifecycle ────────────────────────────────────────────────────
    void Start()
    {
        SetupButtons();
        BuildGodCards();
        StartCoroutine(IntroSequence());
    }

    void SetupButtons()
    {
        startButton?.onClick.AddListener(OnStartPressed);
        upgradesButton?.onClick.AddListener(OnUpgradesPressed);
        quitButton?.onClick.AddListener(OnQuitPressed);
        backFromGodSelect?.onClick.AddListener(OnBackFromGodSelect);
        confirmGodButton?.onClick.AddListener(OnConfirmGod);
    }

    // ── Intro-Sequenz ──────────────────────────────────────────────────────
    IEnumerator IntroSequence()
    {
        // Alle Panels verstecken
        SetAlpha(titlePanel,       0f);
        SetAlpha(mainButtonPanel,  0f);
        SetAlpha(godSelectPanel,   0f);
        if (metaUpgradePanel) SetAlpha(metaUpgradePanel, 0f);

        yield return new WaitForSeconds(0.4f);

        // Titel einblenden
        yield return FadePanel(titlePanel, 0f, 1f, 0.8f);
        yield return new WaitForSeconds(0.3f);

        // Buttons einblenden
        yield return FadePanel(mainButtonPanel, 0f, 1f, 0.5f);
    }

    // ── Button-Handler ─────────────────────────────────────────────────────
    void OnStartPressed()
    {
        StartCoroutine(TransitionToGodSelect());
    }

    IEnumerator TransitionToGodSelect()
    {
        yield return FadePanel(mainButtonPanel, 1f, 0f, 0.25f);
        mainButtonPanel.blocksRaycasts = false;

        SetAlpha(godSelectPanel, 0f);
        godSelectPanel.blocksRaycasts = true;
        yield return FadePanel(godSelectPanel, 0f, 1f, 0.35f);
    }

    void OnBackFromGodSelect()
    {
        StartCoroutine(BackToMainButtons());
    }

    IEnumerator BackToMainButtons()
    {
        yield return FadePanel(godSelectPanel, 1f, 0f, 0.25f);
        godSelectPanel.blocksRaycasts = false;
        mainButtonPanel.blocksRaycasts = true;
        yield return FadePanel(mainButtonPanel, 0f, 1f, 0.35f);
    }

    void OnUpgradesPressed()
    {
        if (metaUpgradePanel == null) return;
        StartCoroutine(TransitionToPanel(mainButtonPanel, metaUpgradePanel));
    }

    void OnQuitPressed()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void OnConfirmGod()
    {
        if (FavorManager.Instance != null)
            FavorManager.Instance.MainGod = selectedGod;

        StartCoroutine(LoadGameScene());
    }

    IEnumerator LoadGameScene()
    {
        yield return FadePanel(godSelectPanel, 1f, 0f, 0.4f);
        // Fade-to-black hier optional über ein separates FullScreenFade-Panel
        SceneManager.LoadScene(gameSceneName);
    }

    // ── Gott-Karten erstellen ──────────────────────────────────────────────
    void BuildGodCards()
    {
        if (godCardContainer == null || godCardPrefab == null) return;

        foreach (var data in GodData)
        {
            var cardGO = Instantiate(godCardPrefab, godCardContainer);
            var card   = cardGO.GetComponent<GodCardUI>();
            card?.Setup(data, OnGodCardSelected);
        }

        // Zeus als Standard vorauswählen
        OnGodCardSelected(GodData[0]);
    }

    void OnGodCardSelected(GodSelectData data)
    {
        selectedGod = data.God;
        if (selectedGodNameText != null) selectedGodNameText.text = data.Name;
        if (selectedGodDescText != null) selectedGodDescText.text = data.Description;

        // Alle Karten deselektieren, gewählte hervorheben
        foreach (Transform child in godCardContainer)
        {
            var card = child.GetComponent<GodCardUI>();
            card?.SetSelected(card.GodId == selectedGod);
        }
    }

    // ── Panel-Transition ───────────────────────────────────────────────────
    IEnumerator TransitionToPanel(CanvasGroup from, CanvasGroup to)
    {
        yield return FadePanel(from, 1f, 0f, 0.25f);
        from.blocksRaycasts = false;
        to.blocksRaycasts   = true;
        yield return FadePanel(to,   0f, 1f, 0.35f);
    }

    IEnumerator FadePanel(CanvasGroup group, float from, float to, float duration)
    {
        if (group == null) yield break;
        float t = 0f;
        group.alpha = from;
        while (t < duration)
        {
            t += Time.deltaTime;
            group.alpha = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }
        group.alpha = to;
    }

    void SetAlpha(CanvasGroup group, float alpha)
    {
        if (group == null) return;
        group.alpha = alpha;
        group.blocksRaycasts = alpha > 0f;
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// Datenklasse für Gott-Auswahl
// ─────────────────────────────────────────────────────────────────────────────
[System.Serializable]
public class GodSelectData
{
    public FavorManager.God God;
    public string           Name;
    public string           Subtitle;
    public string           Description;
    public Color            AccentColor;

    public GodSelectData(FavorManager.God god, string name, string subtitle, string desc, Color color)
    { God = god; Name = name; Subtitle = subtitle; Description = desc; AccentColor = color; }
}
