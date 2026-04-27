// SmithyMenuController.cs
// Ablegen in: Assets/Scripts/UI/SmithyMenu/SmithyMenuController.cs
// In-Game Schmiedemenü (Taste F) — 3 Tabs, kein Pause, schließt bei Feindkontakt
//
// Canvas Hierarchy:
//   SmithyMenu_Canvas (CanvasGroup, Sort Order 15)
//   ├── Header "⚒ HEPHAISTOS-SCHMIEDE"
//   ├── InfoRow
//   │   ├── FavorInfoText  "Gunst: 65 / 100"
//   │   ├── OreText        "🪨 45 Erz"
//   │   └── LegendaryText  "Legendäre: 1 / 2"
//   ├── TabBar
//   │   ├── BtnUpgrade, BtnCraft, BtnRecipes
//   └── ContentArea
//       ├── UpgradePanel   (ScrollRect)
//       ├── CraftPanel     (ScrollRect)
//       └── RecipesPanel   (ScrollRect)

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class SmithyMenuController : MonoBehaviour
{
    public static SmithyMenuController Instance { get; private set; }

    [Header("Root")]
    [SerializeField] CanvasGroup canvasGroup;

    [Header("Info")]
    [SerializeField] TextMeshProUGUI favorInfoText;
    [SerializeField] TextMeshProUGUI oreText;
    [SerializeField] TextMeshProUGUI legendaryCountText;

    [Header("Panels")]
    [SerializeField] GameObject upgradePanel;
    [SerializeField] GameObject craftPanel;
    [SerializeField] GameObject recipesPanel;
    [SerializeField] Transform  upgradeList;
    [SerializeField] Transform  craftList;
    [SerializeField] Transform  recipesList;

    [Header("Card Prefabs")]
    [SerializeField] GameObject upgradeCardPrefab;
    [SerializeField] GameObject legendaryCardPrefab;

    [Header("Options")]
    [SerializeField] bool closeOnEnemyContact = true;

    bool isOpen    = false;
    enum SmithyTab { Upgrade, Craft, Recipes }
    SmithyTab activeTab = SmithyTab.Upgrade;

    // Temporäre Waffen-Liste — wird später von WeaponManager gesteuert
    readonly List<string> equippedWeapons = new() { "shortsword", "bow" };

    // ─────────────────────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        if (canvasGroup != null) { canvasGroup.alpha = 0f; canvasGroup.blocksRaycasts = false; }
        GameEvents.OnSmithyMenuToggle += Toggle;
    }

    void OnDestroy() => GameEvents.OnSmithyMenuToggle -= Toggle;

    void Update()
    {
        if (!isOpen) return;
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.F)) Close();

        if (closeOnEnemyContact)
        {
            var player = FindObjectOfType<PlayerController>();
            if (player != null && player.NearbyEnemies.Count > 0) Close();
        }
    }

    // ── Öffnen / Schließen ─────────────────────────────────────────────────
    public void Toggle()
    {
        if (!PlayerState.Instance.hasForge) return;
        if (isOpen) Close(); else Open();
    }

    void Open()
    {
        isOpen = true;
        RefreshHeader();
        ShowTab(activeTab);
        StartCoroutine(FadeGroup(0f, 1f, 0.15f));
        if (canvasGroup != null) canvasGroup.blocksRaycasts = true;
    }

    void Close()
    {
        isOpen = false;
        StartCoroutine(FadeGroup(1f, 0f, 0.12f));
        if (canvasGroup != null) canvasGroup.blocksRaycasts = false;
    }

    IEnumerator FadeGroup(float from, float to, float dur)
    {
        if (canvasGroup == null) yield break;
        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(from, to, t / dur);
            yield return null;
        }
        canvasGroup.alpha = to;
    }

    void RefreshHeader()
    {
        float favor = FavorManager.Instance.GetFavor(FavorManager.God.Hephaistos);
        if (favorInfoText    != null) favorInfoText.text    = $"Gunst: {Mathf.FloorToInt(favor)} / 100";
        if (oreText          != null) oreText.text          = $"🪨 {PlayerState.Instance.ore} Erz";
        if (legendaryCountText != null)
        {
            int count = HephaistosForge.Instance?.CraftedLegendaryCount ?? 0;
            legendaryCountText.text = $"Legendäre: {count} / 2";
        }
    }

    // ── Tabs ──────────────────────────────────────────────────────────────
    public void ShowTab(SmithyTab tab)
    {
        activeTab = tab;
        if (upgradePanel != null) upgradePanel.SetActive(tab == SmithyTab.Upgrade);
        if (craftPanel   != null) craftPanel.SetActive(tab == SmithyTab.Craft);
        if (recipesPanel != null) recipesPanel.SetActive(tab == SmithyTab.Recipes);

        switch (tab)
        {
            case SmithyTab.Upgrade:  BuildUpgradePanel(); break;
            case SmithyTab.Craft:    BuildCraftPanel();   break;
            case SmithyTab.Recipes:  BuildRecipesPanel(); break;
        }
    }

    public void OnTabUpgrade()  => ShowTab(SmithyTab.Upgrade);
    public void OnTabCraft()    => ShowTab(SmithyTab.Craft);
    public void OnTabRecipes()  => ShowTab(SmithyTab.Recipes);

    // ── TAB 1: UPGRADE ─────────────────────────────────────────────────────
    void BuildUpgradePanel()
    {
        if (upgradeList == null) return;
        foreach (Transform c in upgradeList) Destroy(c.gameObject);

        var forge = HephaistosForge.Instance;
        if (forge == null) { ShowNoForgeMessage(upgradeList); return; }

        foreach (var weaponId in equippedWeapons)
        {
            var cardGO = Instantiate(upgradeCardPrefab, upgradeList);
            var card   = cardGO.GetComponent<WeaponUpgradeCardUI>();
            card?.Setup(weaponId, forge, OnUpgradeClicked);
        }
    }

    void OnUpgradeClicked(string weaponId)
    {
        var result = HephaistosForge.Instance?.UpgradeWeapon(weaponId);
        if (result?.Success == true)
        {
            RefreshHeader();
            BuildUpgradePanel();
            // Hammer-Funken-Sound wäre hier gut: AudioManager.Play("hammer_spark");
        }
    }

    // ── TAB 2: HERSTELLEN ─────────────────────────────────────────────────
    void BuildCraftPanel()
    {
        if (craftList == null) return;
        foreach (Transform c in craftList) Destroy(c.gameObject);

        var forge = HephaistosForge.Instance;

        foreach (var def in HephaistosForge.LegendaryWeapons)
        {
            var cardGO = Instantiate(legendaryCardPrefab, craftList);
            var card   = cardGO.GetComponent<LegendaryWeaponCardUI>();
            bool canCraft = forge != null && forge.CanCraftLegendary(def);
            card?.Setup(def, canCraft, canCraft ? () => OnCraftClicked(def) : null);
        }
    }

    void OnCraftClicked(HephaistosForge.LegendaryWeaponDef def)
    {
        var result = HephaistosForge.Instance?.CraftLegendary(def);
        if (result?.Success == true)
        {
            RefreshHeader();
            BuildCraftPanel();
            // WeaponManager.Instance?.EquipLegendary(def);
        }
    }

    // ── TAB 3: REZEPTE (immer lesbar) ─────────────────────────────────────
    void BuildRecipesPanel()
    {
        if (recipesList == null) return;
        foreach (Transform c in recipesList) Destroy(c.gameObject);

        foreach (var def in HephaistosForge.LegendaryWeapons)
        {
            var cardGO = Instantiate(legendaryCardPrefab, recipesList);
            var card   = cardGO.GetComponent<LegendaryWeaponCardUI>();
            card?.Setup(def, canCraft: false, onCraft: null, readOnly: true);
        }
    }

    void ShowNoForgeMessage(Transform parent)
    {
        var go  = new GameObject("NoForgeMsg");
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = "Keine Schmiede gebaut.";
        tmp.fontSize  = 14f;
        tmp.color     = new Color(0.6f, 0.5f, 0.4f);
        tmp.alignment = TMPro.TextAlignmentOptions.Center;
        go.transform.SetParent(parent, false);
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// WeaponUpgradeCardUI.cs
// ─────────────────────────────────────────────────────────────────────────────
public class WeaponUpgradeCardUI : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI weaponNameText;
    [SerializeField] TextMeshProUGUI levelText;
    [SerializeField] TextMeshProUGUI costText;
    [SerializeField] TextMeshProUGUI propertiesText;
    [SerializeField] Button          upgradeButton;
    [SerializeField] Image           levelBar;   // Visueller Fortschritt (1/3, 2/3, 3/3)

    public void Setup(string weaponId, HephaistosForge forge, System.Action<string> onUpgrade)
    {
        int  level  = forge.GetWeaponLevel(weaponId);
        int  cost   = forge.GetUpgradeCost(weaponId);
        bool canUp  = forge.CanUpgrade(weaponId);
        var  props  = forge.GetWeaponProperties(weaponId);

        if (weaponNameText != null) weaponNameText.text = weaponId.Replace("_"," ").ToUpper();
        if (levelText      != null) levelText.text      = $"STUFE {level} / 3";
        if (costText       != null) costText.text       = level < 3 ? $"{cost} 🪨 Erz" : "✓ MAX";
        if (levelBar       != null) levelBar.fillAmount = level / 3f;

        if (propertiesText != null)
        {
            if (props.Count > 0)
            {
                var propNames = new List<string>();
                foreach (var p in props) propNames.Add(p.ToString());
                propertiesText.text = string.Join("  ·  ", propNames);
            }
            else
                propertiesText.text = "Noch keine Eigenschaften";
        }

        if (upgradeButton != null)
        {
            upgradeButton.interactable = canUp;
            upgradeButton.onClick.RemoveAllListeners();
            upgradeButton.onClick.AddListener(() => onUpgrade(weaponId));
        }
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// LegendaryWeaponCardUI.cs
// ─────────────────────────────────────────────────────────────────────────────
public class LegendaryWeaponCardUI : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI nameText;
    [SerializeField] TextMeshProUGUI baseText;
    [SerializeField] TextMeshProUGUI costText;
    [SerializeField] TextMeshProUGUI requirementText;
    [SerializeField] TextMeshProUGUI effectText;
    [SerializeField] Button          craftButton;
    [SerializeField] TextMeshProUGUI craftButtonLabel;
    [SerializeField] Image           cardBG;
    [SerializeField] Image           goldBorder;

    public void Setup(HephaistosForge.LegendaryWeaponDef def, bool canCraft,
                      System.Action onCraft, bool readOnly = false)
    {
        if (nameText    != null) nameText.text    = def.DisplayName.ToUpper();
        if (baseText    != null) baseText.text    = $"Basis: {def.BaseWeapon}";
        if (costText    != null) costText.text    = $"{def.EreCost} 🪨 Erz";
        if (effectText  != null) effectText.text  = FormatEffect(def.SpecialEffect);

        string req = def.RequiresTempel
            ? $"Benötigt: {FavorManager.GodNames[(int)def.RequiredGod]}-Tempel"
            : "Kein Tempel benötigt";
        if (requirementText != null) requirementText.text = req;

        // Visuell: verfügbar = golden, nicht verfügbar = gedimmt
        if (cardBG != null)
            cardBG.color = canCraft ? new Color(0.20f, 0.15f, 0.04f) : new Color(0.10f, 0.10f, 0.10f);
        if (goldBorder != null)
            goldBorder.color = canCraft
                ? new Color(0.85f, 0.70f, 0.25f, 1f)
                : new Color(0.35f, 0.30f, 0.20f, 1f);

        if (craftButton != null)
        {
            craftButton.gameObject.SetActive(!readOnly);
            craftButton.interactable = canCraft;
            craftButton.onClick.RemoveAllListeners();
            if (onCraft != null) craftButton.onClick.AddListener(() => onCraft());
        }
        if (craftButtonLabel != null)
            craftButtonLabel.text = canCraft ? "SCHMIEDEN" : "NICHT VERFÜGBAR";
    }

    string FormatEffect(string raw) => raw.Replace("_", " ").ToUpper();
}
