## Garden of Words — Godot 2D Development Roadmap

This document is a pragmatic, step-by-step engineering roadmap to turn the "Garden of Words" concept into a modular, maintainable Godot 4 2D game. It focuses on high cohesion, low coupling, modularity, and making classes and systems open to extension.

---

## Quick overview

- Target engine: Godot 4 (GDScript by default; substitute C# if you prefer). 
- Project intent: data-driven, scene-prefab based, signal-first architecture. 
- Big idea: typing is the primary input and gameplay mechanic — make it a clean, testable, decoupled system.

## Small contract (2–3 bullets)

- Inputs: keyboard typing (characters, backspace, enter), player movement, map/biome selection.
- Outputs: purified enemies, Word Seeds currency, UI state updates, Conservatory growth changes (persisted saves).
- Success criteria: playable core loop (enter zone -> typing combat -> purification -> rest grove -> Conservatory return) using placeholder art and no crashes.

## Top-level phases (developer checklist)

1. Project setup and tooling
2. Minimal prototype of core loop (placeholder assets)
3. Robust typing system and UI feedback
4. Aura, enemy logic, and purification VFX
5. Rest Grove / Shop / Upgrades and Conservatory hub
6. Boss framework and specialized encounters
7. Polish: art, sound, VFX, accessibility
8. Tests, CI, export, and release

## Recommended repository layout

docs/
scenes/           # Godot scene files (.tscn)
scripts/          # GDScript modules (subfolders by system)
assets/           # art, sfx, music, fonts
data/             # json/tres resources for content (words, enemies, relics)
project.godot

Keep scenes and scripts named clearly: e.g. `scenes/combat/CombatRoom.tscn`, `scripts/combat/aura.gd`.

## Design principles and patterns

- Composition over rigid deep inheritance: prefer small components (Nodes) that implement single responsibilities.
- Data-driven design: enemy types, word pools, relics and shop offers should live in resource files (JSON or Godot `Resource`/`.tres`) not hard-coded.
- Signals for decoupling: systems should communicate via signals (e.g., `typing_succeeded`, `aura_collapsed`, `seed_collected`).
- Single Responsibility & high cohesion: each script/class does one thing (e.g., TypingManager only handles typing). Keep UI separate from logic.
- Open/Closed: design base classes and use composition so new behaviors can be added without editing core systems (e.g., attach a `DistortionEffect` component to an enemy to modify visible text).

## Core systems (responsibilities & suggested names)

- Autoloads (singletons):
  - `TypingManager` (Autoload) — central API: queueWord(), cancel(), currentBuffer, emits `word_completed`, `mistyped`, `character_typed`.
  - `GameState` (Autoload) — run state, current biome, currency, save/load hooks.

- Combat / Arena:
  - `CombatRoom.tscn` — root for a combat encounter (spawns enemies, manages aura radius node, listens to TypingManager).
  - `Aura.gd` component — tracks radius, aura timer, slow-time control, emits `aura_started`, `aura_failed`, `aura_extended`.

- Enemy system:
  - `EnemyBase.gd` (script) — small API: setChallenge(word_data), onPurified(), applyDistortion(modifier). Keep logic minimal; use signals to notify death/purification.
  - Composable enemy parts: `Distortion.gd`, `MirrorWordBehavior.gd`, `SpeedyFragment.gd` attached as child nodes or script components.

- Typing challenge component:
  - `TypingChallenge.gd` (node or component) — responsible for presenting the word, tracking progress, visual caret, and reporting success/failure to owner.

- UI:
  - `HUD.tscn` — shows timer, combo, current word(s), seed count.
  - `ShopUI.gd`, `ConservatoryUI.gd` — separate concerns from game logic; call into `GameState` to apply upgrades.

- Persistence and data:
  - `SaveSystem.gd` — small wrapper around Godot `ConfigFile` or JSON saves.
  - Content in `data/words.json`, `data/enemies.json`, `data/relics.json` or as Godot `Resource`s for editor-friendly tuning.

## Scene / node architecture tips

- Use scenes as prefabs. Example CombatRoom tree:

  CombatRoom (Node2D)
  ├─ SpawnPoints (Node2D)
  ├─ Aura (Area2D, script Aura.gd)
  ├─ EnemyContainer (Node2D)
  └─ HUD (CanvasLayer)

- Keep logic in scripts, visuals in children. For example, `EnemyBase.gd` should NOT directly draw the UI; the scene's child `Sprite` or `AnimatedSprite` handles visuals.

## Typing system recommendations

- Single source of truth: `TypingManager` collects key events, exposes a buffer, validates characters against the active `TypingChallenge` instance.
- Non-blocking: typing input should be usable while animations run. Use signals so multiple systems can subscribe.
- Error handling: provide lenient and strict modes (configurable). On error, `TypingManager` emits `mistyped` with context.
- Accessibility: keep an option for larger fonts and higher-contrast text; allow remapping of keys via InputMap.

## Data-driven content

- Word pools by biome: keep structured lists, e.g. `data/biomes/glade.json` with arrays for easy tuning.
- Enemy definitions reference word pools and components:
  {
    "id": "echo_wisp",
    "word_pool": "biomes/glade.json",
    "components": ["mirror", "slow_drift"],
    "reward_seeds": 3
  }

- Relics and upgrades should store modifiers (percent/flat) and be applied through a clear Upgrade API.

## Making classes open to extension

- Provide small base contracts and events. Example `EnemyBase` API:
  - func set_challenge(challenge: Dictionary) -> void
  - signal purified(enemy: Node, reward: int)
  - func apply_modifier(mod: Resource) -> void

- For new enemy behaviors, create components that implement a common interface and can be attached without changing `EnemyBase`.

## Quality gates / tests

- Test types: unit tests for `TypingManager`, scene tests for `CombatRoom` spawn/cleanup, integration tests for save/load.
- Prefer Godot test runners or community plugin GUT. Add one happy-path test for `TypingManager` (type full word, expect `word_completed`).
- Lint/format: adopt `gdformat` and a linter (community tools exist) to keep code style consistent.

## Edge cases & known pitfalls

- Keyboard focus: ensure typing continues when dialogs open or when the game window regains focus.
- Large word lists: lazy-load data or pool objects to avoid spikes.
- Concurrency: avoid tight coupling between `TypingManager` and many listeners to prevent hard-to-follow race conditions — use signals and small queues.
- Save/load schema changes: version saves and provide migration paths.

## Performance & optimization guidance

- Pool enemies and VFX to avoid alloc churn.
- Profile early with Godot's built-in profiler; keep the typing input loop lightweight (avoid heavy allocations per keypress).

## Art & audio pipeline notes

- Keep placeholder alpha art for prototyping. Replace with batches later.
- Use OGG/FLAC for music and compressed formats for SFX. Integrate an AudioManager singleton for volume controls and audio bus routing.

## Accessibility & UX details

- Configurable font size and contrast.
- Optional “lenient typing” mode (accept small typos) and `time dilation` strength slider.
- Clear visual feedback for success, partial success, and errors (audio + VFX + UI).

## Minimal test plan (happy + a couple edge cases)

- Happy: Player types a displayed word fully within the aura timer -> enemy purified -> seeds increment.
- Edge: Player mistypes several times -> aura collapses -> player takes penalty; verify state resets correctly for next encounter.

## Deliverables per milestone (what to finish before moving on)

- Prototype: working CombatRoom with at least one enemy type and typing interaction.
- Alpha: Rest Grove + Shop, one biome with 3 room transitions, Conservatory basic UI.
- Beta: Boss encounter template, audio and visual polish, save/load working.
- Release candidate: basic export presets, short README, and selected platform build(s).

## Suggested short timeline (example)

- Week 1: Project setup, TypingManager, simple CombatRoom prototype.
- Week 2: EnemyBase, Aura, basic enemy behaviors.
- Week 3: Shop/Rest Grove, seed currency, Conservatory skeleton.
- Week 4: Boss framework, polish and playtesting, create content roadmap for words and relics.

## How to use this roadmap

- Start at the top (Project setup). Make the smallest playable slice that satisfies the contract. Iterate in small vertical slices (prototype -> add one feature -> test -> repeat).
- Keep changes small and commit often. Use the todo list in your repo to track progress (we've seeded an initial list).

## Next steps (immediate)

1. Open the project in Godot (`project.godot`) and validate that Godot loads the workspace.
2. Create `scripts/typing/TypingManager.gd` (autoload) and implement a minimal API: buffer, queueWord(), emit `word_completed` when full.
3. Build `scenes/combat/CombatRoom.tscn` with `Aura` and a single `Enemy` placeholder.

---

Files added/edited by this roadmap:

- `docs/Godot_2D_Development_Roadmap.md` — this file; high-level engineering plan and checklist.

Completion summary

This roadmap gives a step-by-step plan designed to maximize modularity and testability. Work in vertical slices, prefer small, composable components, and keep your content data-driven so designers can tune without changing code.

If you want, I can:
- scaffold the repository (create script stubs / scene placeholders), or
- implement `TypingManager.gd` and a tiny `CombatRoom.tscn` prototype next.

Good luck — let's build one small, beautiful loop at a time.
