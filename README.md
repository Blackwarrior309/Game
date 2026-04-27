# Olympus Survivors

Top-down Survivors-Action im antiken Griechenland: Verteidige die heilige Flamme **Pyros** durch 10 Wellen bis zum **Kronos**-Endboss, baue Türme/Mauern/Tempel und sammle die Gunst von sechs Göttern (Zeus, Athena, Ares, Poseidon, Hades, Hephaistos).

> Unity 2022.3 LTS / 2023.x · 3D Core + AI Navigation · v0.2-Prototyp

## Status

| Phase | Name                              | Fortschritt    |
|-------|-----------------------------------|----------------|
| 1     | Core Prototype                    | 11 / 12        |
| 2     | Building System                   | 13 / 13 ✓      |
| 3     | Favor & Götter (Basis)            | 9 / 13         |
| 4     | Hephaistos & Schmiede             | 20 / 20 ✓      |
| 5     | Synergien                         | 14 / 14 ✓      |
| 6     | Vollständige Feinde & Wellen      | 6 / 7          |
| 7     | Kronos Endboss                    | 11 / 14        |
| 8     | Meta-Progression & Polish         | 3 / 16         |
| **Gesamt** |                              | **87 / 109 (~80 %)** |

### Was steht noch offen?

- **Phase 1** — Arena-Layout (Terrain, runde 80-m-Arena)
- **Phase 3** — **Zeus + Athena voll** (Interventionen + Avatare). Ares/Poseidon/Hades brauchen noch Interventionen + Avatar-KI. Tempel-Upgrade-Stufen 1–3 fehlen.
- **Phase 6** — Kletterverhalten auf Gebäuden (Off-Mesh-Links, primär Unity-NavMesh-Setup).
- **Phase 7** — Kronos-Modell + Animationen, Boss-Rewards (Oboloi-Berechnung), Voice-Lines.
- **Phase 8** — Oboloi-Meta-Currency, Meta-Upgrade-Menü, vollständige Artefakte (9) und Basis-Waffen (7), Evolutions-Upgrades, Audio/Musik, Wellen-Balance, diverse VFX.

## Repo-Struktur

```
Game/
├── README.md                       (dieses Dokument)
├── CLAUDE.md                       Architektur-Guide für AI-Assistenten
└── olympus_unity/                  Unity-Projekt-Wurzel
    ├── README.md                   Setup-Guide (Unity-Version, Layer, Prefabs)
    └── Assets/
        ├── UI_Mockup.html          HUD-Mockup (Browser öffnen)
        └── Scripts/
            ├── Core/               Singletons + Event-Bus (Player/Favor/Synergy/Wave/...)
            ├── Player/             PlayerController + AttackRangeDetector
            ├── Enemies/            EnemyBase + alle Feinde + ENEMY_SETUP.md
            ├── Allies/             ShadowAlly (Hades-Verbündeter)
            ├── Combat/             ProjectileBase
            ├── Buildings/          Türme, Mauern, Schmiede + BUILDING_SETUP.md
            ├── Pickups/            PickupBase (Asche/Erz/XP)
            ├── World/              OreDeposit
            └── UI/                 HUD/, MainMenu/, BuildMenu/, SmithyMenu/, EndScreens
```

## Mitentwickeln

Das Repo enthält **nur den C#-Quellcode** — keine Unity-Projektdateien (`ProjectSettings/`, `Packages/manifest.json`, …), keine Tests, kein Build-Skript.

1. Unity 2022.3 LTS oder 2023.x mit dem **3D Core**-Template ein leeres Projekt anlegen.
2. Package `com.unity.ai.navigation` (NavMesh) installieren.
3. `olympus_unity/Assets/` ins Unity-Projekt kopieren bzw. als Ordner verlinken.
4. Layer 6–11 + Tags wie in `olympus_unity/README.md` beschrieben anlegen (Player, Enemy, Pyros, Building, Pickup, Ally).
5. Prefabs gemäß `Assets/Scripts/Buildings/BUILDING_SETUP.md` und `Assets/Scripts/Enemies/ENEMY_SETUP.md` zusammenbauen.
6. `Singletons`-GameObject mit allen Manager-Scripts anlegen, Prefab-Referenzen am `WaveManager` und `BuildMenuController` setzen.

## Hauptkonzepte

- **Singleton-Manager + statischer Event-Bus** — alle globalen Manager liegen auf einem `Singletons`-GameObject (DontDestroyOnLoad). Cross-System-Kommunikation läuft über `Core/GameEvents.cs` (`static event Action<...>`), nicht über direkte Manager-Referenzen.
- **Sechs Götter mit Gunst-System** — pro Gott 0–100 Gunst mit Schwellen bei 25/50/75/100 (Passiv ab 50, Interventionen bei 25/75, Avatar bei 100 für 30 s, danach auf 0). Tempel geben +2/min Regeneration. Hephaistos hat keinen Avatar — dafür die Schmiede.
- **10 Synergien** — fest definierte Zwei-Götter-Kombinationen, automatisch aktiv wenn beide Götter aktiv sind (Tempel ODER Schmiede ODER Gunst ≥ 50).
- **10 Wellen** — letzte Welle ist Kronos (3 Phasen, Zeit-Mechaniken: Slow-Aura, Zeit-Blasen, Rewind-Heal, Götter-Unterdrückung, Arena-Shrink).
- **Zwei Ressourcen** — Asche (Standard, von jedem Feind) und Erz (nur >40 m vom Pyros, plus `OreDeposit`-Abbau in der Gefahrenzone).

Tieferer Architektur-Überblick und Konventionen für Code-Änderungen: siehe [`CLAUDE.md`](CLAUDE.md).
