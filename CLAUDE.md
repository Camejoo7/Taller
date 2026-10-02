# CLAUDE.md

## Contexto del Proyecto

**CodeBreak** es un videojuego educativo para enseñar Python a estudiantes de noveno año liceal. Desarrollado como proyecto de Taller — Profesorado de Informática.

### Historia
Año 2077, ciudad Central City. La corporación NEXCORP controla todo el conocimiento de programación a nivel global. El jugador es un ex-analista de sistemas que descubre Python oculto en los servidores y libera a KIRA, una IA rebelde que se instala en su implante neural. Juntos huyen y aprenden Python para enfrentarse a NEXCORP.

### Personajes
- **NX-7** — El jugador. Ex-analista, pragmático, silencioso.
- **KIRA** — IA rebelde, 51 años encadenada en NEXCORP. Sarcástica, humorística, brillante. Guía educativa del juego.

### Estructura del juego (cambiada el 02/10/2026)

**No son seis stages.** La idea original era un nivel por concepto; Kevin la cambió por:

- **Stage 1** (`SampleScene`) — la intro: historia, movimiento, el dron kamikaze, la salida. Sin contenido de Python todavía.
- **Stage 2** (`Stage2`) — **el juego**: un mapa grande que se va extendiendo, con **muchas terminales repartidas** que entre todas cubren toda la currícula (print, variables y tipos, condicionales, bucles, listas y cadenas, funciones).

Las stages 3 a 6 **están canceladas**. Si en alguna sesión aparece la idea de "armar la Stage 4", es desorientación: ese contenido ahora es una zona más del Stage 2.

### Mecánica educativa: la terminal

El jugador se acerca a una consola de NEXCORP, aprieta **E**, y **escribe con el teclado de verdad** la parte que falta de una línea de Python. Al acertar, la línea se ejecuta, imprime su salida y **pasa algo en el mundo**: se abre una puerta blindada, se enciende un sector que estaba sin energía.

Piezas: `TerminalChallenge` (ScriptableObject con la consigna, el hueco, las respuestas aceptadas y **los errores previstos con su traceback real de Python y la explicación de Kira**), `CodeTerminal`, `CodeTerminalUI`, `PoweredDoor` y `PowerCurtain`. Agregar un ejercicio nuevo es crear un `.asset` y poner una consola — no se programa nada.

**La regla de diseño que manda acá:** lo que hace que sea un juego no es cómo se responde, es **qué pasa en el mundo cuando acertás**. Un cartel de "¡Correcto!" se hace en cualquier página web; una puerta blindada que tiembla y se abre mientras el pasillo recupera la luz, no. Toda terminal nueva necesita su consecuencia física.

> El **"Code Blaster"** (dispararle al fragmento correcto entre varios flotando) fue la mecánica anterior y está **congelada**: Kevin la rechazó por sentirse "un múltiple opción que podría hacer en cualquier HTML". Los scripts siguen en el proyecto sin usarse. Ver `ROADMAP.md`.

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**Taller** is a Unity 6 2D platformer/action game using URP (Universal Render Pipeline 17.4.0). The narrative centers on a protagonist who encounters Kira, an AI drone that escaped NexCorp's servers after 51 years hidden in their systems.

El juego ya se puede jugar de punta a punta: Menú Principal → `SampleScene` (Stage 1: diálogo de intro con Kira, dron kamikaze, corazones, salida) → `Stage2` (el mapa grande, con el arma, drones de patrulla, la terminal de código y la puerta blindada). Lo que falta es **ancho de mapa y contenido**, no sistemas.

## Running and Building

This is a Unity project — all building, running, and testing must be done through the **Unity Editor** (open `TallerV3` as a Unity project). There is no CLI build script. Tests use Unity's Test Framework (`com.unity.test-framework` 1.6.0) and run via the Unity Test Runner window.

**Scene order (Build Settings):**
1. `Assets/Scenes/MainMenu.unity` — title screen
2. `Assets/Scenes/SampleScene.unity` — Stage 1 gameplay

## Roadmap y Workflow con IA

Este proyecto se desarrolla con Claude Code conectado al Editor vía MCP (`com.coplaydev.unity-mcp`, pineado a **v10.0.0** — no seguir `#main`). Grupos de herramientas activos: `core`, `animation`, `scripting_ext`, `testing`, `docs`. `probuilder`, `vfx`, `ui` (UI Toolkit) y `asset_gen` están desactivados a propósito: este proyecto usa Canvas/uGUI clásico, no UI Toolkit, y los sprites vienen de asset packs de itch.io, no de generación por IA.

**El plan completo de desarrollo, fase por fase, está en `ROADMAP.md` en la raíz del proyecto, con checkboxes por ítem.** Reglas para no perder la orientación entre sesiones:

- Al arrancar cualquier sesión nueva, leer `ROADMAP.md` y decirle a Kevin en qué fase está parado y qué ítems faltan de esa fase, antes de proponer una tarea.
- Al terminar un ítem, marcarlo `[x]` en `ROADMAP.md` en el mismo momento — no dejarlo para después.
- Si Kevin pide algo de una fase posterior mientras quedan ítems sin marcar en la fase actual, avisarle explícitamente ("esto es de la Fase X, todavía faltan N ítems de la Fase actual") antes de proceder. No bloquearlo si igual quiere seguir, pero que sea una decisión consciente, no un salto por desorientación.

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

El contenido **ya no está hardcodeado**: sale de un `StageData` (ScriptableObject), con `enum DialogueSegment { Intro, Mid, Outro }` → `introDialogue` / `midDialogue` / `outroDialogue`. Hay uno por escena: `Assets/Data/Stage1.asset` y `Assets/Data/Stage2.asset`.

Intro congela al jugador, anima al dron subiendo, avance manual y handoff a `KiraFollower`. Mid/Outro no congelan y son solo auto-avance.

> `DialogueManager2` **ya no existe**: se fusionó en `DialogueManager` y se borró.

Inspector fields: `dialogueCanvas`, `speakerNameText`, `dialogueText`, `kiraAvatar`, `kiraDrone`, `kiraSpawnBelow`, `kiraTargetPos`.

### Kira Trigger Zone
`Assets/Scripts/KiraTriggerZone.cs` — A one-shot `OnTriggerEnter2D` that fires `DialogueManager.StartDialogue()` when the player enters. Holds a `private bool triggered` to prevent re-firing.

### Kira Follower
`Assets/Scripts/KiraFollower.cs` — Activated by `DialogueManager.EndDialogue()`, o desde el arranque si `followFromStart` está en true (Stage 2).

Reescrito el 02/10/2026 porque Kira "parecía un gif pegado en un punto de la pantalla". Lo que la hace parecer viva es **llegar tarde**:

- `Vector3.SmoothDamp` (no `MoveTowards`): acelera, se queda atrás, sobrepasa y se asienta. `smoothTime` 0,38.
- **Se pone siempre del lado contrario al que mira el jugador** y cruza con un arco por arriba (`crossLift`) cuando él se da vuelta. El offset ya no es fijo.
- Se inclina hacia donde va (`bankAngle` 16°). **Ojo:** como el flip es `localScale.x = ±1`, el ángulo se niega al mirar a la izquierda, o se tumba al revés.
- Deriva con dos senos de frecuencias no múltiplos (0,83 y 1,17) para que el ciclo no se lea como un loop.
- `followSpeed` ahora es el **techo** de velocidad del SmoothDamp, no la velocidad de seguimiento. Tiene que ser mayor que la del jugador (8 contra 3).

### Fall Respawn
`Assets/Scripts/FallRespawn.cs` — Trigger zone placed below the level. When the player falls into it, teleports them to `respawnPoint` and zeroes `rb.linearVelocity`.

### Enemy System
- `Assets/Scripts/EnemySpwaner.cs` (**filename typo** — class is `EnemySpawner`) — one-shot `OnTriggerEnter2D`; instantiates `enemyPrefab` at the spawner's position when the player enters.
- `Assets/Enemy/EnemyIA.cs` (**filename** — class is `EnemyAI`) — Chases player in `FixedUpdate` via `Rigidbody2D.MovePosition`. Stops at `stopDistance`; when stopped sets `rb.linearVelocity = Vector2.zero`. Flips via `transform.localScale`. Drives `animator.SetBool("isMoving", ...)`.

- `Assets/Scripts/Combat/DroneEnemy.cs` — el dron de patrulla del Stage 2. Se acerca, **avisa** (se frena y parpadea en rojo, `telegraphColor`), embiste en línea recta y se aleja a dar un respiro. El aviso y el respiro son lo que lo hace esquivable: ver los números en `ROADMAP.md` antes de tocarlos.
- `Assets/Scripts/Combat/KamikazeDrone.cs` — el del Stage 1: espera flotando, se acomoda a la altura del jugador y sale derecho. Explota contra lo que toque.

> El `EnemySpawner` / `EnemyAI` viejos siguen en el proyecto pero el Stage 1 ya usa `KamikazeDrone`. El `CodeBlasterTarget` que iba a extenderlos **quedó congelado** con el resto del Code Blaster.

### Terminal (la mecánica educativa)
Todo en `Assets/Scripts/Terminal/`:

- `TerminalChallenge.cs` — ScriptableObject: la consigna, la línea partida en `codeBefore` / hueco / `codeAfter`, las respuestas aceptadas y **los errores previstos con su traceback real de Python y la explicación de Kira**. Esa lista es la mitad del valor educativo. Los assets viven en `Assets/Data/Terminal/`.
- `CodeTerminal.cs` — proximidad, tecleo (vía `Keyboard.current.onTextInput`), validación, puntaje, y el `UnityEvent onSolved` donde se engancha la consecuencia física.
- `CodeTerminalUI.cs` — la pantalla. Se arma sola en runtime; el monitor es un sprite del pack Consola y el recorte del vidrio son campos del Inspector.
- `PoweredDoor.cs` — la puerta que se abre al resolver.
- `PowerCurtain.cs` — el sector sin energía del otro lado de la puerta; al resolver, la luz entra barriendo.
- `InteractPrompt.cs` — el cartelito de "E": dibuja una tecla que entra con rebote, flota y late. Se prende y apaga con `SetActive`.
- `SpriteFlipbook.cs` — cicla frames en un `SpriteRenderer`, para los objetos animados de los packs sin montar un Animator.

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

### Orden de dibujo (sorting)
Las capas del proyecto son `Fondo` (-1), `Default` (0) y `Personaje` (1). Los **tilemaps del nivel están en `Default`**, así que cualquier cosa en `Personaje` les queda por delante sin importar el orden. La regla, a partir del bug del jugador tapado por la consola:

| Qué | Capa | Orden |
|---|---|---|
| Props del mundo (consolas, puertas, drones) | `Personaje` | **0 o menos** |
| El jugador (`Player/Visual`) | `Personaje` | **10** |
| Carteles de interfaz en el mundo (el prompt de "E") | `Personaje` | **20+** |

### Números del jugador — leer el prefab, no el script
`PlayerMovement.cs` tiene `speed = 5` y `jumpForce = 10` como valores por defecto, pero **el `Player.prefab` serializa 3 y 6,7**, y el serializado es el que manda. Balancear enemigos contra los números del script da resultados mal. Con `gravityScale` 3, `jumpForce` 6,7 da un salto de ~0,76 de alto.

El collider del jugador usa `Assets/Physics/Jugador.physicsMaterial2D` con **fricción 0**. No se la subas: con fricción, apretar contra una pared mientras saltás mata la velocidad vertical y el personaje se queda pegado. Es seguro tenerla en 0 porque el movimiento setea la velocidad a mano.

El `Rigidbody2D` va en `NeverSleep` (se fuerza en `Start()`): dormido deja de emitir `OnCollisionStay2D` y la detección de piso se queda sin contactos.

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
