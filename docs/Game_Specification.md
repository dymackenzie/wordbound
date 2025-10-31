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

- Player can control an avatar that moves around the room, has an Aura, and can activate a kill-aura by pressing Enter when enemies are inside the aura.
 - Player can control an avatar that moves around the room, has an Aura, and can activate a kill-aura by pressing Enter when enemies are inside the aura.
 - Activating the kill-zone triggers a short cinematic: the camera smoothly zooms in on the player, global time slows (configurable timescale), and a TypingChallenge is instantiated above the targeted enemy; an Aura timer UI (clockwise arc) starts and letters typed extend the timer. If the aura timer expires the player receives damage and the cinematic ends.

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

5.7 Player (avatar, scene + controller)
  - Responsibilities: representation of the player avatar in rooms; movement, relic storage and activation of the Aura. The Player node owns a child `Aura` (Area2D) that detects nearby enemies. When enemies are detected the HUD shows an activation prompt; pressing Enter activates the kill-aura or starts the appropriate interaction (typing challenge or immediate purification depending on balance/design).
  - Public API:
    - func move(direction: Vector2, delta: float) -> void
    - func attempt_activate_aura() -> void
    - func dash() -> void  # upgradeable
    - func equip_relic(relic_id: String) -> void
    - func unequip_relic(relic_id: String) -> void
  - Signals:
    - signal aura_activation_ready(has_enemies: bool)
    - signal aura_activated()
    - signal dashed()
    - signal relic_equipped(relic_id: String)
    - signal relic_unequipped(relic_id: String)

  - Implementation notes:
    - The `Player` scene should be a `CharacterBody2D` (or `KinematicBody2D` in older Godot) with exported movement speed, acceleration and optional gravity if needed for variants.
    - The player contains a child `Area2D` node named `Aura` with a CollisionShape2D sized to the desired radius. `Aura` tracks bodies that enter/exit and exposes whether activation is allowed.
    - `attempt_activate_aura()` is called by input (Enter key). If there are enemies in the aura, the game can either (A) instantiate a TypingChallenge for the nearest enemy (preferred for consistency), or (B) perform an immediate purification effect if a relic/upgrade permits instant activation.
    - `dash()` is an upgradeable ability that performs a short high-speed movement in the current input direction with a short cooldown. Dash can be gated by relics or unlocked via progression.
    - The player holds a small list/table of equipped relic ids and can serialize/deserialize this via `GameState` for persistence.

5.8 Kill-zone / Cinematic Flow
  - Purpose: define the UX and implementation contract for the cinematic kill-zone that occurs when the player activates their Aura on nearby enemies.
  - Trigger:
    - Player calls `attempt_activate_aura()` (Enter). If `Aura.has_enemies_inside()` is true the CombatRoom chooses a target enemy (nearest or by priority) and begins the kill-zone flow.
  - Cinematic behaviour:
    - Camera: smoothly interpolate the active Camera2D zoom toward a configured `KillZoom` centered on the player over `ZoomDuration` seconds.
    - Time dilation: set `Engine.TimeScale` to a configurable `KillTimeScale` (e.g., 0.2) for the duration of the cinematic. Ensure audio pitch and input remain usable or provide overrides for accessibility.
    - TypingChallenge placement: instantiate the TypingChallenge above the target enemy. The challenge should be parented to a world-overlay node so it follows the enemy's world position (or use a CanvasLayer with position converted from world to screen coordinates).
  - Aura timer & visuals:
    - Start an Aura timer displayed as a clockwise arc (UI) around the player or as a screen HUD element tied to the challenge instance.
    - The arc decreases smoothly from full to empty over the configured `AuraTimeLimit` unless replenished.
    - Each correct letter typed awards `LetterTimeBonus` seconds (configurable) added to the remaining aura time, clamped to a `AuraMaxTime` value.
    - The arc must render clockwise reduction; update frequency should be smooth and visually tied to unscaled time so it feels consistent during slow-mo.
  - Typing timing during slow-mo:
    - TypingChallenge countdowns must use unscaled time (OS.GetTicksMsec() or an equivalent real-time source) so the player experiences normal typing responsiveness while the world is slowed.
  - Success & failure outcomes:
    - Success: When the TypingChallenge emits `completed`, purify the target enemy, award seeds, play VFX/SFX, stop the cinematic, restore camera zoom and `Engine.TimeScale` to normal, and hide the Aura timer UI.
    - Failure: If the Aura timer reaches zero before completion, emit `aura_failed`, apply configured damage to the player (or other penalty), end the cinematic, restore camera and time, and cleanup the challenge UI.
  - Configuration knobs (recommended exports):
    - KillZoom (Vector2), ZoomDuration (float), KillTimeScale (float), AuraTimeLimit (float), LetterTimeBonus (float), AuraMaxTime (float), DamageOnTimeout (int).
  - Accessibility and options:
    - Provide options to reduce or disable cinematic zoom/time-slow for accessibility.
    - Allow an alternate mode where slow-mo is less extreme but the TypingChallenge still uses unscaled timing.


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
    - func has_enemies_inside() -> bool
    - func get_enemies_inside() -> Array
  - Signals:
    - signal aura_started()
    - signal aura_failed()
    - signal aura_extended(additional_time: float)

Design note: the Aura component can be used as both a passive detection mechanic (starts a TypingChallenge when enemies enter) and an actively-operated kill-aura triggered by the player pressing Enter. Use configuration flags (e.g., `auto_trigger_on_enter`) to choose behaviour per enemy type or per difficulty.

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
  ├─ Player (CharacterBody2D, script Player.gd)
  │   └─ Aura (Area2D, script Aura.gd)
  ├─ EnemyContainer (Node2D)
  └─ HUD (CanvasLayer, scene HUD.tscn)

Implementation notes:
- Use EnemyContainer to pool/instance enemies.
- Aura is responsible for slow-time and will call CombatRoom to spawn TypingChallenge UI overlays for enemies in range.
 - Player movement: the `Player` node moves under player control inside CombatRoom; the `Aura` child on the player detects enemies and coordinates activation prompts. When the player presses Enter and `Aura.has_enemies_inside()` is true, the `Player` should call into `CombatRoom` / `TypingManager` to begin an interaction for one or more enemies.

Implementation notes for Dash upgrade:
- Dash should be implemented on `Player` as a short, directional impulse with an independent cooldown timer. Expose `DashDistance`, `DashCooldown`, and `DashInvulnerabilityTime` as export variables so relics can modify them. When the player unlocks the dash upgrade, enable input binding (e.g., Shift or a gamepad button) and emit `dashed()` when used.

Upgrade example: unlocking Dash
- At progression time (from GameState or via an in-game shop), set a player flag `Player.CanDash = true`. Also allow relics to temporarily grant additional dash charges or reduce cooldown.

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

## 14. Diagrams (flow & signal sequences)

Below are compact, text-first diagrams you can paste into design docs or expand into visual diagrams in a diagram tool (draw.io, Figma, Mermaid).

14.1 High-level run flow

```
[Conservatory] -> (Choose Biome & Relics) -> [Descent Entry]
    -> [CombatRoom A] -> [Rest Grove] -> [CombatRoom B] -> ... -> [Boss]
    -> [Return to Conservatory]
```

14.2 CombatRoom internal sequence (ASCII)

```
Player moves -> Enemy enters Aura (Area2D)
    Aura emits -> combat_time_dilation starts
    CombatRoom spawns TypingChallenge for Enemy
    TypingManager routes keyboard -> TypingChallenge
    TypingChallenge emits progress/completed/failed
        completed -> EnemyBase.purify() -> emit purified -> GameState.add_seeds()
        failed -> Aura.reduce_time() -> maybe aura_failed -> apply penalty
```

14.3 Signal sequence (detailed)

```
Aura (aura_started) -> CombatRoom: select enemy and create TypingChallenge
CombatRoom -> TypingManager.queue_challenge(challenge)
TypingManager -> emits character_typed for each keypress
TypingChallenge -> emits progress -> HUD updates
TypingChallenge -> emits completed -> EnemyBase.handle_purified
EnemyBase -> emits purified -> GameState.add_seeds
GameState -> emits seeds_changed -> HUD updates
```

Use these as the authoritative sequences when wiring signals in code and for creating unit/integration tests that assert the correct emission order and payloads.

## 15. Relic math & examples

This section defines canonical rules for how Relics and Upgrades modify gameplay numbers. Use percentages for scaling where possible and prefer additive stacking for clarity unless specified as multiplicative.

15.1 Modifier model (canonical)

- Each relic exposes a set of modifiers. A modifier is a small dictionary with the following shape:

```
{
  "key": "aura_extend_on_success",
  "type": "additive" | "multiplicative",
  "value": 0.5
}
```

- Interpretation:
  - `key`: the numeric attribute to change (see canonical keys below).
  - `type`: stacking mode. `additive` means sum values; `multiplicative` means multiply current value by (1 + value).
  - `value`: numeric magnitude (seconds, percent, or flat amount depending on key).

15.2 Canonical modifier keys

- `aura_duration_base` (seconds): base aura slow-time duration when a challenge starts.
- `aura_extend_on_success` (seconds): time added to aura when a word is completed.
- `aura_regen_per_second` (seconds/sec): passive recovery of aura time while not fully depleted.
- `seed_reward_multiplier` (multiplier): multiplies seed rewards from an enemy.
- `typing_leniency` (float 0..1): fraction of allowed errors (0 = strict, 1 = accept all).

15.3 Stacking rules

- Additive modifiers for the same key are summed first. Example: two relics with `aura_extend_on_success` values 0.5 and 0.25 result in +0.75s extension.
- Multiplicative modifiers are applied after additive sums. Example: base aura duration 3s, additive modifiers sum +1s -> interim 4s. Then a multiplicative modifier `0.2` yields final duration = 4s * (1 + 0.2) = 4.8s.
- For `seed_reward_multiplier`, multiple multiplicative modifiers compound multiplicatively: e.g., two multipliers 0.1 and 0.2 => final multiplier = (1 + 0.1) * (1 + 0.2) = 1.32.

15.4 Example relic definitions and computed outcomes

- Relic A: `{"id":"echo_bloom","modifiers":[{"key":"aura_extend_on_success","type":"additive","value":0.5}]}`
- Relic B: `{"id":"calm_seed","modifiers":[{"key":"aura_duration_base","type":"additive","value":1.0},{"key":"seed_reward_multiplier","type":"multiplicative","value":0.1}]}`

Given base: aura_duration_base = 3.0s, aura_extend_on_success = 0.0, seed_reward_multiplier = 1.0

Apply Relic A and Relic B:
- aura_extend_on_success total = 0.5 (additive)
- aura_duration_base total (additive) = 3.0 + 1.0 = 4.0
- seed_reward_multiplier (multiplicative) = 1.0 * (1 + 0.1) = 1.1

If player completes a word: aura adds 0.5s; base aura is 4.0s. If enemy reward_seeds = 3, seeds awarded = floor(3 * 1.1) = 3 (or use configurable rounding—recommend `Math.round` for fairness).

15.5 Rounding and deterministic behavior

- Always round deterministic game economy numbers using a single rule. Recommended: use `Math.round()` for rewards, not floor, to avoid systematic bias.
- Keep the RNG deterministic per-run where necessary (seeded RNG) to allow reproducible tests.

15.6 Extending relic math

- To add new modifier keys, register them in `GameState` with:

  - a default base value
  - a stacking rule (additive or multiplicative)
  - serialization name

- When reading relics at run start, compute an `ActiveModifiers` table once and cache it on `GameState` for fast lookup during gameplay.

## 16. Save versioning & migration plan

Design the save format to include a top-level `version` integer. SaveSystem must provide a migration API that transforms old schemas to the current schema during load.

16.1 Save file header (must include)

```
{
  "version": 2,
  "payload": { ... }
}
```

16.2 Migration strategy

- On `SaveSystem.load()`:
  1. Read file, determine `version` (if missing, assume 1).
  2. If `version` < CURRENT_VERSION, run sequential migrations `migrate_v1_to_v2`, `migrate_v2_to_v3`, ... until up-to-date.
  3. Each `migrate_vX_to_vY` returns a transformed payload and logs changes.
  4. After migrating, write back the updated file (optional, but recommended) and continue load.

16.3 Example migration steps

- v1 -> v2: rename `word_seeds` to `seeds` and move `conservatory` under `payload.conservatory`.

```
func migrate_v1_to_v2(data: Dictionary) -> Dictionary:
    var out = {}
    out["seeds"] = data.has("word_seeds") ? data["word_seeds"] : 0
    out["unlocked_relics"] = data.get("unlocked_relics", [])
    out["conservatory"] = data.get("conservatory", {})
    return out
```

- v2 -> v3: split `conservatory.flora` items into objects with `id` and `planted_at` timestamp.

16.4 Migration testing

- Ship a small set of historical sample saves (v1, v2) in `docs/sample_saves/` and include unit tests that load them and assert current schema fields are present after migration.

16.5 Backwards compatibility & user safety

- Always keep the original save file until a migration completes successfully. Write migrated file to a temporary path and then replace the original after successful validation.
- If migration fails, revert to original and surface a clear error to the player with instructions for manual backup.

## 17. Validation & schema checks

- When loading data files (JSON / `.tres`) validate required fields and types early (on project start or data load). Fail-fast with readable error messages that list the offending file and key.
- Provide a `DataValidator` tool that runs in editor or in CI to assert all `data/enemies`, `data/biomes`, and `data/relics` conform to schema.

## 18. Performance budget and targets

- Input loop: respond to keypress within 8ms on target hardware (desktop). Profile and keep per-keypress logic allocation-free.
- Enemy spawn/VFX: use pools. Aim for <5ms allocation spikes when entering a combat room.
- Memory: keep resident working set minimal; pool large arrays for word lookups.

## 19. Detailed test matrix

- TypingManager:
  - Happy: single challenge, type perfectly -> `word_completed` true.
  - Edge: rapid keypresses, backspace, and paste events -> buffer sanity.
  - Focus loss: while typing, window loses focus -> input disabled then restored.

- Aura & CombatRoom:
  - Chain: complete multiple enemies in sequence -> aura extends correctly.
  - Fail: repeated mistakes reduce aura -> aura_failed triggers room end.

- Save migrations:
  - Load v1 sample -> assert migrated fields
  - Corrupt save -> assert graceful fallback and user-visible error

## 20. Documentation & editor tools

- Add editor-only tools for designers:
  - Word pool previewer: shows sampling distribution and example words per difficulty.
  - Relic composer: craft a relic JSON and preview its computed `ActiveModifiers` impact.
  - Save migration tester: load historical samples and show diffs.

## 21. Enemy selection & spawning (biome / room rules)

This section specifies how to determine which enemies spawn in a biome or room. The goal is a data-driven, designer-friendly, deterministic system that composes encounters from an enemy pool while respecting difficulty budgets and runtime modifiers.

21.1 Principles

- Data-driven: designers declare enemy pools, weights, complexity costs and tags in JSON (or `.tres`).
- Deterministic: selection uses a seeded RNG so previews and tests are reproducible.
- Budgeted complexity: each room has a complexity budget; enemies consume cost toward that budget so rooms remain balanced.
- Constraint-aware: respect min/max counts, banned tag combos, and runtime filters (relics, difficulty).

21.2 Selection algorithm (weighted sampling + complexity budget)

Recommended approach (pseudocode):

```
func choose_enemies(biome, room_index, rng, active_modifiers) -> Array:
    var budget = biome.base_room_complexity + room_index * biome.complexity_scale_per_room
    budget *= (1 + active_modifiers.get("spawn_density_multiplier", 0))
    var selected = []
    var attempts = 0
    var candidates = filter_candidates(biome.enemy_pool, active_modifiers, biome.allowed_tags)
    while budget > 0 and attempts < 200:
        attempts += 1
        var enemy_id = weighted_sample(candidates, rng)
        var enemy = load_enemy_definition(enemy_id)
        if selected.count(enemy_id) >= enemy.get("max_count", 999): continue
        if enemy.get("complexity", 1) > budget: continue
        if violates_banned_combos(selected, enemy, biome.banned_tag_combinations): continue
        selected.append(enemy_id)
        budget -= enemy.get("complexity", 1)
    return selected
```

Notes:
- Cap attempts to avoid infinite loops.
- Use a fallback pool (low-cost enemies) if selection fails to fill expected content.

21.3 Templates & augmentation

- Allow designer-crafted `encounter_templates` (predefined enemy lists) selectable by chance. Templates can be used for scripted or signature rooms and then augmented with random adds using the budget method.

21.4 Runtime modifiers and integration

- Apply active modifiers from `GameState` before selection (e.g., `spawn_density_multiplier` increases the budget).
- Difficulty scaling can adjust base_room_complexity or multiply enemy complexity to raise challenge.
- Tag filters: relics or events can add or remove allowed tags at runtime; apply these in `filter_candidates()`.

21.5 Spawn scheduling & pacing

- `CombatRoom` precomputes the spawn list and schedules spawns over time rather than instancing all at once. Use `spawn_mode` to vary timing (swarm = clustered, ambush = simultaneous reveal).
- Use `EnemyContainer` object pool for instantiation to avoid allocations.

21.6 Deterministic RNG & testing

- Use Godot's `RandomNumberGenerator` seeded per-run and optionally per-room to produce reproducible spawn lists. Expose seed to designers for preview.
- Add unit tests that assert deterministic outputs for a given seed and biome file.

21.7 Balancing guidance

- Map complexity to expected seconds-to-clear or cognitive load; tune costs by playtests (e.g., simple = 1, medium = 2, hard = 4).
- Track telemetry (avg clear time, failure rate) and adjust weights/complexity accordingly.

21.8 Example JSON snippets

Biome (`data/biomes/glade.json`):
```
{
"id": "glade",
"base_room_complexity": 6,
"complexity_scale_per_room": 0.5,
"enemy_pool": [
    {"enemy_id": "echo_wisp", "weight": 10},
    {"enemy_id": "flicker", "weight": 20},
    {"enemy_id": "loomer", "weight": 5}
],
"banned_tag_combinations": [["mirror","reverse"]]
}
```

Enemy (`data/enemies/echo_wisp.json`):
```
{
"id": "echo_wisp",
"complexity": 2,
"weight": 10,
"min_count": 0,
"max_count": 4,
"tags": ["mirror"],
"components": ["mirror_behavior"],
"reward_seeds": 3
}
```

21.9 Editor tooling & previews

- Provide a `Spawn Previewer` editor tool that runs the selection algorithm with a fixed seed and shows multiple sample rooms and histograms of enemy picks. This helps designers tune weights and complexity curves without running the full game.

21.10 Edge cases & safety

- If all candidates are filtered out by tags/constraints, fall back to a `safe_fallback_pool` to ensure content is present.
- Respect `max_active_enemies` and performance caps; selection must never schedule more than configured live enemies.
- Log and surface selection failures during development (DataValidator / CI) so designers can fix pools or templates.