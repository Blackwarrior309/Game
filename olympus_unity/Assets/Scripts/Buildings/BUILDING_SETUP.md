# Olympus Survivors — Gebäude Setup Guide

## Alle Gebäude auf einen Blick

| Gebäude              | Typ        | Kosten       | HP  | Funktion                                  |
|----------------------|------------|--------------|-----|-------------------------------------------|
| Palisade             | Verteidigung| 20 Asche    | 80  | NavMesh-Hindernis, billig                 |
| Steinmauer           | Verteidigung| 50 Asche    | 300 | Starke Blockade                           |
| Bogenschützenturm    | Angriff    | 60 Asche     | 120 | Auto-Ziel, 1.5 Schüsse/s                 |
| Steinschleuder       | Angriff    | 90 Asche     | 150 | AoE 4m, 1 Schuss/2.5s                    |
| Feuerturm            | Angriff    | 80 Asche     | 100 | AoE + Feuer-DoT 3s                       |
| Heilungsschrein      | Support    | 70 Asche     | 80  | +2 Pyros-HP/s                            |
| Ressourcen-Altar     | Support    | 40 Asche     | 60  | +5 Asche alle 10s                        |
| Opferaltar           | Support    | 50 Asche     | 100 | E → Asche in Favor umwandeln             |
| Hephaistos-Schmiede  | Sonder     | 120A + 30E  | 160 | Upgrades + Legendäre Waffen              |
| Tempel (Zeus/etc.)   | Götter     | 150 Asche    | 200 | Gott-Effekte, max. 3 gleichzeitig        |

---

## Prefab-Setup pro Gebäude

### Palisade.prefab
```
Palisade (StaticBody3D)
├── BoxCollider (Layer: Building, Tag: Building)
├── NavMeshObstacle  ← PFLICHT, carving=true
├── Mesh (Holz-Platzhalter: flacher Cube, 3×1.5×0.3)
└── Palisade.cs
```
**NavMeshObstacle-Einstellungen:**
- Shape: Box
- Center: (0, 0.75, 0)
- Size: (3, 1.5, 0.3)
- Carve: ✓  |  Carve Only Stationary: ✗

---

### StoneWall.prefab
```
StoneWall (StaticBody3D)
├── BoxCollider (Layer: Building)
├── NavMeshObstacle (größer als Palisade)
├── Mesh (Stein-Cube, 3×2×0.5)
└── StoneWall.cs
```

---

### ArcherTower.prefab
```
ArcherTower (StaticBody3D)
├── BoxCollider (Layer: Building)
├── Transform "TurretHead"  ← dreht sich zum Ziel
│   └── Transform "ShootPoint" (vor Turret, Höhe ~3m)
├── Mesh (Turm-Platzhalter)
└── ArcherTower.cs
    ├── turretHead: [TurretHead Transform]
    ├── shootPoint: [ShootPoint Transform]
    ├── detectionRadius: 14
    ├── fireRate: 1.5
    ├── turretDamage: 12
    └── enemyLayer: [Enemy Layer-Maske]
```

**ArrowProjectile.prefab** (wird von ArcherTower gespawnt):
```
Arrow (Rigidbody isKinematic=true)
├── SphereCollider (isTrigger=true, Radius=0.05)
├── Mesh (Capsule sehr klein, 0.05×0.3×0.05)
└── ProjectileBase.cs
    ├── speed: 18
    ├── lifetime: 4
    └── aoeRadius: 0
```

---

### Catapult.prefab
```
Catapult (StaticBody3D)
├── BoxCollider (Layer: Building)
├── Transform "TurretHead"
├── Transform "ShootPoint"
├── Mesh (Katapult-Arm Platzhalter)
└── Catapult.cs
    ├── detectionRadius: 18
    ├── fireRate: 0.4
    ├── turretDamage: 35
    └── aoeRadius: 4
```

**BoulderProjectile.prefab**:
```
Boulder (kein Rigidbody nötig — Parabel via Code)
├── SphereCollider (isTrigger, Radius=0.4)
├── Mesh (Sphere)
└── [kein Script — wird von Catapult.LaunchBoulder() gesteuert]
```

---

### FireTower.prefab
```
FireTower (StaticBody3D)
├── BoxCollider (Layer: Building)
├── Transform "TurretHead"
├── Transform "ShootPoint"
├── ParticleSystem "FlameFX" (Dauerhaftes Feuer-VFX auf Turm)
└── FireTower.cs
    ├── detectionRadius: 10
    ├── fireRate: 0.8
    ├── turretDamage: 8
    ├── fireAoeRadius: 3
    ├── dotDamage: 4
    └── dotDuration: 3
```

---

### HealingShrine.prefab
```
HealingShrine (StaticBody3D)
├── BoxCollider (Layer: Building)
├── ParticleSystem "HealFX" (grüne Partikel)
├── Mesh (Schrein-Platzhalter)
└── HealingShrine.cs
    ├── healPerSecond: 2
    └── [Pyros wird via FindObjectOfType<Pyros>() gefunden]
```

---

### ResourceAltar.prefab
```
ResourceAltar (StaticBody3D)
├── BoxCollider (Layer: Building)
├── ParticleSystem "AshFX" (grauer Rauch)
└── ResourceAltar.cs
    ├── ashPerInterval: 5
    └── intervalSeconds: 10
```

---

### SacrificeAltar.prefab
```
SacrificeAltar (StaticBody3D)
├── BoxCollider (Layer: Building)
├── SphereCollider (isTrigger, Radius=2.5 — Interaktions-Radius)
│   [BEIDE Collider auf demselben GameObject oder Kinder]
└── SacrificeAltar.cs
    ├── ashCostPerSacrifice: 30
    ├── interactRadius: 2.5
    └── sacrificeCooldown: 3
```
**Interaktions-Hinweis:** SacrificeAltar.OnPlayerEnterRange/ExitRange
aufrufen → Sacrifice-UI anzeigen (welcher Gott, E-Taste)

---

### HephaistosForge.prefab
```
HephaistosForge (StaticBody3D)
├── BoxCollider (Layer: Building)
├── ParticleSystem "ForgeFireFX" (oranges Feuer)
├── ParticleSystem "HammerSparkFX" (Funken beim Upgrade)
├── Mesh (Amboss-Platzhalter)
└── HephaistosForge.cs
    [ashCost=120, oreCost=30 direkt im Code]
```
**Achtung:** Nur 1 Schmiede pro Run! (Singleton + Awake-Check)

---

### Temple_Zeus.prefab (Beispiel, alle 5 gleich aufgebaut)
```
Temple (StaticBody3D)
├── BoxCollider (Layer: Building)
├── Mesh (Säulen-Tempel Platzhalter)
└── Temple.cs
    └── templeGodId: Zeus  ← Im Inspector setzen!
```
5 Prefab-Varianten erstellen, je mit unterschiedlichem `templeGodId`.

---

### OreDeposit.prefab (Spielwelt-Objekt, kein Gebäude)
```
OreDeposit (StaticBody3D)
├── BoxCollider (Layer: Building — für Feinde irrelevant)
├── SphereCollider (isTrigger, Radius=2.5 — Spieler-Detektion)
├── Mesh (leuchtender Fels, Emission-Material nötig!)
├── Canvas (World Space, Interact-Prompt)
│   ├── PromptText (TMP)
│   └── ProgressBar (Image, fillMethod=Horizontal)
├── ParticleSystem "MineSparkFX"
└── OreDeposit.cs
    ├── oreAmount: 10
    ├── mineTime: 2
    └── respawnTime: 60
```
**Platzierung:** 4–6 Deposits in der Gefahrenzone (> 40m vom Pyros)
Tags/Gruppe setzen damit der LevelManager sie spawnen kann.

---

## BuildMenu-Setup

```
Singletons-GameObject
└── BuildMenuController.cs
    ├── ghostPrefab: [BuildingGhost.prefab]  ← transparenter Platzierungs-Cursor
    └── buildingPrefabs: Liste befüllen
        ├── Id: "palisade"   → Palisade.prefab
        ├── Id: "stone_wall" → StoneWall.prefab
        ├── Id: "archer_tower" → ArcherTower.prefab
        ├── Id: "catapult"   → Catapult.prefab
        ├── Id: "fire_tower" → FireTower.prefab
        ├── Id: "healing_shrine" → HealingShrine.prefab
        ├── Id: "resource_altar" → ResourceAltar.prefab
        ├── Id: "sacrifice_altar"→ SacrificeAltar.prefab
        ├── Id: "forge"      → HephaistosForge.prefab
        ├── Id: "temple_zeus"    → Temple_Zeus.prefab
        ├── Id: "temple_athena"  → Temple_Athena.prefab
        ├── Id: "temple_ares"    → Temple_Ares.prefab
        ├── Id: "temple_poseidon"→ Temple_Poseidon.prefab
        └── Id: "temple_hades"   → Temple_Hades.prefab
```

### BuildingGhost.prefab
```
BuildingGhost
└── Mesh (transparenter Cube, Emission-Material)
    Material: Standard/URP Transparent, Alpha ~40%
    Farbe: Grün wenn platzierbar, Rot wenn blockiert
```

---

## Schmiedemenü-Setup

```
SmithyMenu_Canvas (Canvas, Sort Order 15)
├── CanvasGroup-Komponente
└── SmithyMenuController.cs
    ├── upgradeCardPrefab: [WeaponUpgradeCard.prefab]
    └── legendaryCardPrefab: [LegendaryWeaponCard.prefab]
```

Auf Singletons-GameObject:
```
└── SmithyMenuController.cs
```

---

## Implementiert

- [x] Palisade + StoneWall (NavMeshObstacle)
- [x] ArcherTower (Auto-Ziel, Einzelschuss)
- [x] Catapult (Parabel-AoE)
- [x] FireTower (AoE + DoT, Hephaistos-Passiv-Integration)
- [x] TurretBase (gemeinsame Turm-Logik, Athena-Intervention)
- [x] HealingShrine (Pyros-Regen)
- [x] ResourceAltar (Asche-Regen)
- [x] SacrificeAltar (E-Taste, Gott-Auswahl, Cooldown)
- [x] HephaistosForge (Upgrades, Legendäre, Synergien)
- [x] Temple (alle 5 Götter, Zeus/Hades Auto-Effekte)
- [x] BuildMenuController (Platzierungs-Ghost, Kosten-Check)
- [x] BuildingCardUI
- [x] SmithyMenuController (3 Tabs)
- [x] WeaponUpgradeCardUI + LegendaryWeaponCardUI
- [x] OreDeposit (E-Halten, Respawn, Lavameer-Synergie)

## Offen

- [ ] WeaponManager (Waffen ausrüsten / Legendäre anwenden)
- [ ] BuildingGhost Material (transparentes Shader-Material)
- [ ] Tempel-Upgrade-System (Stufe 1–3, Kosten verdoppeln)
- [ ] Prometheus-Feuer-Artefakt (Türme +20% via ArtifactManager)
- [ ] AudioManager-Integration (Hammer, Funken, Opfer-Sound)
- [ ] Kamera-Shake bei Katapult-Einschlag
