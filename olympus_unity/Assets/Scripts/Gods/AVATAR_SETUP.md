# Avatar Setup Guide

Götter-Avatare sind temporäre 30-s-Super-Formen, die der Spieler bei
**Favor = 100** auslöst (Taste **G** triggert den Hauptgott). Hephaistos
hat **keinen Avatar** — die Schmiede ist die Hauptbelohnung.

## Architektur

```
FavorManager.TryActivateAvatar(god)        ← Eingangstor (Validierung)
    └─ feuert OnAvatarStarted(god)
        └─ AvatarSpawnSystem.SpawnAvatar   ← instanziiert Prefab
            └─ AvatarBase.Awake/Start      ← NavMeshAgent, Layer "Ally"
            └─ AvatarBase.Update           ← Ziel-Suche + DoAttack/DoSpecial
   …30 s später…
FavorManager.EndAvatar(god)
    └─ feuert OnAvatarEnded(god)
        └─ AvatarSpawnSystem.DespawnAvatar
            └─ AvatarBase.StartDespawn     ← Fade-Out + Destroy
```

## Avatar-Prefab-Setup (gleich für alle 5)

```
ZeusAvatar (NavMeshAgent)
├── NavMeshAgent (Speed=variiert pro Gott, Radius=0.6)
├── CapsuleCollider (Layer: Ally — Layer 11)
├── Mesh (großer leuchtender Capsule, Gott-Farbe)
├── ParticleSystem "AuraFX" (gott-spezifischer Aura-Effekt)
└── ZeusAvatar.cs           ← bzw. AthenaAvatar / AresAvatar / PoseidonAvatar / HadesAvatar
```

**Layer/Tag:** `Ally` (Layer 11), Tag `Ally` werden in `AvatarBase.Awake` gesetzt.

**Materialien** sollten Alpha-Fading unterstützen (URP: Surface Type = Transparent),
damit der Despawn-Fade-Out funktioniert. `AvatarBase.FadeMaterial` versucht
sowohl `_Color` (Standard-Shader) als auch `_BaseColor` (URP) zu setzen.

## Per-Gott Stat-Defaults (in `Awake` der Subklasse)

| Gott      | Speed | Damage | Atk-CD | Special-CD | Special                              |
|-----------|-------|--------|--------|------------|--------------------------------------|
| Zeus      | 7.0   | 60     | 0.7    | 4.0        | Blitz-AoE (6 m, 80 dmg)              |
| Athena    | 5.0   | 40     | 0.9    | 6.0        | Pyros-Heal +25 + Wisdom-Burst (5 m)  |
| Ares      | 9.0   | 75     | 0.5    | 3.0        | 360°-Schwung (5 m, 70 dmg)           |
| Poseidon  | 6.0   | 50     | 0.8    | 5.0        | Flutwelle (9 m, 35 dmg + 0.4× Slow)  |
| Hades     | 5.5   | 45     | 0.8    | 5.0        | 3 Schatten-Spawn um Avatar           |

Werte sind im Inspector der jeweiligen Subklasse pro-Prefab überschreibbar
(`[SerializeField]` auf alle Felder in `AvatarBase` und den Subklassen).

## AvatarSpawnSystem-Setup

Auf dem **Singletons-GameObject**:
```
Singletons
└── AvatarSpawnSystem.cs
    └── prefabEntries: Liste befüllen
        ├── { God: Zeus,     Prefab: ZeusAvatar.prefab }
        ├── { God: Athena,   Prefab: AthenaAvatar.prefab }
        ├── { God: Ares,     Prefab: AresAvatar.prefab }
        ├── { God: Poseidon, Prefab: PoseidonAvatar.prefab }
        └── { God: Hades,    Prefab: HadesAvatar.prefab }
```

(Hephaistos-Eintrag bleibt leer — `FavorManager.TryActivateAvatar`
lehnt Hephaistos sowieso ab, der Spawn-Pfad wird nie betreten.)

## Status

- [x] AvatarBase (NavMesh-Targeting, Special-Cooldown, Despawn-Fade)
- [x] AvatarSpawnSystem (Event-Listener, Prefab-Map, Despawn-Forwarding)
- [x] ZeusAvatar / AthenaAvatar / AresAvatar / PoseidonAvatar / HadesAvatar
      (Stub-Specials — volle KI in P3-05..P3-09)

## Offen (in den Phase-3-Tasks pro Gott)

- [x] **P3-05 Zeus** — Kettenblitz-Avatar + Blitzeinschlag/Donnersturm-Interventionen
- [x] **P3-06 Athena** — Pyros-Barriere-Avatar + HP-Regen-Passiv +
      Strategische-Übersicht/Tempo-Schub-Interventionen
- [x] **P3-07 Ares** — Kill-Streak-Avatar mit Aggro-Pull + Kriegsschrei/
      Berserker-Interventionen, Kill-Streak-Passiv über damageMultiplier,
      war_strategy-Synergie heilt Gebäude per BuildingBase.Heal
- [x] **P3-08 Poseidon** — Radial-Flutwelle + Vorwärts-Kegel-Slow als
      Wassermauer-Avatar; Flutwelle/Erdspaltung-Interventionen
- [x] **P3-09 Hades** — Avatar markiert alle Schatten beim Spawn und
      bei jedem Special permanent; Massen-Beschwörung-/Seelen-Sog-Interventionen
