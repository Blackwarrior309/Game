# Olympus Survivors — Feinde Setup Guide

## Alle Feinde auf einen Blick

| Feind          | HP   | Speed | Schaden | Besonderheit                              | XP  |
|----------------|------|-------|---------|-------------------------------------------|-----|
| SatyrRunner    | 20   | 6.0   | 8       | Rush auf Pyros, schnellster Feind         | 8   |
| StoneGolem     | 180  | 1.8   | 18      | Greift Gebäude bevorzugt, AoE-Stomp       | 30  |
| ShadowWraith   | 45   | 4.5   | 12      | Teleportiert, ignoriert Mauern            | 25  |
| MedusaArcher   | 55   | 3.2   | 14      | Fernkampf, 35% Chance auf Pyros-Beschuss  | 22  |
| Cyclops        | 400  | 2.8   | 25      | Mini-Boss, Gebäude sofort zerstört, Roar  | 80  |
| TitanServant   | 250  | 3.8   | 22      | Kronos Phase 3, Gott-Eigenschaften        | 50  |
| Kronos         | 2500 | 3.5   | 35      | ENDBOSS, 3 Phasen, Build-Reaktion         | —   |

---

## Prefab-Setup pro Feind

### SatyrRunner.prefab
```
SatyrRunner (CharacterController oder NavMeshAgent)
├── NavMeshAgent (Speed=6, StoppingDist=1.2, Radius=0.4)
├── CapsuleCollider (Layer: Enemy, Tag: Enemy)
├── Mesh (grüner Capsule-Platzhalter)
└── SatyrRunner.cs
```
**Drop-Prefabs im Inspector zuweisen:** AshDrop, XpDrop

---

### StoneGolem.prefab
```
StoneGolem (NavMeshAgent)
├── NavMeshAgent (Speed=1.8, StoppingDist=2.0, Radius=0.7)
├── CapsuleCollider (Height=2.5, Radius=0.7, Layer: Enemy)
├── Mesh (grauer Cube-Platzhalter, Scale 0.8×1.2×0.8)
└── StoneGolem.cs
    ├── buildingDetectRadius: 6
    └── buildingDamageMultiplier: 2.5
```
**Wichtig:** Layer "Building" muss existieren für OverlapSphere

---

### ShadowWraith.prefab
```
ShadowWraith (NavMeshAgent)
├── NavMeshAgent (Speed=4.5, StoppingDist=1.5, Radius=0.4)
├── CapsuleCollider (Layer: Enemy, Tag: Enemy)
├── MeshRenderer (transparentes Material! — Alpha-Fade nötig)
│   → Material: Standard Transparent oder URP Unlit/Transparent
├── ParticleSystem "TeleportFX" (lila Partikel, Child)
└── ShadowWraith.cs
    ├── teleportCooldown: 5
    ├── teleportRange: 12
    └── phaseInDuration: 0.4
```
**Wichtig:** Alle Renderer-Materialien müssen Alpha-Fading unterstützen!
Für URP: Material → Surface Type → Transparent

---

### MedusaArcher.prefab
```
MedusaArcher (NavMeshAgent)
├── NavMeshAgent (Speed=3.2, StoppingDist=14, Radius=0.45)
├── CapsuleCollider (Layer: Enemy, Tag: Enemy)
├── Transform "ShootPoint" (vor dem Modell, Höhe ~1m)
├── Mesh (grüner Capsule-Platzhalter)
└── MedusaArcher.cs
    ├── shootPoint: [ShootPoint Transform]
    ├── arrowPrefab: [ArrowProjectile.prefab]
    ├── preferredRange: 14
    └── pyrosTargetChance: 0.35
```

#### ArrowProjectile.prefab
```
Arrow (Rigidbody isKinematic + SphereCollider isTrigger)
├── Mesh (Capsule, sehr klein, Scale 0.05×0.3×0.05)
└── ProjectileBase.cs
    ├── speed: 18
    ├── lifetime: 4
    └── aoeRadius: 0 (kein AoE für Pfeile)
```

---

### Cyclops.prefab
```
Cyclops (NavMeshAgent)
├── NavMeshAgent (Speed=2.8, StoppingDist=3.0, Radius=0.9)
├── CapsuleCollider (Height=3.5, Radius=0.9, Layer: Enemy)
├── Transform "StompCenter" (auf Bodenhöhe)
├── ParticleSystem "StompFX" (Staub-Partikel)
├── ParticleSystem "RoarFX" (Schockwellen-Partikel)
└── Cyclops.cs
    ├── stompRadius: 4.5
    ├── stompDamage: 60
    ├── stompCooldown: 6
    ├── buildingKillRadius: 3
    └── roarRadius: 12
```
**Cyclops ist ein Mini-Boss → HUDManager.ShowBossPanel(true) wird aufgerufen**

---

### Kronos.prefab
```
Kronos (NavMeshAgent)
├── NavMeshAgent (Speed=3.5, StoppingDist=4.0, Radius=1.2)
├── CapsuleCollider (Height=5.0, Radius=1.2, Layer: Enemy)
├── Transform "ScytheAttackPoint"
├── Transform "ProjectileSpawnPoint"
├── ParticleSystem "PhaseTransitionFX"
├── ParticleSystem "TimeBubbleFX"
└── Kronos.cs
    ├── timeBubblePrefab: [TimeBubble.prefab]
    ├── timeWavePrefab:   [TimeWave.prefab]
    ├── titanServantPrefab_Zeus:     [TitanServant_Zeus.prefab]
    ├── titanServantPrefab_Athena:   [TitanServant_Athena.prefab]
    ├── titanServantPrefab_Ares:     [TitanServant_Ares.prefab]
    ├── titanServantPrefab_Poseidon: [TitanServant_Poseidon.prefab]
    ├── titanServantPrefab_Hades:    [TitanServant_Hades.prefab]
    └── arenaShrinkWall: [TimeWallRing GameObject]
```

#### TimeBubble.prefab
```
TimeBubble (SphereCollider isTrigger, Radius=0.8)
├── Mesh (Sphere, lila/transparent)
└── TimeBubble.cs
    → Initialize(freezeDuration) wird von Kronos aufgerufen
```

#### TitanServant_Zeus.prefab (Beispiel)
```
TitanServant_Zeus (NavMeshAgent)
├── CapsuleCollider (Layer: Enemy)
└── TitanServant.cs
    └── servantGod: Zeus
```
→ 5 Varianten erstellen (Zeus, Athena, Ares, Poseidon, Hades)
→ Jede hat andere servantGod-Einstellung

---

### ShadowAlly.prefab
```
ShadowAlly (NavMeshAgent)
├── NavMeshAgent (Speed=5, StoppingDist=1.8, Radius=0.4)
├── CapsuleCollider (Layer: Ally, Tag: Ally)
├── Mesh (dunkler, transparenter Capsule)
└── ShadowAlly.cs
```

#### ShadowAllySpawner (auf Singletons-GameObject)
```
Singletons
└── ShadowAllySpawner.cs
    ├── shadowAllyPrefab: [ShadowAlly.prefab]
    └── maxShadows: 10
```

---

## Layer-Übersicht (alle Feinde/Allies)

| Layer | Name     | Wofür                              |
|-------|----------|------------------------------------|
| 6     | Player   | Spieler-Collider                   |
| 7     | Enemy    | Alle Feinde                        |
| 8     | Pyros    | Das Kern-Objekt                    |
| 9     | Building | Alle Gebäude                       |
| 10    | Pickup   | Asche, Erz, XP                     |
| 11    | Ally     | Schattenkrieger, Verbündete        |

---

## Implementiert

- [x] SatyrRunner — Rush-Feind
- [x] StoneGolem — Gebäude-Zerstörer
- [x] ShadowWraith — Teleport, Mauern ignorieren
- [x] MedusaArcher — Fernkampf, Pyros-Targeting
- [x] Cyclops — Mini-Boss, Stomp + Roar
- [x] Kronos — 3-Phasen-Endboss + Zeit-Mechaniken
- [x] TitanServant — Kronos Phase 3, 5 Gott-Varianten
- [x] ShadowAlly — Hades-Verbündeter
- [x] ProjectileBase — Wiederverwendbares Projektil
- [x] TimeBubble — Kronos Phase 1 Zeitblase

## Offen (nice-to-have)

- [ ] CameraShake-Komponente (für Stomp/Kronos-Attacken)
- [ ] GlobalSlowManager (für Kronos Zeit-Slow-Aura auf Türme)
- [ ] FirePuddle-Prefab (Himmelsfeuer-Synergie)
- [ ] Animatoren für alle Feinde
- [ ] Boss-Musik-Trigger (AudioManager)
