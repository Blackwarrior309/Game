// AudioManager.cs
// Ablegen in: Assets/Scripts/Core/AudioManager.cs
// Anhängen an: Singletons-GameObject (DontDestroyOnLoad)
//
// Zentrale SFX- und Musik-Steuerung. Hört auf GameEvents + die typed
// Events der einzelnen Manager und spielt passende Clips ab. Clips werden
// im Inspector zugewiesen — null-Clips werden stillschweigend ignoriert,
// damit das System auch ohne fertige Audio-Files läuft.
//
// SFX laufen über einen kleinen AudioSource-Pool (Round-Robin), damit
// gleichzeitige Sounds (z.B. mehrere Kills im Kombo) nicht truncaten.
// Musik hat eine separate Source mit Loop + simpler State-Machine
// (Calm → Wave → Boss).

using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    // ── SFX-Clips ──────────────────────────────────────────────────────────
    [Header("Combat-SFX")]
    [SerializeField] AudioClip enemyHitClip;
    [SerializeField] AudioClip enemyKillClip;
    [SerializeField] AudioClip playerDeathClip;
    [SerializeField] AudioClip lightningClip;       // Zeus-Passiv / Avatar
    [SerializeField] AudioClip lavaSplashClip;      // Hephaistos Vulkan-Zorn

    [Header("Pickup / Resources")]
    [SerializeField] AudioClip pickupAshClip;
    [SerializeField] AudioClip pickupOreClip;
    [SerializeField] AudioClip pickupXpClip;
    [SerializeField] AudioClip levelUpClip;

    [Header("Building / Smithy")]
    [SerializeField] AudioClip buildingPlaceClip;
    [SerializeField] AudioClip forgeHammerClip;
    [SerializeField] AudioClip sacrificeClip;

    [Header("Götter / Synergien")]
    [SerializeField] AudioClip thresholdPingClip;   // Favor-Schwelle erreicht
    [SerializeField] AudioClip avatarSpawnClip;
    [SerializeField] AudioClip synergyActivatedClip;

    [Header("Wellen / Boss / End")]
    [SerializeField] AudioClip waveStartClip;
    [SerializeField] AudioClip bossSpawnClip;
    [SerializeField] AudioClip gameOverClip;
    [SerializeField] AudioClip gameWonClip;

    // ── Musik ──────────────────────────────────────────────────────────────
    [Header("Musik")]
    [SerializeField] AudioClip musicCalm;
    [SerializeField] AudioClip musicWave;
    [SerializeField] AudioClip musicBoss;
    [SerializeField, Range(0f, 1f)] float musicVolume = 0.45f;

    // ── Pool ───────────────────────────────────────────────────────────────
    [Header("SFX-Pool")]
    [SerializeField] int sourcePoolSize = 8;
    [SerializeField, Range(0f, 1f)] float sfxVolume = 1f;

    AudioSource[] sfxPool;
    int           sfxIdx;
    AudioSource   musicSource;

    // ── Unity Lifecycle ────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        sfxPool = new AudioSource[sourcePoolSize];
        for (int i = 0; i < sourcePoolSize; i++)
        {
            var go = new GameObject($"SFX_{i}");
            go.transform.SetParent(transform);
            sfxPool[i] = go.AddComponent<AudioSource>();
            sfxPool[i].playOnAwake = false;
        }

        var musicGO = new GameObject("Music");
        musicGO.transform.SetParent(transform);
        musicSource = musicGO.AddComponent<AudioSource>();
        musicSource.loop      = true;
        musicSource.volume    = musicVolume;
        musicSource.playOnAwake = false;
    }

    void OnEnable()
    {
        GameEvents.OnEnemyKilled         += HandleEnemyKilled;
        GameEvents.OnPlayerAttacked      += HandlePlayerAttacked;
        GameEvents.OnBuildingPlaced      += HandleBuildingPlaced;
        GameEvents.OnGameOver            += HandleGameOver;
        GameEvents.OnGameWon             += HandleGameWon;
        PlayerState.OnLevelUp            += HandleLevelUp;
        PlayerState.OnPlayerDied         += HandlePlayerDied;
        PlayerState.OnAshChanged         += HandleAshChanged;
        PlayerState.OnOreChanged         += HandleOreChanged;
        FavorManager.OnThresholdReached  += HandleThreshold;
        FavorManager.OnAvatarStarted     += HandleAvatarStarted;
        SynergySystem.OnSynergyActivated += HandleSynergy;
        WaveManager.OnWaveStarted        += HandleWaveStarted;
        WaveManager.OnBossWaveStarted    += HandleBossWaveStarted;
        WaveManager.OnAllWavesCompleted  += HandleAllWavesCompleted;
    }

    void OnDisable()
    {
        GameEvents.OnEnemyKilled         -= HandleEnemyKilled;
        GameEvents.OnPlayerAttacked      -= HandlePlayerAttacked;
        GameEvents.OnBuildingPlaced      -= HandleBuildingPlaced;
        GameEvents.OnGameOver            -= HandleGameOver;
        GameEvents.OnGameWon             -= HandleGameWon;
        PlayerState.OnLevelUp            -= HandleLevelUp;
        PlayerState.OnPlayerDied         -= HandlePlayerDied;
        PlayerState.OnAshChanged         -= HandleAshChanged;
        PlayerState.OnOreChanged         -= HandleOreChanged;
        FavorManager.OnThresholdReached  -= HandleThreshold;
        FavorManager.OnAvatarStarted     -= HandleAvatarStarted;
        SynergySystem.OnSynergyActivated -= HandleSynergy;
        WaveManager.OnWaveStarted        -= HandleWaveStarted;
        WaveManager.OnBossWaveStarted    -= HandleBossWaveStarted;
        WaveManager.OnAllWavesCompleted  -= HandleAllWavesCompleted;
    }

    // ── Public API (für Stellen ohne passendes Event, z.B. Schmiede) ───────
    public void Play(AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null || sfxPool == null || sfxPool.Length == 0) return;
        var src = sfxPool[sfxIdx];
        sfxIdx = (sfxIdx + 1) % sfxPool.Length;
        src.PlayOneShot(clip, volumeScale * sfxVolume);
    }

    public void PlayMusic(AudioClip clip)
    {
        if (musicSource == null || clip == null) return;
        if (musicSource.clip == clip && musicSource.isPlaying) return;
        musicSource.clip = clip;
        musicSource.volume = musicVolume;
        musicSource.Play();
    }

    public void StopMusic()
    {
        if (musicSource != null) musicSource.Stop();
    }

    // Direkter Hook für die Schmiede (Hammer-Funken haben kein passendes Event).
    public void PlayForgeHammer() => Play(forgeHammerClip);
    public void PlaySacrifice()   => Play(sacrificeClip);

    // ── Event-Handler ──────────────────────────────────────────────────────
    void HandleEnemyKilled(GameObject _, Vector3 __) => Play(enemyKillClip, 0.7f);
    void HandlePlayerAttacked(GameObject _)          => Play(enemyHitClip, 0.5f);
    void HandleBuildingPlaced(string _, Vector3 __)  => Play(buildingPlaceClip);
    void HandleGameOver(string _)                    { Play(gameOverClip); StopMusic(); }
    void HandleGameWon()                             { Play(gameWonClip); StopMusic(); }
    void HandleLevelUp(int _)                        => Play(levelUpClip);
    void HandlePlayerDied()                          => Play(playerDeathClip);
    void HandleThreshold(FavorManager.God _, string __) => Play(thresholdPingClip, 0.7f);
    void HandleAvatarStarted(FavorManager.God _)     => Play(avatarSpawnClip);
    void HandleSynergy(string _, string __)          => Play(synergyActivatedClip);
    void HandleAllWavesCompleted()                   => Play(gameWonClip);

    // Pickup-Sounds nur bei Zuwachs (Verbrauch ignorieren)
    int lastAsh = 0;
    int lastOre = 0;
    void HandleAshChanged(int amount) { if (amount > lastAsh) Play(pickupAshClip, 0.4f); lastAsh = amount; }
    void HandleOreChanged(int amount) { if (amount > lastOre) Play(pickupOreClip, 0.5f); lastOre = amount; }

    // Wellen-State-Machine
    void HandleWaveStarted(int wave)
    {
        Play(waveStartClip);
        PlayMusic(musicWave);
    }

    void HandleBossWaveStarted()
    {
        Play(bossSpawnClip);
        PlayMusic(musicBoss);
    }
}
