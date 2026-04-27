// BuildMenuController.cs
// Ablegen in: Assets/Scripts/UI/BuildMenu/BuildMenuController.cs
// In-Game Baumenü — öffnet mit Taste B, Spiel läuft weiter (kein Pause)
//
// Canvas Hierarchy:
//   BuildMenu_Canvas (CanvasGroup, Sort Order 15)
//   ├── Background (Image, halbtransparent)
//   ├── Title (TextMeshProUGUI) "BAUEN [B]"
//   ├── TempleSlotInfo (TextMeshProUGUI) "Tempel: 2/3"
//   ├── CategoryTabs (HorizontalLayoutGroup)
//   │   ├── Tab_Defense, Tab_Attack, Tab_Support, Tab_Temple
//   └── BuildingGrid (GridLayoutGroup)
//       └── BuildingCard.prefab × N

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class BuildMenuController : MonoBehaviour
{
    public static BuildMenuController Instance { get; private set; }

    [Header("UI Refs")]
    [SerializeField] CanvasGroup         canvasGroup;
    [SerializeField] TextMeshProUGUI     templeSlotText;
    [SerializeField] Transform           buildingGrid;
    [SerializeField] GameObject          buildingCardPrefab;
    [SerializeField] GameObject          ghostPrefab;          // Platzierungs-Vorschau

    [Header("Category Buttons")]
    [SerializeField] Button btnDefense;
    [SerializeField] Button btnAttack;
    [SerializeField] Button btnSupport;
    [SerializeField] Button btnTemple;

    // ── Gebäude-Daten ──────────────────────────────────────────────────────
    public enum BuildCategory { Defense, Attack, Support, Temple }

    public class BuildingData
    {
        public string        Id;
        public string        DisplayName;
        public string        Description;
        public int           AshCost;
        public int           OreCost;
        public BuildCategory Category;
        public string        IconEmoji;
        public System.Func<bool> CanBuild;   // Validierungs-Funktion

        public BuildingData(string id, string name, string desc, int ash, int ore,
                            BuildCategory cat, string icon, System.Func<bool> canBuild = null)
        {
            Id = id; DisplayName = name; Description = desc;
            AshCost = ash; OreCost = ore; Category = cat; IconEmoji = icon;
            CanBuild = canBuild ?? (() => true);
        }
    }

    static readonly List<BuildingData> AllBuildings = new()
    {
        // Verteidigung
        new("palisade",    "Palisade",     "Blockiert Feindpfade. Schnell gebaut.",            20, 0, BuildCategory.Defense,  "🪵"),
        new("stone_wall",  "Steinmauer",   "Starke Blockade mit hohen HP.",                    50, 0, BuildCategory.Defense,  "🧱"),

        // Angriff
        new("archer_tower","Bogenturm",    "Auto-Angriff auf nächsten Feind. Schnelle Rate.",   60, 0, BuildCategory.Attack,   "🏹"),
        new("catapult",    "Steinschleuder","AoE-Schaden, langsamer Beschuss.",                 90, 0, BuildCategory.Attack,   "⚙️"),
        new("fire_tower",  "Feuerturm",    "Flächenschaden + Feuer-DoT.",                       80, 0, BuildCategory.Attack,   "🔥"),

        // Support
        new("healing_shrine","Heilungsschrein","Regeneriert Pyros HP passiv.",                  70, 0, BuildCategory.Support,  "💚"),
        new("resource_altar","Ressourcen-Altar","Generiert Asche passiv.",                      40, 0, BuildCategory.Support,  "🏺"),
        new("sacrifice_altar","Opferaltar", "Opfere Asche für Göttergunst.",                    50, 0, BuildCategory.Support,  "🔮"),

        // Schmiede (kein Tempel-Slot)
        new("forge",       "Hephaistos-Schmiede","Waffen upgraden + Legendäre schmieden.",     120,30, BuildCategory.Support,  "⚒️",
            () => !PlayerState.Instance.hasForge),

        // Tempel (max 3 gleichzeitig)
        new("temple_zeus",    "Tempel des Zeus",    "Blitze alle 20s, +15% Blitzschaden.",    150, 0, BuildCategory.Temple,   "⚡",
            () => PlayerState.Instance.activeTemples < PlayerState.MaxTemples
               && !FavorManager.Instance.IsTempleBuilt(FavorManager.God.Zeus)),

        new("temple_athena",  "Tempel der Athena",  "Gebäude +30% HP, Türme Stufe+1.",        150, 0, BuildCategory.Temple,   "🛡",
            () => PlayerState.Instance.activeTemples < PlayerState.MaxTemples
               && !FavorManager.Instance.IsTempleBuilt(FavorManager.God.Athena)),

        new("temple_ares",    "Tempel des Ares",    "Respawn mit 50% HP.",                     150, 0, BuildCategory.Temple,   "🗡",
            () => PlayerState.Instance.activeTemples < PlayerState.MaxTemples
               && !FavorManager.Instance.IsTempleBuilt(FavorManager.God.Ares)),

        new("temple_poseidon","Tempel des Poseidon","Wachsender Wasserbereich, -20% Feindspeed.",150,0,BuildCategory.Temple,  "🌊",
            () => PlayerState.Instance.activeTemples < PlayerState.MaxTemples
               && !FavorManager.Instance.IsTempleBuilt(FavorManager.God.Poseidon)),

        new("temple_hades",   "Tempel des Hades",   "Schatten-Spawn alle 45s.",               150, 0, BuildCategory.Temple,   "💀",
            () => PlayerState.Instance.activeTemples < PlayerState.MaxTemples
               && !FavorManager.Instance.IsTempleBuilt(FavorManager.God.Hades)),
    };

    // ── State ──────────────────────────────────────────────────────────────
    bool            isOpen        = false;
    BuildCategory   activeCategory = BuildCategory.Attack;
    BuildingData    selectedBuilding;
    GameObject      placementGhost;
    bool            isPlacing     = false;

    // Prefab-Map (im Inspector oder Resources-Ordner)
    [SerializeField] List<BuildingPrefabEntry> buildingPrefabs;

    [System.Serializable]
    public class BuildingPrefabEntry
    {
        public string     Id;
        public GameObject Prefab;
    }

    // ── Unity ─────────────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        if (canvasGroup != null) { canvasGroup.alpha = 0f; canvasGroup.blocksRaycasts = false; }

        btnDefense?.onClick.AddListener(() => ShowCategory(BuildCategory.Defense));
        btnAttack?.onClick.AddListener(()  => ShowCategory(BuildCategory.Attack));
        btnSupport?.onClick.AddListener(() => ShowCategory(BuildCategory.Support));
        btnTemple?.onClick.AddListener(()  => ShowCategory(BuildCategory.Temple));

        GameEvents.OnBuildMenuToggle += Toggle;
    }

    void OnDestroy() => GameEvents.OnBuildMenuToggle -= Toggle;

    void Update()
    {
        if (isPlacing) UpdatePlacementGhost();
        if (Input.GetKeyDown(KeyCode.Escape) && isOpen) Close();
    }

    // ── Öffnen / Schließen ─────────────────────────────────────────────────
    public void Toggle()
    {
        if (isOpen) Close(); else Open();
    }

    void Open()
    {
        isOpen = true;
        ShowCategory(activeCategory);
        UpdateTempleSlotText();
        StartCoroutine(Fade(0f, 1f, 0.15f));
        if (canvasGroup != null) canvasGroup.blocksRaycasts = true;
    }

    void Close()
    {
        isOpen = false;
        StartCoroutine(Fade(1f, 0f, 0.12f));
        if (canvasGroup != null) canvasGroup.blocksRaycasts = false;
        CancelPlacement();
    }

    IEnumerator Fade(float from, float to, float dur)
    {
        if (canvasGroup == null) yield break;
        float t = 0f;
        while (t < dur) { t += Time.deltaTime; canvasGroup.alpha = Mathf.Lerp(from, to, t / dur); yield return null; }
        canvasGroup.alpha = to;
    }

    // ── Kategorie anzeigen ─────────────────────────────────────────────────
    void ShowCategory(BuildCategory cat)
    {
        activeCategory = cat;

        // Alte Karten entfernen
        foreach (Transform child in buildingGrid) Destroy(child.gameObject);

        // Neue Karten
        foreach (var bld in AllBuildings)
        {
            if (bld.Category != cat) continue;

            var cardGO = Instantiate(buildingCardPrefab, buildingGrid);
            var card   = cardGO.GetComponent<BuildingCardUI>();
            if (card != null)
                card.Setup(bld, OnBuildingCardClicked);
        }
    }

    void UpdateTempleSlotText()
    {
        if (templeSlotText != null)
            templeSlotText.text =
                $"Tempel: {PlayerState.Instance.activeTemples} / {PlayerState.MaxTemples}";
    }

    // ── Karte geklickt → Platzierungs-Modus ───────────────────────────────
    void OnBuildingCardClicked(BuildingData data)
    {
        // Kosten prüfen
        if (!PlayerState.Instance.SpendAsh(0) && PlayerState.Instance.ash < data.AshCost) return;
        if (data.OreCost > 0 && PlayerState.Instance.ore < data.OreCost)        return;
        if (!data.CanBuild()) return;

        selectedBuilding = data;
        StartPlacement(data);
        Close();
    }

    // ── Platzierungs-Ghost ─────────────────────────────────────────────────
    void StartPlacement(BuildingData data)
    {
        if (ghostPrefab == null) return;
        isPlacing      = true;
        placementGhost = Instantiate(ghostPrefab);

        // Ghost einfärben (grün = platzierbar, rot = blockiert)
        var renderer = placementGhost.GetComponent<Renderer>();
        if (renderer != null)
            renderer.material.color = new Color(0f, 1f, 0f, 0.4f);
    }

    void UpdatePlacementGhost()
    {
        // Maus-Position auf Bodenhöhe projizieren
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 200f, LayerMask.GetMask("Ground")))
        {
            if (placementGhost != null)
                placementGhost.transform.position = hit.point;

            // Platzieren mit linker Maustaste
            if (Input.GetMouseButtonDown(0))
                PlaceBuilding(hit.point);

            // Abbrechen mit rechter Maustaste
            if (Input.GetMouseButtonDown(1))
                CancelPlacement();
        }
    }

    void PlaceBuilding(Vector3 position)
    {
        if (selectedBuilding == null) return;

        // Kosten abziehen
        if (!PlayerState.Instance.SpendAsh(selectedBuilding.AshCost))   { CancelPlacement(); return; }
        if (selectedBuilding.OreCost > 0 &&
            !PlayerState.Instance.SpendOre(selectedBuilding.OreCost))   { CancelPlacement(); return; }

        // Gebäude-Prefab instanziieren
        var prefab = GetPrefab(selectedBuilding.Id);
        if (prefab != null)
        {
            var building = Instantiate(prefab, position, Quaternion.identity);
            building.GetComponent<BuildingBase>()?.StartBuilding();
            GameEvents.RaiseBuildingPlaced(selectedBuilding.Id, position);
        }

        CancelPlacement();
    }

    void CancelPlacement()
    {
        isPlacing        = false;
        selectedBuilding = null;
        if (placementGhost != null) { Destroy(placementGhost); placementGhost = null; }
    }

    GameObject GetPrefab(string id)
    {
        foreach (var entry in buildingPrefabs)
            if (entry.Id == id) return entry.Prefab;
        return null;
    }
}
