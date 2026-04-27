// LevelUpSystem.cs
// Ablegen in: Assets/Scripts/Core/LevelUpSystem.cs
// UpgradeData-Assets im Inspector in "upgradePool" ziehen

using UnityEngine;
using System.Collections.Generic;

public class LevelUpSystem : MonoBehaviour
{
    public static LevelUpSystem Instance { get; private set; }

    [Header("Upgrade Pool")]
    [SerializeField] List<UpgradeData> upgradePool = new();

    int offersCount = 3;  // Delphi-Orakel setzt auf 4

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void OnEnable()  => PlayerState.OnLevelUp += OnLevelUp;
    void OnDisable() => PlayerState.OnLevelUp -= OnLevelUp;

    void OnLevelUp(int _)
    {
        var choices = GetRandomChoices();
        GameEvents.RaiseShowLevelUpChoices(choices);
    }

    List<UpgradeData> GetRandomChoices()
    {
        var pool = new List<UpgradeData>(upgradePool);
        // Fisher-Yates Shuffle
        for (int i = pool.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (pool[i], pool[j]) = (pool[j], pool[i]);
        }
        int count = Mathf.Min(offersCount, pool.Count);
        return pool.GetRange(0, count);
    }

    public void ApplyUpgrade(string upgradeId)
    {
        var ps = PlayerState.Instance;
        switch (upgradeId)
        {
            case "artifact_hp":
                ps.maxHp += 30f; ps.Heal(30f); break;
            case "artifact_speed":
                ps.moveSpeed *= 1.2f; break;
            case "artifact_armor":
                ps.armor += 2f; break;     // Vereinfacht
            case "artifact_xp":
                ps.xpMultiplier *= 1.25f; break;
            case "artifact_damage":
                ps.damage *= 1.1f; break;
            case "artifact_oracle":
                offersCount = 4; break;    // Delphi-Orakel
            case "artifact_prometheus":
                // Türme +20% Schaden — via TurretDamageMultiplier-Komponente
                break;
            default:
                Debug.Log("LevelUpSystem: Unbekanntes Upgrade: " + upgradeId);
                break;
        }
    }

    public void SetOffersCount(int count) => offersCount = count;
}
