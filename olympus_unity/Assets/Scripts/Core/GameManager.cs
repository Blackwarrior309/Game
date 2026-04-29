// GameManager.cs
// Ablegen in: Assets/Scripts/Core/GameManager.cs

using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void OnEnable()
    {
        GameEvents.OnGameOver += HandleGameOver;
        GameEvents.OnGameWon  += HandleGameWon;
    }

    void OnDisable()
    {
        GameEvents.OnGameOver -= HandleGameOver;
        GameEvents.OnGameWon  -= HandleGameWon;
    }

    public void StartNewRun()
    {
        PlayerState.Instance.Reset();
        FavorManager.Instance.Reset();
        SynergySystem.Instance.Reset();
        WeaponManager.Instance?.Reset();
        ArtifactManager.Instance?.Reset();
        WaveManager.Instance.StartGame();
    }

    void HandleGameOver(string reason)
    {
        Debug.Log("GAME OVER: " + reason);
        // TODO: GameOverScreen laden
        // SceneManager.LoadScene("GameOverScreen");
    }

    void HandleGameWon()
    {
        Debug.Log("VICTORY!");
        // TODO: VictoryScreen laden
    }
}
