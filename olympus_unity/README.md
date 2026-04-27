# OLYMPUS SURVIVORS — Unity Setup Guide

## Projekt erstellen
- Unity 2022.3 LTS oder 2023.x
- Template: **3D Core** (nicht URP/HDRP für den Prototype, später wechselbar)
- Package Manager: **AI Navigation** (NavMesh) installieren

---

## Ordnerstruktur

```
Assets/
├── Scripts/
│   ├── Core/
│   │   ├── GameEvents.cs        ← Statischer Event-Bus
│   │   ├── PlayerState.cs       ← Singleton
│   │   ├── FavorManager.cs      ← Singleton
│   │   ├── SynergySystem.cs     ← Singleton
│   │   ├── WaveManager.cs       ← Singleton
│   │   ├── LevelUpSystem.cs     ← Singleton
│   │   ├── GameManager.cs       ← Singleton
│   │   ├── Pyros.cs
│   │   └── UpgradeData.cs       ← ScriptableObject
│   ├── Player/
│   │   ├── PlayerController.cs
│   │   └── AttackRangeDetector.cs
│   ├── Enemies/
│   │   ├── EnemyBase.cs
│   │   └── SatyrRunner.cs
│   ├── Buildings/
│   │   └── BuildingBase.cs      ← enthält auch Temple
│   └── Pickups/
│       └── PickupBase.cs
├── Prefabs/
│   ├── Player.prefab
│   ├── Pyros.prefab
│   ├── Enemies/
│   │   └── SatyrRunner.prefab
│   ├── Pickups/
│   │   ├── AshDrop.prefab
│   │   ├── OreDrop.prefab
│   │   └── XpDrop.prefab
│   └── Singletons.prefab        ← alle Manager zusammen
└── Scenes/
    └── GameScene.unity
```

---

## Singletons-Setup (WICHTIG)

Alle Manager-Scripts auf ein einziges **"Singletons" GameObject**:
- `GameManager`
- `PlayerState`
- `FavorManager`
- `SynergySystem`
- `WaveManager`
- `LevelUpSystem`

Dieses GameObject mit **DontDestroyOnLoad** bleibt über Szenen erhalten.

---

## Layers einrichten

Window → Project Settings → Tags & Layers:

| Layer | Name     |
|-------|----------|
| 6     | Player   |
| 7     | Enemy    |
| 8     | Pyros    |
| 9     | Building |
| 10    | Pickup   |

---

## Minimal-Szene aufbauen

### 1. Boden (Arena)
- Plane (200×200) → StaticBody
- NavMeshSurface-Komponente → Bake drücken
- Material: Stein-Textur (Platzhalter: grau)

### 2. Player GameObject
```
Player (CharacterController + PlayerController.cs)
├── Mesh (Capsule als Platzhalter)
├── AttackRange (SphereCollider isTrigger, Radius=3, Layer=Player)
│   └── AttackRangeDetector.cs
└── PickupRange (SphereCollider isTrigger, Radius=3)
    └── [PickupBase reagiert selbst via OnTriggerEnter]
```
- Tag: **Player**
- Layer: **Player**
- CharacterController: Height=2, Radius=0.5

### 3. Pyros GameObject
```
Pyros (Rigidbody isKinematic + Pyros.cs)
├── Mesh (Zylinder, Scale 1×2×1)
└── CapsuleCollider
```
- Tag: **Pyros**

### 4. Spawn-Punkte (8 Stück)
- 8× leere GameObjects, gleichmäßig am Rand verteilt (Radius ~80m)
- Tag: **SpawnPoint**
- Keine Collider nötig

### 5. Singletons GameObject
- Alle Manager-Scripts drauf (siehe oben)
- WaveManager: Prefabs in die Felder ziehen

---

## Prefab: SatyrRunner

```
SatyrRunner (NavMeshAgent + SatyrRunner.cs)
├── Mesh (Capsule, grün einfärben)
└── CapsuleCollider
```
- NavMeshAgent: Speed=6, Stopping Distance=1.2, Radius=0.4
- Layer: **Enemy**
- Tag: **Enemy**
- Drop-Prefabs im Inspector zuweisen

---

## Prefab: AshDrop / OreDrop / XpDrop

```
AshDrop (SphereCollider isTrigger + PickupBase.cs)
└── Mesh (kleine Sphere)
```
- PickupBase: Type=Ash, Amount=2
- Layer: **Pickup**

---

## Input (Edit → Project Settings → Input Manager)

| Name       | Taste        |
|------------|--------------|
| Horizontal | A/D          |
| Vertical   | W/S          |
| (Space)    | KeyCode.Space — direkt in PlayerController.cs |
| (B/F/G)    | KeyCode — direkt in PlayerController.cs |

---

## NavMesh einrichten

1. Window → AI → Navigation
2. Agents: Name="Humanoid", Radius=0.4, Height=1.8, MaxSlope=45
3. Bake auf dem Boden-Plane
4. SatyrRunner bekommt NavMeshAgent (AgentType=Humanoid)

---

## Spielstart-Code

In der GameScene: ein leeres GameObject "GameStarter":

```csharp
// GameStarter.cs
using UnityEngine;
public class GameStarter : MonoBehaviour
{
    void Start() => GameManager.Instance.StartNewRun();
}
```

---

## Implementiert (Phase 1)

- [x] PlayerState (HP, Ressourcen, XP, Events)
- [x] FavorManager (6 Götter, alle Schwellen, Avatar-Timer)
- [x] SynergySystem (10 Synergien, event-getrieben)
- [x] GameEvents (statischer Event-Bus)
- [x] WaveManager (alle 10 Wellen konfiguriert)
- [x] GameManager
- [x] LevelUpSystem + UpgradeData ScriptableObject
- [x] PlayerController (WASD, Dash, Auto-Angriff, Zeus-Passiv)
- [x] EnemyBase (NavMesh, HP, Drops, Slow/Stun)
- [x] SatyrRunner
- [x] Pyros
- [x] BuildingBase + Temple
- [x] PickupBase (Ash/Ore/XP)
- [x] AttackRangeDetector

## Nächste Phase

- [ ] HUD (Canvas: Favor-Leisten, HP-Bars, Ressourcen)
- [ ] Baumenü UI
- [ ] Restliche Feinde (StoneGolem, ShadowWraith, MedusaArcher, Cyclops)
- [ ] Kronos Boss (3 Phasen)
- [ ] Hephaistos Schmiede + Schmiedemenü
- [ ] Legendäre Waffen
- [ ] Alle Götter-Interventionen und Avatare
