# CLAUDE.md

## Contexto del Proyecto

**CodeBreak** es un videojuego educativo para enseñar Python a estudiantes de noveno año liceal. Desarrollado como proyecto de Taller — Profesorado de Informática.

### Historia
Año 2077, ciudad Central City. La corporación NEXCORP controla todo el conocimiento de programación a nivel global. El jugador es un ex-analista de sistemas que descubre Python oculto en los servidores y libera a KIRA, una IA rebelde que se instala en su implante neural. Juntos huyen y aprenden Python para enfrentarse a NEXCORP.

### Personajes
- **NX-7** — El jugador. Ex-analista, pragmático, silencioso.
- **KIRA** — IA rebelde, 51 años encadenada en NEXCORP. Sarcástica, humorística, brillante. Guía educativa del juego.

### Stages planeados
- Stage 1: Información general de Python (EN DESARROLLO)
- Stage 2: Print, Variables y Tipos
- Stage 3: Condicionales
- Stage 4: Bucles
- Stage 5: Listas y Cadenas
- Stage 6: Funciones

### Mecánicas educativas
Cada stage tiene diálogos de Kira que explican conceptos de Python, y encuentros de combate "Code Blaster" donde el jugador dispara a la respuesta correcta entre varios fragmentos de código flotando — el combate y la verificación de aprendizaje son la misma acción. Ver `ROADMAP.md` para el diseño completo de esta mecánica.

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**Taller** is a Unity 6 2D platformer/action game using URP (Universal Render Pipeline 17.4.0). The narrative centers on a protagonist who encounters Kira, an AI drone that escaped NexCorp's servers after 51 years hidden in their systems. Stage 1 is built: the game starts at a Main Menu with an animated scrolling background, transitions into the gameplay scene (`SampleScene`), triggers an introductory dialogue with Kira, and then drops the player into a level with enemy spawners.

## Running and Building

This is a Unity project — all building, running, and testing must be done through the **Unity Editor** (open `TallerV3` as a Unity project). There is no CLI build script. Tests use Unity's Test Framework (`com.unity.test-framework` 1.6.0) and run via the Unity Test Runner window.

**Scene order (Build Settings):**
1. `Assets/Scenes/MainMenu.unity` — title screen
2. `Assets/Scenes/SampleScene.unity` — Stage 1 gameplay

## Roadmap y Workflow con IA

Este proyecto se desarrolla con Claude Code conectado al Editor vía MCP (`com.coplaydev.unity-mcp`, pineado a **v10.0.0** — no seguir `#main`). Grupos de herramientas activos: `core`, `animation`, `scripting_ext`, `testing`, `docs`. `probuilder`, `vfx`, `ui` (UI Toolkit) y `asset_gen` están desactivados a propósito: este proyecto usa Canvas/uGUI clásico, no UI Toolkit, y los sprites vienen de asset packs de itch.io, no de generación por IA.

**El plan completo de desarrollo, fase por fase, está en `ROADMAP.md` en la raíz del proyecto.** Antes de arrancar una tarea grande, consultarlo para saber en qué fase se está trabajando.

### Flujo obligatorio para cualquier cambio de código o escena

Ninguna tarea se considera terminada hasta completar este ciclo:

1. Editar o crear el script.
2. Llamar a `refresh_unity` para compilar.
3. Llamar a `read_console` y revisar si hay errores.
4. Si hay errores: corregirlos y volver al paso 2. No avanzar con errores pendientes.
5. Asignar referencias del Inspector (`manage_components`, `manage_gameobject`) directamente por herramientas MCP — nunca pedirle a Kevin que arrastre algo a mano, salvo que lo pida explícitamente.
6. Nunca editar archivos `.unity`, `.prefab` o `.meta` directamente como texto. Usar siempre las herramientas MCP (`manage_scene`, `manage_prefabs`, etc.).

### Seguridad

- Git está inicializado en este proyecto (o debería estarlo — ver Fase 0 del roadmap). Antes de cualquier operación grande o experimental (refactor de sistemas, cambios de escena masivos), sugerir un commit de checkpoint primero.
- `execute_code` no es un sandbox completo — evitar operaciones destructivas de archivos o de sistema aunque el código parezca inofensivo.

## Architecture

### Player
`Assets/Scripts/Player/PlayerMovement.cs` — Uses the **new Input System** (`UnityEngine.InputSystem`). Reads `Keyboard.current` directly (no `InputAction` assets).

Key details:
- The player GameObject has a child named `"Visual"` that holds the Animator and sprite. `PlayerMovement` manipulates `Visual.localScale` for directional flipping — **not** `SpriteRenderer.flipX`.
- Scale when moving **right**: `(-2, 2, 1)`. Scale when moving **left**: `(2, 2, 1)`. The base scale is ±2, not ±1.
- `LateUpdate()` pins `visual.localPosition = Vector3.zero` every frame to prevent physics drift.
- Movement uses `rb.linearVelocity` (Unity 6 API — `rb.velocity` is deprecated and will not compile).
- `canMove` is the public interface for freezing the player. `SetCanMove(false)` also zeroes velocity immediately. Dialogue calls this.
- Ground detection uses `OnCollisionEnter2D` / `OnCollisionStay2D` / `OnCollisionExit2D` by inspecting contact normals (`normal.y > 0.5f`).
- Animator bools: `"isWalking"` (horizontal movement) and `"isJumping"` (airborne state).

### Dialogue System
`Assets/Scripts/DialogueManager.cs` — Triggered once by `KiraTriggerZone`. Full sequence:
1. `StartDialogue(player)` freezes the player via `SetCanMove(false)` and activates the dialogue canvas.
2. Kira's drone animates up from `kiraSpawnBelow` to `kiraTargetPos` over 1.5 s using `SmoothStep`.
3. A `KiraFloat()` coroutine keeps Kira bobbing sinusoidally while dialogue is active.
4. Lines display via a typewriter coroutine (`typingSpeed = 0.03s/char`).
5. **Manual advance**: Space or Enter — if still typing, skips to full line; otherwise advances.
6. **Auto-advance**: `autoAdvanceTime = 3f` seconds after typing finishes — implemented as a float timer in `Update()`, not a coroutine.
7. On end: re-enables player movement and calls `KiraFollower.StartFollowing(playerMovement.transform)`.

Dialogue content is hardcoded in `DialogueManager.Awake()` — 5 lines alternating between `"KIRA"` (cyan) and `"..."` (yellow) speakers.

> **Nota (Fase 0 del roadmap):** este manager y `DialogueManager2` se están fusionando en un único `DialogueManager` que lee el contenido desde un `StageData` (ScriptableObject) en vez de tenerlo hardcodeado. Ver `ROADMAP.md`.

Inspector fields: `dialogueCanvas`, `speakerNameText`, `dialogueText`, `kiraAvatar`, `kiraDrone`, `kiraSpawnBelow`, `kiraTargetPos`.

### Dialogue System 2
`Assets/Scripts/DialogueManager2.cs` — Lighter dialogue manager for mid-gameplay use (no player freeze, no Kira drone animation). 4 hardcoded lines in `Awake()`: all spoken by `"KIRA"` (cyan) except one `"..."` (yellow) line for NX-7. Auto-advance only (`autoAdvanceTime = 4f`); no manual skip. `StartDialogue()` takes no arguments and just activates the canvas and starts typing.

### Kira Trigger Zone
`Assets/Scripts/KiraTriggerZone.cs` — A one-shot `OnTriggerEnter2D` that fires `DialogueManager.StartDialogue()` when the player enters. Holds a `private bool triggered` to prevent re-firing.

### Kira Follower
`Assets/Scripts/KiraFollower.cs` — Activated by `DialogueManager.EndDialogue()`. Makes Kira trail the player using `Vector3.MoveTowards` in `Update`.

- Default offset: `(-2, 1.5)` relative to the player.
- Adds a sinusoidal Y bob: `Mathf.Sin(floatTimer * 1.5f) * 0.15f`.
- Sets `animator.SetBool("isMoving", distance > 0.1f)`.
- Flips its own `localScale.x` to face the player.

### Fall Respawn
`Assets/Scripts/FallRespawn.cs` — Trigger zone placed below the level. When the player falls into it, teleports them to `respawnPoint` and zeroes `rb.linearVelocity`.

### Enemy System
- `Assets/Scripts/EnemySpwaner.cs` (**filename typo** — class is `EnemySpawner`) — one-shot `OnTriggerEnter2D`; instantiates `enemyPrefab` at the spawner's position when the player enters.
- `Assets/Enemy/EnemyIA.cs` (**filename** — class is `EnemyAI`) — Chases player in `FixedUpdate` via `Rigidbody2D.MovePosition`. Stops at `stopDistance`; when stopped sets `rb.linearVelocity = Vector2.zero`. Flips via `transform.localScale`. Drives `animator.SetBool("isMoving", ...)`.

> **Nota (Fase 0 del roadmap):** este sistema se extiende con un componente `CodeBlasterTarget` para la mecánica de combate educativo. Ver `ROADMAP.md`.

### Camera
`Assets/Scripts/CameraFollow.cs` — `LateUpdate` smooth-follow using `Vector3.Lerp(current, target, smoothSpeed * Time.deltaTime)`. Always preserves `transform.position.z` — never overwrite the Z axis.

### Menu & UI
- `Assets/Scripts/ScriptMenu/MainMenu.cs` — `Jugar()` loads `"SampleScene"` via `SceneManager.LoadScene`; `Salir()` calls `Application.Quit()` with an Editor fallback.
- `Assets/Scripts/ScriptMenu/AutoScrollUI.cs` — Animates a `RawImage` UV rect by incrementing `uvRect.x` each frame (`velocidad = 0.1f`). Attach to the background `RawImage` in the Main Menu canvas.
- `Assets/Scripts/ScriptMenu/SceneTransition.cs` — Fade in on `Start`, fade out then load on `CargarEscena(sceneName)`. Uses a full-screen black `Image panelFade`. Duration is `duracion = 1f` seconds.

## Key Conventions

- **Input**: Always use `UnityEngine.InputSystem` (new Input System). Read `Keyboard.current.*` directly. Never use legacy `Input.GetKey` / `Input.GetAxis`.
- **Velocity**: Use `rb.linearVelocity` — the Unity 6 API. `rb.velocity` is deprecated and will cause a compile error.
- **Sprite direction**: Flip via `transform.localScale` (x = ±1 for most objects, ±2 for the player `Visual`). Never use `SpriteRenderer.flipX`.
- **Player detection**: Use `other.CompareTag("Player")` in trigger callbacks. Use `GameObject.FindWithTag("Player")` when finding at `Start`.
- **One-shot triggers**: Guard with `private bool triggered = false` (or `spawned`). Set to `true` immediately on first entry.
- **Coroutines**: Stop by name string (`StopCoroutine("TypeLine")`) when re-running a coroutine that may already be running.
- **Packages**: Sprites can be authored in Aseprite (`com.unity.2d.aseprite` 4.0.1). The 2D Animation and PSD Importer packages are also installed.

## Known File Naming Issues

| File | Actual Class |
|------|-------------|
| `Assets/Scripts/EnemySpwaner.cs` | `EnemySpawner` (typo in filename) |
| `Assets/Enemy/EnemyIA.cs` | `EnemyAI` (filename uses Spanish initialism) |

Do not rename these files without also updating all scene/prefab references in the Unity Editor.

## Package Reference

Key packages (`Packages/manifest.json`):
- `com.unity.inputsystem` 1.19.0
- `com.unity.render-pipelines.universal` 17.4.0
- `com.unity.2d.aseprite` 4.0.1
- `com.unity.2d.animation` 14.0.3
- `com.unity.2d.tilemap.extras` 7.0.1
- `com.unity.ugui` 2.0.0 (TextMesh Pro included)
- `com.unity.test-framework` 1.6.0
- `com.coplaydev.unity-mcp` (git, pineado a `#v10.0.0` — MCP server for Unity Editor integration. Grupos activos: core, animation, scripting_ext, testing, docs)
