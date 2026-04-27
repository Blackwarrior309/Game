// WaveManager.cs
// Singleton — Wellen-Steuerung (alle 10 Wellen)
// Ablegen in: Assets/Scripts/Core/WaveManager.cs
// Spawn-Punkte: GameObjects mit Tag "SpawnPoint"

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class WaveManager : MonoBehaviour
{
    public static WaveManager Instance { get; private set; }

    // ── Prefab-Referenzen (im Inspector zuweisen) ──────────────────────────
    [Header("Enemy Prefabs")]
    [SerializeField] GameObject satyrPrefab;
    [SerializeField] GameObject golemPrefab;
    [SerializeField] GameObject wraithPrefab;
    [SerializeField] GameObject medusaPrefab;
    [SerializeField] GameObject cyclopsPrefab;
    [SerializeField] GameObject giantPrefab;       // Welle 9 Mini-Boss
    [SerializeField] GameObject kronosPrefab;

    // ── State ──────────────────────────────────────────────────────────────
    public int  CurrentWave    { get; private set; } = 0;
    public int  EnemiesAlive   { get; private set; } = 0;
    public bool WaveInProgress { get; private set; } = false;

    List<Transform> spawnPoints = new();
    Dictionary<string, GameObject> prefabMap = new();

    // ── Events ─────────────────────────────────────────────────────────────
    public static event Action<int>  OnWaveStarted;
    public static event Action<int>  OnWaveCompleted;
    public static event Action       OnAllWavesCompleted;
    public static event Action       OnBossWaveStarted;

    // ── Wellen-Definition ─────────────────────────────────────────────────
    class EnemyGroup
    {
        public string Type;
        public int    Count;
        public float  DelayBetween;
        public EnemyGroup(string t, int c, float d) { Type = t; Count = c; DelayBetween = d; }
    }

    class WaveData
    {
        public float              PauseBefore;
        public bool               IsBossWave;
        public List<EnemyGroup>   Groups;
        public WaveData(float pause, bool boss, List<EnemyGroup> groups)
        { PauseBefore = pause; IsBossWave = boss; Groups = groups; }
    }

    List<WaveData> waveTable;

    void BuildWaveTable()
    {
        waveTable = new List<WaveData>
        {
            // Welle 1
            new(3f,  false, new(){ new("satyr",   8,  0.3f) }),
            // Welle 2
            new(5f,  false, new(){ new("satyr",   12, 0.25f) }),
            // Welle 3
            new(5f,  false, new(){ new("satyr",   10, 0.3f),  new("golem",   2, 1.0f) }),
            // Welle 4 — Mini-Boss
            new(8f,  false, new(){ new("satyr",   6,  0.4f),  new("cyclops", 1, 0.0f) }),
            // Welle 5 — Mini-Boss
            new(5f,  false, new(){ new("satyr",   8,  0.3f),  new("cyclops", 1, 0.0f), new("golem", 3, 0.8f) }),
            // Welle 6
            new(6f,  false, new(){ new("satyr",   10, 0.3f),  new("medusa",  3, 1.0f) }),
            // Welle 7
            new(5f,  false, new(){ new("wraith",  4,  0.5f),  new("medusa",  4, 0.8f), new("satyr", 8, 0.3f) }),
            // Welle 8
            new(5f,  false, new(){ new("golem",   5,  0.8f),  new("wraith",  5, 0.5f), new("medusa",3, 1.0f) }),
            // Welle 9 — Gigant-Vorläufer (Mini-Boss mit Slow-Aura, Vorschau auf Kronos)
            new(10f, false, new(){ new("giant",   1,  0.0f),  new("cyclops", 1,  3.0f), new("satyr", 12, 0.25f) }),
            // Welle 10 — Kronos
            new(15f, true,  new(){ new("kronos",  1,  0.0f) }),
        };
    }

    // ── Unity Lifecycle ────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        BuildWaveTable();
    }

    void Start()
    {
        // Spawn-Punkte sammeln
        var pts = GameObject.FindGameObjectsWithTag("SpawnPoint");
        foreach (var p in pts) spawnPoints.Add(p.transform);

        // Prefab-Map aufbauen
        prefabMap["satyr"]   = satyrPrefab;
        prefabMap["golem"]   = golemPrefab;
        prefabMap["wraith"]  = wraithPrefab;
        prefabMap["medusa"]  = medusaPrefab;
        prefabMap["cyclops"] = cyclopsPrefab;
        prefabMap["giant"]   = giantPrefab;
        prefabMap["kronos"]  = kronosPrefab;

        GameEvents.OnEnemyKilled += OnEnemyDied;
    }

    void OnDestroy() => GameEvents.OnEnemyKilled -= OnEnemyDied;

    // ── Spiel starten ──────────────────────────────────────────────────────
    public void StartGame()
    {
        CurrentWave = 0; EnemiesAlive = 0;
        StartCoroutine(RunNextWave());
    }

    // ── Wellen-Loop ────────────────────────────────────────────────────────
    IEnumerator RunNextWave()
    {
        if (CurrentWave >= waveTable.Count) { OnAllWavesCompleted?.Invoke(); yield break; }

        var wave = waveTable[CurrentWave];
        yield return new WaitForSeconds(wave.PauseBefore);

        CurrentWave++;
        WaveInProgress = true;
        OnWaveStarted?.Invoke(CurrentWave);
        if (wave.IsBossWave) OnBossWaveStarted?.Invoke();

        yield return StartCoroutine(SpawnWave(wave.Groups));
    }

    IEnumerator SpawnWave(List<EnemyGroup> groups)
    {
        foreach (var group in groups)
        {
            if (!prefabMap.TryGetValue(group.Type, out var prefab) || prefab == null)
            { Debug.LogWarning($"WaveManager: Kein Prefab für '{group.Type}'"); continue; }

            for (int i = 0; i < group.Count; i++)
            {
                SpawnEnemy(prefab);
                EnemiesAlive++;
                if (group.DelayBetween > 0f)
                    yield return new WaitForSeconds(group.DelayBetween);
            }
        }
    }

    void SpawnEnemy(GameObject prefab)
    {
        if (spawnPoints.Count == 0) { Debug.LogWarning("WaveManager: Keine Spawn-Punkte!"); return; }
        var pt = spawnPoints[Random.Range(0, spawnPoints.Count)];
        Instantiate(prefab, pt.position, Quaternion.identity);
    }

    void OnEnemyDied(GameObject _, Vector3 __)
    {
        EnemiesAlive = Mathf.Max(0, EnemiesAlive - 1);
        if (EnemiesAlive == 0 && WaveInProgress)
            StartCoroutine(WaveCleared());
    }

    IEnumerator WaveCleared()
    {
        WaveInProgress = false;
        OnWaveCompleted?.Invoke(CurrentWave);
        yield return new WaitForSeconds(2f);
        StartCoroutine(RunNextWave());
    }

    public int TotalWaves => waveTable?.Count ?? 0;
}
