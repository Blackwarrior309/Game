// GameEvents.cs
// Statischer Event-Bus — kein Singleton nötig, überall verfügbar
// Ablegen in: Assets/Scripts/Core/GameEvents.cs

using UnityEngine;
using System;

public static class GameEvents
{
    // ── Spieler ────────────────────────────────────────────────────────────
    public static event Action<GameObject> OnPlayerAttacked;
    public static void RaisePlayerAttacked(GameObject target) => OnPlayerAttacked?.Invoke(target);

    // ── Feinde ─────────────────────────────────────────────────────────────
    public static event Action<GameObject, Vector3> OnEnemyKilled;
    public static void RaiseEnemyKilled(GameObject enemy, Vector3 pos) => OnEnemyKilled?.Invoke(enemy, pos);

    public static event Action<Vector3> OnSpawnShadowAlly;
    public static void RaiseSpawnShadowAlly(Vector3 pos) => OnSpawnShadowAlly?.Invoke(pos);

    // ── UI ─────────────────────────────────────────────────────────────────
    public static event Action OnBuildMenuToggle;
    public static void RaiseBuildMenuToggle() => OnBuildMenuToggle?.Invoke();

    public static event Action OnSmithyMenuToggle;
    public static void RaiseSmithyMenuToggle() => OnSmithyMenuToggle?.Invoke();

    public static event Action<System.Collections.Generic.List<UpgradeData>> OnShowLevelUpChoices;
    public static void RaiseShowLevelUpChoices(System.Collections.Generic.List<UpgradeData> choices)
        => OnShowLevelUpChoices?.Invoke(choices);

    // ── Gebäude ────────────────────────────────────────────────────────────
    public static event Action<string, Vector3> OnBuildingPlaced;
    public static void RaiseBuildingPlaced(string type, Vector3 pos) => OnBuildingPlaced?.Invoke(type, pos);

    // ── Spiel-State ────────────────────────────────────────────────────────
    public static event Action<string> OnGameOver;
    public static void RaiseGameOver(string reason) => OnGameOver?.Invoke(reason);

    public static event Action OnGameWon;
    public static void RaiseGameWon() => OnGameWon?.Invoke();
}
