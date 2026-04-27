// AvatarSpawnSystem.cs
// Ablegen in: Assets/Scripts/Gods/AvatarSpawnSystem.cs
// Anhängen an: Singletons-GameObject (DontDestroyOnLoad)
//
// Zentrale Vermittlung zwischen FavorManager-Avatar-Events und den
// AvatarBase-Instanzen in der Szene. Hört auf:
//   - FavorManager.OnAvatarStarted(god)  → Prefab instanziieren
//   - FavorManager.OnAvatarEnded(god)    → AvatarBase.StartDespawn() rufen
//
// Hephaistos hat keinen Avatar (FavorManager.TryActivateAvatar lehnt ab) —
// daher gibt es kein Hephaistos-Prefab im Inspector.

using UnityEngine;
using System.Collections.Generic;

public class AvatarSpawnSystem : MonoBehaviour
{
    [System.Serializable]
    public class AvatarPrefabEntry
    {
        public FavorManager.God God;
        public GameObject       Prefab;
    }

    [Header("Avatar-Prefabs (5 Götter, ohne Hephaistos)")]
    [SerializeField] List<AvatarPrefabEntry> prefabEntries = new();

    [Header("Spawn")]
    [SerializeField] float spawnOffsetForward = 2f;     // m vor dem Spieler

    Dictionary<FavorManager.God, GameObject> prefabMap   = new();
    Dictionary<FavorManager.God, AvatarBase> activeAvatars = new();

    // ── Unity Lifecycle ────────────────────────────────────────────────────
    void Awake()
    {
        foreach (var entry in prefabEntries)
            if (entry.Prefab != null) prefabMap[entry.God] = entry.Prefab;
    }

    void OnEnable()
    {
        FavorManager.OnAvatarStarted += SpawnAvatar;
        FavorManager.OnAvatarEnded   += DespawnAvatar;
    }

    void OnDisable()
    {
        FavorManager.OnAvatarStarted -= SpawnAvatar;
        FavorManager.OnAvatarEnded   -= DespawnAvatar;
    }

    // ── Spawn ──────────────────────────────────────────────────────────────
    void SpawnAvatar(FavorManager.God god)
    {
        if (!prefabMap.TryGetValue(god, out var prefab))
        {
            Debug.LogWarning($"AvatarSpawnSystem: kein Prefab für {god}");
            return;
        }

        // Doppelte Spawns verhindern (wenn z.B. Avatar noch despawnt)
        if (activeAvatars.TryGetValue(god, out var existing) && existing != null)
        {
            existing.StartDespawn();
            activeAvatars.Remove(god);
        }

        var player = GameObject.FindGameObjectWithTag("Player");
        Vector3 spawnPos = player != null
            ? player.transform.position + player.transform.forward * spawnOffsetForward
            : Vector3.zero;

        var go     = Instantiate(prefab, spawnPos, Quaternion.identity);
        var avatar = go.GetComponent<AvatarBase>();
        if (avatar == null)
        {
            Debug.LogWarning($"AvatarSpawnSystem: Prefab für {god} hat keine AvatarBase-Komponente");
            Destroy(go);
            return;
        }

        activeAvatars[god] = avatar;
    }

    // ── Despawn ────────────────────────────────────────────────────────────
    void DespawnAvatar(FavorManager.God god)
    {
        if (activeAvatars.TryGetValue(god, out var avatar) && avatar != null)
            avatar.StartDespawn();
        activeAvatars.Remove(god);
    }

    // ── Getter (für UI / Debugging) ────────────────────────────────────────
    public bool IsAvatarActive(FavorManager.God god)
        => activeAvatars.TryGetValue(god, out var a) && a != null;
}
