# JellyBuster Clone

A Unity clone of **Jelly Busters** (Moon Active).

## Working rule (set by the user on 2026-10-03)

**Let the user build this themselves.** Do not implement game features, scripts, or
systems on your own initiative. Only implement something when the user explicitly
asks for it in a given message.

When something *is* implemented on request, **explain how it was built** — the
approach taken, key decisions, and why — so the user can follow along and learn
from it, not just receive a finished artifact.

Exploration, answering questions, reviewing the user's own code, and generating
requested assets (like sprites) or tooling config (like `.gitignore`) are all fine
without a special ask — the rule is about not writing game logic/systems unprompted.
When reviewing the user's code, point out bugs and explain fixes in conversation;
don't edit their script files directly unless they explicitly ask for that.

## Game concept (as described by the user)

- A grid **board** of jellies in several colors that need to be destroyed.
- A **moving platform ("treadmill")** wraps around the perimeter of the board and
  carries a **shooter jelly** along it. The treadmill's movement is **automatic**
  (not player-controlled) — it continuously travels the loop.
- Shooting is also **automatic**: whenever the shooter's current position on the
  treadmill puts a board jelly of **its own color** in a straight **line of
  sight** (no other jelly blocking the line), it fires.
- **Bullets** travel in a straight line from the shooter to the target jelly.
- **Player's core interaction**: choosing which color jelly boards the treadmill
  **next** (i.e. picking the next shooter's color from an upcoming queue). The
  player does not move the shooter or trigger shots directly — only feeds the
  queue.

This is the working understanding — update this section if the user corrects or
refines the mechanics.

## Project status

- **2026-10-03**: Fresh Unity project scaffold existed. No gameplay code written yet.
- **2026-10-03**: Generated a first pass of placeholder sprites into `Assets/Sprites/`.
- **2026-10-03**: Treadmill movement built and working, using `SurfaceEffector2D`
  (not translating the treadmill's own transform). Static layout (Board +
  Treadmill) saved as `Assets/Prefabs/Layout.prefab`.
- **2026-10-03**: Five `ShooterJelly_<color>` prefabs created, placed around the
  loop, shooter-vs-shooter collision disabled via per-collider Exclude Layers.
- **2026-10-03**: Decided **not to use `shooter_platform.png`** at all — shooters
  are just the plain colored jelly sprites, no chassis/cannon visual. Four of the
  five shooter prefabs (red/blue/yellow/purple) were deleted, keeping only
  `ShooterJelly_green.prefab` as the one being actively worked on — the plan is
  to finish it, then duplicate+reskin per color once it's right.
- **2026-10-03**: First gameplay scripts written: `JellyColor.cs`,
  `JellyController.cs`, `BulletController.cs` (see "Scripts" below). Bullets
  spawn and travel in a straight line toward the board; direction is computed
  dynamically from which edge of the board the shooter is nearest to. **Two
  known bugs are still unfixed** in the current files — see "Known bugs /
  pending fixes" below.

## Tech stack

- Unity **6000.4.6f1** (Unity 6), Universal Render Pipeline, 2D URP project
  template (`Renderer2D.asset`, `Lit2DSceneTemplate`).
- New Input System package is installed (`InputSystem_Actions.inputactions`
  exists from the default project template) but **nothing uses it yet** — no
  input is wired to anything. The whole game so far is non-interactive
  (everything moves/fires automatically).
- Physics: `Rigidbody2D`/`Collider2D` (2D physics), no 3D physics in use.

## Scripts (`Assets/Scripts/`)

**`JellyColor.cs`** — plain `enum JellyColor { Red, Green, Blue, Yellow, Purple }`.
Shared color identity, meant to be used both by shooters and (eventually) board
jellies for match checks. Note it's a plain enum, not a `Component` — it can't be
fetched with `GetComponent<JellyColor>()`.

**`JellyController.cs`** — lives on the shooter jelly. Responsibilities:
- Holds its own `jellyColor`, a reference to the `BulletController` prefab to
  spawn (currently wired to `Bullet_green`), `fireInterval`, and a `boardCenter`
  reference (currently points at the `Board` transform).
- `GetAimDirection()` — computes which way to fire by comparing the shooter's
  position to `boardCenter`: whichever axis (x or y) has the larger offset
  determines the edge it's on, and direction is snapped to the cardinal inward
  direction for that edge (not a smooth toward-center vector). This is
  recalculated fresh on every shot rather than cached, since the shooter keeps
  moving around the whole loop — caching it would leave it firing a stale
  direction as it travels.
- `Fire()` — instantiates the bullet prefab at the shooter's position and calls
  `bullet.Init(direction, jellyColor)`.
- There's also a leftover `aimDirection` serialized field (`Vector2`, defaults
  to `(0,1)`) from an earlier hardcoded-direction version — it's no longer read
  by `Fire()` (which uses `GetAimDirection()` instead), so it's dead/vestigial
  at this point. Fine to delete whenever.

**`BulletController.cs`** — lives on the bullet. `Init(direction, color)` is
called once by the shooter right after `Instantiate`; it sets the bullet's own
`jellyColor`, points its rotation to face `direction`, and sets
`rb.linearVelocity = direction * speed` so Unity's physics engine carries it
forward every `FixedUpdate` with no per-frame movement code needed. The bullet
has no lifetime/cleanup logic yet (see known bugs).

## Known bugs / pending fixes

As of 2026-10-03, these are real, diagnosed-but-not-yet-fixed issues in the
current files:

1. **`BulletController.Init` rotation is broken.**
   ```csharp
   transform.rotation = new Quaternion(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg, 0);
   ```
   `new Quaternion(x, y, z, w)` takes raw quaternion components, **not** Euler
   degrees — putting an angle value into the `z` slot with `w = 0` produces an
   invalid/non-unit quaternion, not the intended rotation. Fix is
   `Quaternion.Euler(0f, 0f, angle)`. Also, once that's fixed, the angle itself
   needs a `-90f` correction, because `Mathf.Atan2` assumes 0° points along
   `+X`, but the bullet sprites were drawn pointing `+Y` (up) by default — so
   `angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f`.

2. **Fire rate doesn't respond to live changes to `fireInterval`.**
   `JellyController.Start()` currently does
   `InvokeRepeating(nameof(Fire), 0f, fireInterval)`, which reads `fireInterval`
   **once** at that call and bakes it into Unity's internal schedule — changing
   the field afterward (even in the Inspector during Play mode) has no effect.
   Fix is to drop `InvokeRepeating` and drive firing from a manually-tracked
   timer in `Update()` that re-reads `fireInterval` every frame (accumulate
   `Time.deltaTime`, fire and reset when it crosses the threshold). This also
   sets up the right place to later add the "only fire on line-of-sight match"
   condition.

3. **Bullets are never destroyed.** No despawn-on-hit or despawn-out-of-bounds
   logic exists yet — every bullet fired lives forever. Not urgent while testing
   in short bursts, but will need addressing before this is left running for
   any length of time.

4. **`JellyController` is attached to the scene instance, not the prefab.**
   `ShooterJelly_green.prefab`'s saved asset has no scripts on it — the
   `JellyController` component (and its field values: `jellyColor: Green`,
   `bulletController: Bullet_green`, `fireInterval: 1`, `boardCenter: Board`)
   currently only exist as an override on the one instance sitting in
   `SampleScene.unity`. A fresh drag-in of the prefab would come up with no
   shooting behavior at all. Worth applying back onto the prefab
   (Overrides → Apply All) once the two bugs above are fixed, so the prefab
   itself is the complete, working thing to duplicate per color.

5. **Two unlabeled trigger `BoxCollider2D`s were added directly onto the
   Treadmill in the scene** (not saved into `Layout.prefab` — scene-instance
   override only). Small (~0.74 × 0.09), both `Is Trigger`, sitting near where
   the shooter currently is. Purpose isn't obvious from reading the files —
   worth checking in the Editor whether these are intentional (e.g. an
   in-progress spawn-point or line-of-sight experiment) or leftover clutter
   from testing, since right now they're silent, undocumented, and not
   referenced by any script.

## Current scene & prefab contents

As of 2026-10-03, reflecting what's actually in `Assets/Scenes/SampleScene.unity`
and the prefabs in `Assets/Prefabs/` (check these files directly if this drifts
out of date — this is a snapshot, not a substitute for reading the scene/prefab).

**`Layout.prefab`** — the static board+treadmill layout:
- `Layout` (empty root transform, just a positioning anchor)
  - `Board` — `SpriteRenderer` only (`board_background.png`). No physics; purely
    visual background. The actual jelly grid/data model doesn't exist yet.
  - `Treadmill` — `SpriteRenderer` (`treadmill_track.png`) plus the conveyor
    physics setup:
    - `Rigidbody2D` — **Kinematic**. Never translated; only exists so the
      colliders below can host effector/physics behavior.
    - `SurfaceEffector2D` — `Speed: -2` (negative is what produced clockwise
      motion matching the drawn arrows), `Force Scale: 0.1`, `Use Friction` +
      `Use Bounce` on, `Use Contact Force` off.
    - Three `EdgeCollider2D`s: the **effector path** (centerline loop, `Used By
      Effector` ✓ — this is what actually pushes things), an **outer guide
      wall**, and an **inner guide wall** (both plain, `Used By Effector` ✗).
    - **Gotcha**: a shooter only gets pushed if it's actually touching/
      overlapping the effector-path collider — sitting between the guide walls
      isn't enough on its own. Matters for wherever shooters get spawned
      programmatically later.

**`Assets/Prefabs/ShooterJelly_green.prefab`** — the one surviving shooter
prefab (the other four colors were deleted; plan is to perfect this one, then
duplicate+reskin per color). As saved: `SpriteRenderer` (`jelly_green.png`),
`Rigidbody2D` (Dynamic, Gravity Scale 0, rotation frozen, linear damping 0),
`CircleCollider2D` (radius 0.4, not a trigger, Exclude Layers = `ShooterJelly`
so it ignores other shooters). **`JellyController` is not saved onto this
prefab** — see known bugs #4 above.

- Shooter-vs-shooter collision is off via a *per-collider* layer filter, not the
  global matrix: a custom Physics2D layer `ShooterJelly` (index 3) was added,
  each shooter is placed on it, and each `CircleCollider2D`'s **Exclude Layers**
  is set to `ShooterJelly`. The project-wide Physics 2D → Layer Collision
  Matrix is untouched. This is also what keeps queue order stable — nothing can
  shove one shooter past another.

**`Assets/Prefabs/Bullet_green.prefab`** — `SpriteRenderer` (`bullet_green.png`),
`Rigidbody2D` (Dynamic, Gravity Scale 0), `CapsuleCollider2D` (**Is Trigger**),
`BulletController` (`speed: 10`, `jellyColor: Green`). No lifetime/cleanup yet
(known bug #3).

**`SampleScene.unity`** — three root objects:
- `Main Camera` — default orthographic URP 2D camera (size `5`, solid
  background color).
- `Layout` — one instance of `Layout.prefab`. Has two added (scene-only)
  trigger `BoxCollider2D`s on the Treadmill — see known bug #5.
- `Shooters` — a plain empty container GameObject (not a prefab), positioned at
  the treadmill's world origin. Currently holds **one** child: the
  `ShooterJelly_green` instance, with `JellyController` added as a
  scene-instance override (fields: `jellyColor: Green`,
  `bulletController: Bullet_green`, `fireInterval: 1`, `boardCenter: Board`).

## Assets/Sprites/ — sprite set v1

Generated with a Python + Pillow script (rasterized PNG, not hand-drawn/AI-image-gen)
so shapes are deterministic and easy to re-tune by editing parameters.

| File | Purpose | Size |
|---|---|---|
| `jelly_red.png`, `jelly_blue.png`, `jelly_green.png`, `jelly_yellow.png`, `jelly_purple.png` | Destroyable board jellies, also used as the shooter's own visual | 128×128 |
| `bullet_red.png` ... `bullet_purple.png` | Projectile fired by the shooter, matching jelly colors, drawn pointing up by default | 48×72 |
| `board_background.png` | 8×8 game board panel with checker grid cells | 720×720 |
| `treadmill_track.png` | Square looping track/rail that wraps around the board with directional chevrons | 900×900 |
| `shooter_platform.png` | A chassis/cannon sprite generated early on — **decided against using it**, not wired in anywhere, can be ignored/deleted | 160×200 |
| `scene_preview.png` | Composite mockup for reference only, not a real sprite | 900×900 |

`board_background.png`, `treadmill_track.png`, all five `jelly_<color>.png`, and
`bullet_green.png` are now in use in the scene. The other four bullet colors and
`shooter_platform.png` are unused.

## Next steps (not started — waiting on user direction)

- Fix the two known bugs above (rotation, fire-rate), then apply `JellyController`
  back onto `ShooterJelly_green.prefab` so the prefab is self-contained.
- Figure out/clean up the two unlabeled trigger colliders on the Treadmill instance.
- Add bullet lifetime: despawn on hitting something or leaving the board bounds.
- Once the single shooter is working end-to-end, duplicate+reskin it (and a
  matching bullet prefab) for the other four colors.
- Board data model + jelly grid placement (the board is currently just a
  background image — no actual grid of jelly GameObjects exists yet). This is
  also a prerequisite for the line-of-sight/same-color auto-fire condition,
  since there's nothing for a raycast to hit yet.
- Line-of-sight detection + same-color match rule — gate `Fire()` on an actual
  check instead of firing unconditionally on a timer.
- The player-facing color queue: UI/logic for picking which color boards the
  treadmill next.
