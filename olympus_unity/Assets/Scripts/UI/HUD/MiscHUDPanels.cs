// SynergyNotification.cs
// Ablegen in: Assets/Scripts/UI/HUD/SynergyNotification.cs
// Mitte Screen — kurze Einblendung bei Synergie-Aktivierung
//
// Hierarchy:
//   SynergyNotification (CanvasGroup)
//   ├── BG (Image, halbtransparent)
//   ├── SynergyIcon (Image, 48×48)
//   ├── TitleText (TextMeshProUGUI) "SYNERGIE AKTIV"
//   └── NameText  (TextMeshProUGUI) "⚡ GEWITTERFLUT"

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class SynergyNotification : MonoBehaviour
{
    [SerializeField] CanvasGroup     canvasGroup;
    [SerializeField] TextMeshProUGUI nameText;
    [SerializeField] RectTransform   panel;

    [SerializeField] float displayDuration = 2.5f;
    [SerializeField] float fadeDuration    = 0.3f;

    Coroutine activeCoroutine;

    void Awake()
    {
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup != null) canvasGroup.alpha = 0f;
    }

    public void Show(string displayName)
    {
        if (activeCoroutine != null) StopCoroutine(activeCoroutine);
        activeCoroutine = StartCoroutine(ShowCoroutine(displayName));
    }

    IEnumerator ShowCoroutine(string displayName)
    {
        if (nameText != null) nameText.text = displayName.ToUpper();

        // Panel von unten einfahren
        if (panel != null)
        {
            panel.anchoredPosition = new Vector2(0f, -30f);
            StartCoroutine(SlideIn());
        }

        // Fade In
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            if (canvasGroup != null) canvasGroup.alpha = Mathf.Clamp01(t / fadeDuration);
            yield return null;
        }

        yield return new WaitForSeconds(displayDuration);

        // Fade Out
        t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            if (canvasGroup != null) canvasGroup.alpha = 1f - Mathf.Clamp01(t / fadeDuration);
            yield return null;
        }
        if (canvasGroup != null) canvasGroup.alpha = 0f;
    }

    IEnumerator SlideIn()
    {
        if (panel == null) yield break;
        float t = 0f;
        float dur = 0.25f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float y = Mathf.Lerp(-30f, 0f, t / dur);
            panel.anchoredPosition = new Vector2(0f, y);
            yield return null;
        }
        panel.anchoredPosition = Vector2.zero;
    }
}

// ─────────────────────────────────────────────────────────────────────────────

// WeaponHUDPanel.cs
// Ablegen in: Assets/Scripts/UI/HUD/WeaponHUDPanel.cs
// Unten Mitte — ausgerüstete Waffen (max 2) + Artefakt-Icons
//
// Hierarchy:
//   WeaponPanel
//   ├── Weapon1Slot (Image + UpgradeStars + GoldFrame)
//   ├── Weapon2Slot (Image + UpgradeStars + GoldFrame)
//   └── ArtifactRow (HorizontalLayoutGroup)
//       └── ArtifactIcon (Image × N)

using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WeaponHUDPanel : MonoBehaviour
{
    [Header("Weapon Slots")]
    [SerializeField] Image[]           weaponIcons;       // 2 Slots
    [SerializeField] GameObject[]      legendaryFrames;   // Gold-Rahmen, aktiv bei legendary
    [SerializeField] TextMeshProUGUI[] upgradeStars;      // "★★★"
    [SerializeField] GameObject[]      weaponSlotRoots;

    [Header("Artifacts")]
    [SerializeField] Transform         artifactContainer;
    [SerializeField] GameObject        artifactIconPrefab;

    public void Initialize()
    {
        // Slots initially leer
        foreach (var frame in legendaryFrames)
            if (frame != null) frame.SetActive(false);
    }

    // Aufgerufen wenn Waffe ausgerüstet/geupgradet wird
    public void UpdateWeaponSlot(int slot, Sprite icon, int upgradeLevel, bool isLegendary)
    {
        if (slot >= weaponIcons.Length) return;
        if (weaponIcons[slot] != null)  weaponIcons[slot].sprite = icon;

        if (upgradeStars != null && slot < upgradeStars.Length && upgradeStars[slot] != null)
            upgradeStars[slot].text = new string('★', upgradeLevel);

        if (legendaryFrames != null && slot < legendaryFrames.Length && legendaryFrames[slot] != null)
            legendaryFrames[slot].SetActive(isLegendary);
    }

    public void AddArtifactIcon(Sprite icon)
    {
        if (artifactContainer == null || artifactIconPrefab == null) return;
        var go  = Instantiate(artifactIconPrefab, artifactContainer);
        var img = go.GetComponent<Image>();
        if (img != null && icon != null) img.sprite = icon;
    }
}

// ─────────────────────────────────────────────────────────────────────────────

// LevelUpScreen.cs
// Ablegen in: Assets/Scripts/UI/HUD/LevelUpScreen.cs
// Vollbild-Overlay bei Level-Up — pausiert Time.timeScale nicht (Kein Pause!)
//
// Hierarchy:
//   LevelUpOverlay (CanvasGroup, Sort Order 20)
//   ├── DimBackground (Image, schwarz 60% alpha)
//   ├── TitleText (TextMeshProUGUI) "AUFSTIEG"
//   └── ChoiceContainer (HorizontalLayoutGroup)
//       └── UpgradeCard Prefab × 3

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class LevelUpScreen : MonoBehaviour
{
    [SerializeField] CanvasGroup     canvasGroup;
    [SerializeField] Transform       choiceContainer;
    [SerializeField] GameObject      upgradeCardPrefab;
    [SerializeField] TextMeshProUGUI titleText;

    bool isOpen = false;

    void Awake()
    {
        if (canvasGroup != null) canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
    }

    void OnEnable()  => GameEvents.OnShowLevelUpChoices += ShowChoices;
    void OnDisable() => GameEvents.OnShowLevelUpChoices -= ShowChoices;

    void ShowChoices(List<UpgradeData> choices)
    {
        if (isOpen) return;
        isOpen = true;

        // Alte Karten löschen
        foreach (Transform child in choiceContainer)
            Destroy(child.gameObject);

        // Karten erstellen
        foreach (var upgrade in choices)
        {
            var cardGO = Instantiate(upgradeCardPrefab, choiceContainer);
            var card   = cardGO.GetComponent<UpgradeCardUI>();
            card?.Setup(upgrade, OnChoiceSelected);
        }

        StartCoroutine(FadeIn());
    }

    void OnChoiceSelected(string upgradeId)
    {
        LevelUpSystem.Instance?.ApplyUpgrade(upgradeId);
        StartCoroutine(FadeOut());
    }

    IEnumerator FadeIn()
    {
        canvasGroup.blocksRaycasts = true;
        float t = 0f;
        while (t < 0.2f)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = Mathf.Clamp01(t / 0.2f);
            yield return null;
        }
        canvasGroup.alpha = 1f;
    }

    IEnumerator FadeOut()
    {
        float t = 0f;
        while (t < 0.2f)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = 1f - Mathf.Clamp01(t / 0.2f);
            yield return null;
        }
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        isOpen = false;
    }
}

// ─────────────────────────────────────────────────────────────────────────────

// UpgradeCardUI.cs
// Ablegen in: Assets/Scripts/UI/HUD/UpgradeCardUI.cs
// Auf dem UpgradeCard Prefab
//
// Hierarchy:
//   UpgradeCard (Button)
//   ├── CardBG (Image — Stein-Textur-Look)
//   ├── IconText (TextMeshProUGUI — Emoji-Icon)
//   ├── NameText (TextMeshProUGUI)
//   ├── DescText (TextMeshProUGUI)
//   └── CategoryBadge (Image + Text)

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class UpgradeCardUI : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI iconText;
    [SerializeField] TextMeshProUGUI nameText;
    [SerializeField] TextMeshProUGUI descText;
    [SerializeField] TextMeshProUGUI categoryText;
    [SerializeField] Button          button;
    [SerializeField] Image           cardBG;

    static readonly Color[] CategoryColors = {
        new Color(0.85f, 0.72f, 0.30f),  // Weapon — Gold
        new Color(0.40f, 0.72f, 0.45f),  // Passive — Grün
        new Color(0.50f, 0.50f, 0.80f),  // Building — Blau
    };

    public void Setup(UpgradeData data, Action<string> onSelected)
    {
        if (iconText     != null) iconText.text     = data.iconEmoji;
        if (nameText     != null) nameText.text      = data.displayName.ToUpper();
        if (descText     != null) descText.text      = data.description;
        if (categoryText != null) categoryText.text  = data.category.ToString().ToUpper();

        // Kategorie-Farbe
        int idx = (int)data.category;
        if (cardBG != null && idx < CategoryColors.Length)
        {
            Color c = CategoryColors[idx];
            c.a = 0.15f;
            cardBG.color = c;
        }

        // Hover-Effekt
        if (button != null)
        {
            button.onClick.AddListener(() => onSelected(data.upgradeId));
            var trigger = gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();
            AddHoverEffect(trigger);
        }
    }

    void AddHoverEffect(UnityEngine.EventSystems.EventTrigger trigger)
    {
        var enter = new UnityEngine.EventSystems.EventTrigger.Entry
        { eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter };
        enter.callback.AddListener(_ => transform.localScale = Vector3.one * 1.05f);

        var exit = new UnityEngine.EventSystems.EventTrigger.Entry
        { eventID = UnityEngine.EventSystems.EventTriggerType.PointerExit };
        exit.callback.AddListener(_ => transform.localScale = Vector3.one);

        trigger.triggers.Add(enter);
        trigger.triggers.Add(exit);
    }
}
