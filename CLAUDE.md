# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Repository Layout

The Unity project lives at the repo root under **`olympus_unity/`**:

```
olympus_unity/
├── README.md                      # Setup guide (Unity version, layers, prefab wiring)
└── Assets/
    ├── UI_Mockup.html             # Static HTML mockup of the in-game HUD
    └── Scripts/
        ├── Core/                  # Singletons + event bus (PlayerState, FavorManager, …)
        ├── Player/                # PlayerController + AttackRangeDetector
        ├── Enemies/               # EnemyBase + every enemy/boss + ENEMY_SETUP.md
        ├── Allies/                # ShadowAlly (Hades summon)
        ├── Gods/                  # Per-god behaviour controllers (HephaistosInterventions, AvatarSpawnSystem) + Avatars/ subtree
        ├── Combat/                # ProjectileBase, LavaPuddle (shared effect prefabs)
        ├── Buildings/             # BuildingBase, towers, walls, forge + BUILDING_SETUP.md
        ├── Pickups/               # PickupBase (Ash/Ore/XP)
        ├── World/                 # OreDeposit
        └── UI/                    # HUD/, MainMenu/, BuildMenu/, SmithyMenu/, EndScreens
```

The repo-root `README.md` is the project front door — it carries the phase status table and a "wie mitentwickeln"-Kurzanleitung. The Unity-side setup details (layers, tags, prefab assembly) live in `olympus_unity/README.md`.

## No Build / Test / Lint Tooling in This Repo

Only the C# source tree is checked in — there is no Unity project file (`*.csproj`, `Packages/manifest.json`, `ProjectSettings/`), no test suite, no CI config, and no linter. `olympus_unity/README.md` says the project is meant to be opened in **Unity 2022.3 LTS or 2023.x with the 3D Core template + the AI Navigation (NavMesh) package**, but the project metadata is not in the repo. Don't fabricate build/test commands; if a task implies running the game, point out that Unity isn't available here and stop at static C# edits.

## High-Level Game Architecture

Olympus Survivors is a top-down survivors-style action game: defend the central **Pyros** (sacred flame) through 10 enemy waves culminating in the Kronos boss, while building turrets/temples and earning favor with six gods.

### The Singleton + Static-Event-Bus Pattern

Almost all global state lives on a single `Singletons` GameObject (DontDestroyOnLoad) that hosts every `*Manager` MonoBehaviour. They expose `Instance` properties and communicate through a **static event bus** in `Core/GameEvents.cs` (no Singleton — events are `static event Action<...>`). New cross-system communication should go through `GameEvents` events, not direct manager references, to keep the dependency graph one-way.

Core managers (all in `Assets/Scripts/Core/`):
- **`GameManager`** — `StartNewRun()` resets `PlayerState`, `FavorManager`, `SynergySystem` and kicks off `WaveManager.StartGame()`. Subscribes to `OnGameOver`/`OnGameWon`.
- **`PlayerState`** — single source of truth for HP/resources/XP/stats. Owns its own typed events (`OnHpChanged`, `OnAshChanged`, `OnXpChanged`, `OnLevelUp`, `OnPlayerDied`). HUD panels subscribe directly. `Reset()` is called by `GameManager` at run start.
- **`FavorManager`** — six gods (`Zeus, Athena, Ares, Poseidon, Hades, Hephaistos`) each with 0–100 favor, passive (≥50), interventions (25/75), and an **avatar** (favor=100, 30s, drains favor to 0; Hephaistos has no avatar). Threshold crossings are emitted as `OnThresholdReached(god, "passive"|"intervention_1"|"intervention_2"|"avatar")`. Temple-built/destroyed and forge-built/destroyed methods adjust favor regen and re-trigger `SynergySystem.CheckSynergies()`.
- **`SynergySystem`** — 10 hardcoded two-god combos (e.g. `storm_flood = Zeus+Poseidon`). `CheckSynergies()` is called whenever favor crosses 50, a temple is built/destroyed, or a forge is built/destroyed; it diffs the active set and fires `OnSynergyActivated`/`OnSynergyDeactivated`. Other systems gate behavior with `SynergySystem.Instance.IsActive("id")`.
- **`WaveManager`** — hardcoded `waveTable` of 10 waves (final wave = Kronos). Spawns at `GameObject.FindGameObjectsWithTag("SpawnPoint")`. Tracks `EnemiesAlive` via the `GameEvents.OnEnemyKilled` event; when it hits 0 the wave is "cleared" and the next wave starts after a 2 s delay.
- **`LevelUpSystem`** — listens for `PlayerState.OnLevelUp`, picks N random `UpgradeData` ScriptableObjects from `upgradePool`, raises `GameEvents.OnShowLevelUpChoices`. UI calls `ApplyUpgrade(upgradeId)` which mutates `PlayerState`. Adding a new upgrade = add a `case` in `ApplyUpgrade` AND create the matching `UpgradeData` asset (`Assets > Create > OlympusSurvivors/UpgradeData`).
- **`Pyros`** — the defended objective. `TakeDamage` notifies `FavorManager.OnPyrosDamaged()` (−5 favor to all gods) and raises `GameEvents.OnGameOver("pyros_destroyed")` at 0 HP.

### Player, Enemies, Buildings

- **`PlayerController`** (`Player/`) — `CharacterController` + WASD + space-dash + `KeyCode.B` (build menu), `KeyCode.F` (smithy, requires `hasForge`), `KeyCode.G` (avatar). Auto-attack uses `NearbyEnemies` populated by `AttackRangeDetector` on a child trigger. Zeus passive logic (every 10th hit → AoE lightning) lives here and queries `SynergySystem` for synergy multipliers.
- **`EnemyBase`** (`Enemies/`) — abstract-ish base: `NavMeshAgent`, target is Pyros (default) or Player (`prioritizePyros=false`), `ForcedTarget` overrides for Ares intervention. On `Die()` it grants XP, drops ash (always) + ore (only if killed >40 m from Pyros, with `lava_sea` synergy doubling the ore), grants favor via `FavorManager.OnEnemyKill()`, and may spawn a Hades shadow ally. Subclasses override behavior — `StoneGolem` prefers buildings, `ShadowWraith` teleports, `MedusaArcher` shoots `ProjectileBase`, `Cyclops` is a mini-boss with stomp + roar, `GiantPrecursor` is the Welle-9 mini-boss with a continuous slow-aura (Vorschau auf Kronos Phase 1), `Kronos` is a 3-phase endboss spawning `TitanServant`s.
- **`BuildingBase`** (`Buildings/`) — common HP, build coroutine (`buildTime`), `OnBuildingCompleted`/`OnBuildingDestroyed` events. `Temple` subclass sets `isTemple=true`, calls `FavorManager.OnTempleBuilt(godId)`, and starts god-specific auto-effect coroutines (Zeus auto-lightning every 20 s, Hades shadow spawn every 45 s). `HephaistosForge` is a singleton-checked one-per-run building; placing it sets `PlayerState.hasForge` and unlocks the smithy menu (`KeyCode.F`).
- **`BuildMenuController`** (`UI/BuildMenu/`) — central `AllBuildings` table holds id, costs, category, and a `CanBuild` validator (e.g. temples check `activeTemples < MaxTemples` (3) and "not already built"). Placement uses a ghost prefab and raycasts against the `Ground` layer. Costs are spent only on confirmed placement. To add a building: add an entry to `AllBuildings`, add the prefab id mapping in the inspector list `buildingPrefabs`, and create a `BuildingBase` subclass if behavior is custom.

### Conventions Worth Preserving

- **Layer/Tag setup is part of the contract.** Code uses `LayerMask.GetMask("Enemy"|"Building"|"Ground"|...)` and `GameObject.FindGameObjectWithTag("Player"|"Pyros"|"SpawnPoint"|"Building")`. Layers 6–11 are reserved (Player, Enemy, Pyros, Building, Pickup, Ally) — see `ENEMY_SETUP.md` and the phase1 README.
- **Comments and identifiers are German** (e.g. `// ── Spieler ──`, `Gewitterflut`, `Schmiede`). New code should match.
- **File header comment** — every script starts with `// Filename.cs` and `// Ablegen in: Assets/Scripts/...` indicating its target Unity path. Preserve this format when adding scripts.
- **Event subscribe/unsubscribe must be balanced.** All managers pair `OnEnable`/`OnDisable` (or `Start`/`OnDestroy`) `+= / -=` for `GameEvents` and `*State` events. Forgetting the unsubscribe leaks across scene reloads because most managers are `DontDestroyOnLoad`.
- **Reset on new run.** Anything that holds run state must implement `Reset()` and be invoked from `GameManager.StartNewRun()`.
- **Setup docs alongside code.** `Buildings/BUILDING_SETUP.md` and `Enemies/ENEMY_SETUP.md` document required prefab structure, NavMeshAgent params, child transforms (`TurretHead`, `ShootPoint`, `StompCenter`, etc.), and inspector wiring. When adding/changing a prefab-driven script, update the matching SETUP.md so the Unity-side wiring stays documented.

### Project Status (per phase, ~83 % overall — 91 / 109 tasks)

The README's status table is the canonical reference; this is what each phase means architecturally so you know where to plug new code in.

| # | Phase                          | Status     | Where to plug in                                                                                                  |
|---|--------------------------------|------------|--------------------------------------------------------------------------------------------------------------------|
| 1 | Core Prototype                 | 11 / 12    | Player loop, XP, waves 1–3, Pyros, win/lose. **Open:** Arena terrain layout (no script work — Unity scene).        |
| 2 | Building System                | 13 / 13 ✓  | All building types + build menu + placement.                                                                       |
| 3 | Favor & Götter (Basis)         | 13 / 13 ✓  | All 5 gods full + temple-tier upgrades 1–3. Per-god `Gods/<God>Interventions.cs` + `Gods/Avatars/<God>Avatar`. Tempel-Upgrade via `Temple.TryUpgrade` (E-Taste im 4-m-Radius); `Temple.Level` liest live aus `FavorManager.GetTempleLevel`, sodass die Auto-Coroutinen (Zeus-Blitz, Hades-Schatten) ohne Neustart skalieren. Earlier passives (Zeus 10th-hit lightning, Hades 15 % shadow on kill, Poseidon slow on hit) remain inlined in `PlayerController`/`EnemyBase`. |
| 4 | Hephaistos & Schmiede          | 20 / 20 ✓  | Forge, smithy menu, all 7 legendaries, ore + ore deposits, both Hephaistos interventions (Schmiede-Burst via `PlayerState.damageMultiplier`, Vulkan-Zorn via `Gods/HephaistosInterventions.cs` + `Combat/LavaPuddle.cs`). |
| 5 | Synergien                      | 14 / 14 ✓  | All 10 synergies + activation/deactivation flow done.                                                              |
| 6 | Vollständige Feinde & Wellen   | 6 / 7      | Stone Golem, Shadow Wraith, Medusa, Cyclops, GiantPrecursor (Welle 9 mini-boss with slow-aura), waves 1–9 done. **Open:** enemy climbing on buildings (mostly Unity NavMesh-Off-Mesh-Link config). |
| 7 | Kronos Endboss                 | 11 / 14    | All 3 phases + time mechanics + boss UI implemented. **Open:** Kronos model/animations, Oboloi reward calc, voice-lines. |
| 8 | Meta-Progression & Polish      | 3 / 16     | HUD + legendary visual frame + ore deposit visuals done. **Open:** WeaponManager + 7 base weapons, full ArtifactManager (9 artefakte incl. Prometheus), evolution-upgrade system, Oboloi currency + meta-upgrade menu, Audio/Music systems, wave balance pass, Schmiede-Modell/Tempel-Slot-Markierungen, arena-shrink VFX. |

### Adding to extensible tables

- **New god** → add to `FavorManager.God` enum + `GodNames` array + handle in HUD favor panel + main-menu `GodData`.
- **New synergy** → add to `SynergySystem.BuildSynergyTable()` (id, German display name, two gods); gate behaviour elsewhere with `SynergySystem.Instance.IsActive("id")`.
- **New wave** → add a `WaveData` row to `WaveManager.BuildWaveTable()` and (if a new enemy type) a prefab field + `prefabMap` entry in `Start()`.
- **New levelup upgrade** → both a `case "id":` in `LevelUpSystem.ApplyUpgrade(...)` AND a matching `UpgradeData` ScriptableObject (`Assets > Create > OlympusSurvivors/UpgradeData`) dropped into the inspector's `upgradePool`.
- **New building** → add to `BuildMenuController.AllBuildings` (id, costs, category, `CanBuild` validator), a prefab id mapping in the inspector list `buildingPrefabs`, and a `BuildingBase` subclass if behaviour is custom. Update `Buildings/BUILDING_SETUP.md`.
- **New enemy** → subclass `EnemyBase`, add a serialised prefab field on `WaveManager` + `prefabMap[id] = prefab`, document required transforms (`ShootPoint` etc.) in `Enemies/ENEMY_SETUP.md`.

### Architectural extension points still missing

These don't exist as classes yet — when phase 8 work begins, they need to be created on the `Singletons` GameObject and wired through `GameEvents`:

- **`WeaponManager`** — foundation in `Core/WeaponManager.cs` (7 base-weapon pool, equipped state, smithy-upgrade + legendary hooks). `PlayerController.HandleAutoAttack`/`DoAttack`/`TriggerZeusLightning` now read damage and fire-rate via `WeaponManager.Instance.GetCurrentDamage()` / `GetCurrentFireRate()` (with `PlayerState`-fallback for safety); `GameManager.StartNewRun` resets it. Still pending: P8-06 evolution-trigger logic when a weapon is picked 3× in level-up choices.
- **`ArtifactManager`** — Prometheus artifact (`artifact_prometheus` in `LevelUpSystem`) currently has an empty case; a manager is needed to broadcast multipliers (e.g. tower damage +20%) that turrets/towers query at fire-time.
- **`AudioManager`** — no audio code anywhere yet. Should subscribe to `GameEvents` (and `*State` events) for SFX cues; sound files would live in `Assets/Audio/`.
- **`CameraShake`** — referenced by `BUILDING_SETUP.md` (catapult impact) and `ENEMY_SETUP.md` (Cyclops stomp / Kronos) but not implemented.
- **`GlobalSlowManager`** — referenced by `ENEMY_SETUP.md` for Kronos's slow-aura affecting towers; currently the slow only affects movement via per-enemy `slowFactor`.

## Git Workflow for This Task Stream

Active development branch is `claude/add-claude-documentation-Rbr3T`. Push with `git push -u origin <branch>`, retry network failures up to 4× with exponential backoff (2s/4s/8s/16s). Don't push to other branches without explicit permission, and don't open a PR unless the user asks.
