// HUDManager.cs
// Ablegen in: Assets/Scripts/UI/HUD/HUDManager.cs
// Anhängen an: Canvas GameObject "HUD_Canvas"
// Canvas: Screen Space - Overlay, Sort Order 10

using UnityEngine;

public class HUDManager : MonoBehaviour
{
    public static HUDManager Instance { get; private set; }

    [Header("HUD Panels")]
    [SerializeField] PlayerHUDPanel    playerPanel;     // oben links
    [SerializeField] PyrosHUDPanel     pyrosPanel;      // oben rechts
    [SerializeField] WaveHUDPanel      wavePanel;       // oben mitte
    [SerializeField] FavorHUDPanel     favorPanel;      // unten links
    [SerializeField] WeaponHUDPanel    weaponPanel;     // unten mitte
    [SerializeField] BuildingHUDPanel  buildingPanel;   // unten rechts
    [SerializeField] SynergyNotification synergyNotification; // mitte screen

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        // Alle Sub-Panels initialisieren
        playerPanel?.Initialize();
        pyrosPanel?.Initialize();
        wavePanel?.Initialize();
        favorPanel?.Initialize();
        weaponPanel?.Initialize();
        buildingPanel?.Initialize();

        // Synergie-Notification abonnieren
        SynergySystem.OnSynergyActivated += OnSynergyActivated;
    }

    void OnDestroy()
    {
        SynergySystem.OnSynergyActivated -= OnSynergyActivated;
    }

    void OnSynergyActivated(string id, string displayName)
    {
        synergyNotification?.Show(displayName);
    }

    // ── Boss-HP-Leiste ein/ausblenden ──────────────────────────────────────
    public void ShowBossPanel(bool show)
    {
        wavePanel?.SetBossMode(show);
    }
}
