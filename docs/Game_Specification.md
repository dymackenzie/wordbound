# Garden of Words — Game Specification

This specification consolidates the design and engineering guidance from the concept and roadmap docs into a single actionable reference for development. It focuses on functional requirements, non-functional requirements, system interfaces, scene and node layouts, resource formats, and a concise test plan.

Use this file as the authoritative spec for engineers and designers when implementing features. Keep it updated as APIs or data formats change.

---

## 1. Goals & Scope

- Deliver a focused, single-run typing roguelike loop: enter biome -> encounter -> typing combat -> purification -> rest grove -> return to Conservatory.
- Prioritize modular, testable systems (TypingManager, Enemy components, Aura, CombatRoom). Keep visuals and content data-driven.
- Create clean contracts (signals/functions) so systems can evolve independently.

## 2. High-level Functional Requirements

FR-1: Player can enter a selected Biome and start a run.
FR-2: When an enemy enters the player's Aura, a typing challenge appears and time slows.
FR-3: Successful typing purifies the enemy, increments Word Seeds, and produces VFX.
FR-4: Mistyped or timed-out challenges reduce the aura timer; repeated failures lead to aura collapse and end-of-run consequences.
FR-5: After clearing an area a Rest Grove node appears offering a Shop (spend Seeds), Relics (temporary modifiers), and upgrades.
FR-6: After run end (victory/defeat), player returns to Conservatory where Seeds can be spent, flora placed, and narrative fragments viewed.
FR-7: The game persists meta-progression (seeds, unlocked relics, conservatory state) between sessions.

## 3. Non-functional Requirements

- NFR-1: The core typing input must be responsive (no perceptible lag on keypress).
- NFR-2: Systems must be extensible: new enemy behaviors and relics should be addable without modifying core systems.
- NFR-3: Memory allocations per keypress should be minimized; pool enemies and VFX.
- NFR-4: Accessibility: font sizes and contrast must be configurable; remappable keys via InputMap.

## 4. Acceptance Criteria (for the minimum playable slice)

- A Godot project opens and runs a simple scene.
- TypingManager accepts character input and emits `word_completed` when a challenge is finished.
- CombatRoom spawns one Enemy, enters Aura on proximity, shows a TypingChallenge, and allows purification via typing.
- Seeds counter increments on purification and persists to a simple save file.

## 5. System Components & Contracts

Below are the core systems, their responsibilities, public APIs, and signals. Use Godot signals and small method contracts to decouple.

5.1 Autoloads (Singletons)

- TypingManager (autoload)
  - Responsibilities: collect keyboard input, route characters to the active TypingChallenge, maintain optional lenient/strict modes.
  - Public API:
    - func queue_challenge(challenge: TypingChallenge) -> void
    - func cancel_challenge() -> void
    - func get_buffer() -> String
  - Signals:
    - signal character_typed(char: String)
    - signal mistyped(info: Dictionary)
    - signal word_completed(challenge_id: String, result: Dictionary)

- GameState (autoload)
  - Responsibilities: global run state, current biome, run seeds, save/load hooks, apply upgrades.
  - Public API:
    - func add_seeds(amount: int) -> void
    - func spend_seeds(amount: int) -> bool
    - func save() -> void
    - func load() -> void
  - Signals:
    - signal seeds_changed(new_total: int)

5.2 Combat / Arena

- CombatRoom (scene + controller script)
  - Responsibilities: spawn enemies, manage aura node, progress transitions between rooms.
  - Exposed behavior:
    - func start_encounter(config: Dictionary) -> void
    - func end_encounter() -> void
  - Signals:
    - signal encounter_cleared()

- Aura (component: Aura.gd)
  - Responsibilities: detect enemies in radius, start/stop slow-time, track aura timer, emit aura events.
  - API:
    - func extend(time: float) -> void
    - func collapse() -> void
  - Signals:
    - signal aura_started()
    - signal aura_failed()
    - signal aura_extended(additional_time: float)

5.3 Enemy system

- EnemyBase (EnemyBase.gd)
  - Responsibilities: hold state for an enemy instance, expose set_challenge(), handle purification, and notify reward.
  - API:
    - func set_challenge(challenge: Dictionary) -> void
    - func apply_modifier(mod: Resource) -> void
  - Signals:
    - signal purified(enemy: Node, reward: int)
    - signal died(enemy: Node)

- Enemy behavior components: Distortion.gd, MirrorWordBehavior.gd, SpeedyFragment.gd
  - Responsibilities: augment enemy behavior by listening to TypingManager/TypingChallenge and altering presentation or timing.
  - Contract: must implement `apply_to_enemy(enemy: Node) -> void` and optionally connect to `word_progress` signals.

5.4 Typing challenge component

- TypingChallenge (TypingChallenge.gd)
  - Responsibilities: display the challenge text, accept character input from TypingManager, track progress, report success/failure.
  - API:
    - func start(challenge_id: String, text: String, time_limit: float) -> void
    - func cancel() -> void
  - Signals:
    - signal progress(challenge_id: String, typed: String, remaining_time: float)
    - signal completed(challenge_id: String, accuracy: float, time_left: float)
    - signal failed(challenge_id: String, reason: String)

5.5 UI components

- HUD.tscn / HUD.gd
  - Responsibilities: show current seeds, timer, current word(s), combos, and lenient/strict state.
  - Interactions: subscribes to GameState.seeds_changed, TypingManager.character_typed, TypingChallenge.progress.

- ShopUI.gd, ConservatoryUI.gd
  - Responsibilities: present upgrades, confirm purchases, call GameState.spend_seeds.

5.6 Persistence

- SaveSystem.gd
  - Responsibilities: handle reading/writing JSON or Godot ConfigFile for meta-progression.
  - Save Schema (example):
    {
      "version": 1,
      "seeds": 123,
      "unlocked_relics": ["echo_bloom"],
      "conservatory": {"flora": ["willow_tree"]}
    }

## 6. Data formats and examples

6.1 Biome / Word Pools (JSON)

Example: `data/biomes/glade.json`

```
{
  "id": "glade",
  "word_pools": {
    "easy": ["seed", "bloom", "soft"],
    "medium": ["whisper", "gentle", "unfurl"],
    "hard": ["resilience", "reverberate", "illumination"]
  }
}
```

6.2 Enemy definition (JSON)

Example: `data/enemies/echo_wisp.json`

```
{
  "id": "echo_wisp",
  "word_pool": "data/biomes/glade.json#medium",
  "components": ["mirror", "slow_drift"],
  "reward_seeds": 3,
  "sprite": "res://assets/enemies/echo_wisp.png"
}
```

6.3 Relic / Upgrade schema

Relics: temporary modifiers applied at run start. Store as JSON with modifier values and a human-friendly description.

```
{
  "id": "echo_bloom",
  "type": "relic",
  "description": "Each successful word extends aura by 0.5s",
  "modifiers": {"aura_extend_on_success": 0.5}
}
```

## 7. Signals & Event Flow

Common signals and a typical flow:

1. `Aura` detects enemy in range -> `aura_started` emitted.
2. CombatRoom requests an enemy to set its challenge; `TypingManager.queue_challenge()` is called with a TypingChallenge instance.
3. `TypingManager` emits `character_typed` per key; `TypingChallenge` emits `progress` updates for UI.
4. `TypingChallenge` emits `completed` or `failed`.
   - On `completed`: `EnemyBase` receives success -> emits `purified` and reward processed; `GameState.add_seeds()` called.
   - On `failed`: `Aura` reduces timer or calls `aura_failed` if out of time.

Design note: avoid synchronous blocking calls in this chain — use signals so multiple listeners can react without tight coupling.

## 8. Scene & Node Layouts (examples)

8.1 CombatRoom.tscn (Node2D)

- Node structure:

  CombatRoom (Node2D, script CombatRoom.gd)
  ├─ SpawnPoints (Node2D)
  ├─ Aura (Area2D, script Aura.gd)
  ├─ EnemyContainer (Node2D)
  └─ HUD (CanvasLayer, scene HUD.tscn)

Implementation notes:
- Use EnemyContainer to pool/instance enemies.
- Aura is responsible for slow-time and will call CombatRoom to spawn TypingChallenge UI overlays for enemies in range.

8.2 Conservatory.tscn (Node2D)

- Node structure:

  Conservatory (Node2D)
  ├─ Garden (Node2D)
  ├─ ShopPanel (CanvasLayer)
  └─ PhotoModeController (Node)


## 9. Testing Plan

- Unit tests (GUT or Godot Test Runner):
  - TypingManager: type a sequence -> expect `word_completed` with correct accuracy.
  - SaveSystem: save -> load -> verify persisted values.

- Scene tests:
  - CombatRoom: spawn enemy -> type word -> expect enemy purified and `encounter_cleared` when all enemies done.

- Manual QA checks:
  - Keyboard focus recovery, large wordpool performance, accessibility settings toggles.

## 10. Edge Cases & Error Handling

- Input focus lost: TypingManager must buffer or ignore inputs until focus returns. Provide clear UI state (paused/typing disabled).
- Load failure: SaveSystem should gracefully fall back to defaults and report version mismatches.
- Race conditions: Use queues for incoming challenges; prevent two simultaneous TypingChallenge instances from owning the same input stream.

## 11. Milestones & Deliverables

- M1 (Prototype): TypingManager, CombatRoom with one Enemy, basic HUD and seeds persistence.
- M2 (Alpha): Multiple enemy types, Aura chaining, Rest Grove shop, simple Conservatory.
- M3 (Beta): Boss framework, many relics/upgrades, polish VFX/SFX, accessibility options.
- M4 (Release): Export builds, README, user settings, and fast feedback patching process.

## 12. Acceptance tests (concrete)

- Test A (Happy path): Start CombatRoom -> Enemy enters Aura -> TypingChallenge appears -> Player types accurately -> Enemy purified -> Seeds incremented and persisted.
- Test B (Failure path): Start CombatRoom -> Mistype until timer depletes -> Aura fails -> CombatRoom ends run with appropriate state saved.

## 13. Next actions (immediate)

1. Add `scripts/typing/TypingManager.gd` as an autoload with the minimal API described above.
2. Create `scenes/combat/CombatRoom.tscn` with `Aura` and a placeholder `Enemy` scene and wire TypingManager to the TypingChallenge.
3. Add unit test for TypingManager (happy path) using a Godot test runner or GUT.
