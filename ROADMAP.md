# CodeBreak — Roadmap de Desarrollo

> Generado a partir de una sesión de planificación con Kevin. Actualizalo a medida que el proyecto avanza — es un mapa, no una ley. Si algo cambia, editá este archivo y contáselo a Claude Code al arrancar la sesión siguiente.

## 📍 Dónde estamos ahora

**Fase actual: Fase 2 — Pasada horizontal** (Fase 0 cerrada; Stage 2 arrancado)

> La Fase 1 (Stage 1 de punta a punta) quedó pendiente a propósito: Kevin armó primero el mapa del Stage 2 y ahí se probó el Code Blaster. El molde ya existe y funciona, así que volver al Stage 1 es ahora trabajo de contenido, no de sistemas.

Antes de aceptar o proponer cualquier tarea nueva, mirá la lista de la fase actual más abajo. Si en algún momento Claude Code (o vos mismo) propone algo de una fase más adelante mientras todavía quedan ítems sin marcar en la fase actual, es una señal de alerta — no está prohibido saltar el orden, pero hacerlo a propósito y no por perderse.

## Decisiones tomadas (para no repetir la charla)

| Decisión | Resultado |
|---|---|
| Plazo | 2 meses reales, con meta interna de avanzar fuerte las primeras 3-4 semanas |
| Densidad por stage | Media (10-20 min), flexible — algunos pueden ser más cortos si cumplen el objetivo educativo |
| Prioridad de trabajo | Horizontal primero: avanzar un poco en las 6 stages, después pulir a fondo |
| Relación enemigos-aprendizaje | Mezclada, vía la mecánica "Code Blaster" (ver abajo), con fallback simple si se complica |
| Combate | Con disparo / proyectiles |
| Audio | Ambiente + efectos, pero al final — baja prioridad |
| Progresión | Selector de niveles lineal-gateado (no se puede saltar sin completar el anterior). Sin guardado de sesión |
| Plataforma | Build standalone de Windows, PC/laptop únicamente. Nada de web |

## La mecánica central: Code Blaster — ⚠️ CONGELADA (02/10/2026)

> Kevin la rechazó. Lo de abajo queda como registro de lo que se construyó y aprendió, **no como el plan vigente**. Ver "La mecánica educativa está en revisión" más abajo.

Un dron o enemigo aparece en pantalla y muestra 3-4 fragmentos de código flotando a su alrededor, como si fueran las balas que vienen hacia el jugador. Kira hace una pregunta sobre el concepto de la stage, y el jugador le dispara al fragmento que la responde correctamente. Acertar destruye al enemigo. Errar le hace daño al jugador o el fragmento rebota.

**Por qué funciona para este proyecto:** reutiliza el `EnemySpawner` / `EnemyAI` que ya existen, no requiere evaluar código Python real (eso sería construir un intérprete, un proyecto en sí mismo), y el combate ES la verificación de aprendizaje — no son dos sistemas separados compitiendo por el tiempo de desarrollo.

**Cómo quedó implementada (restricciones de escala que costaron encontrar):** el juego es muy chico — el jugador mide 0,38 unidades y la cámara muestra 3,6 de alto. A ese tamaño un dron con fragmentos alrededor no entra en pantalla. Por eso: (1) durante la pelea la cámara **se aleja** a `orthographicSize` 2,6 y **encuadra el punto medio** entre el jugador y el dron, si no el cartel de la pregunta tapa los fragmentos de arriba; (2) la órbita es **ovalada** (`fragmentOrbitVerticalScale`, 0,45 por defecto) porque la pantalla es apaisada; (3) el `fontSize` de TextMeshPro en el mundo **no son unidades** — cada letra ocupa ~0,1 × fontSize, así que los fragmentos usan tamaños cerca de 1,2 y no de 0,15. Valores que se sienten bien hoy: bala a 14 u/s, órbita a 14°/s, fragmento de 1,70 × 0,42. Con la órbita más rápida o los fragmentos más finitos, apuntar se vuelve el desafío en vez de saber la respuesta.

## Evaluación y puntaje (sustento de la fundamentación)

Decisión tomada el 24/09/2026, charlando sobre cómo defender el proyecto: **el juego no pretende enseñar Python desde cero** — nadie aprende qué es una variable eligiendo entre cuatro opciones. Se fundamenta como herramienta de **refuerzo con corrección inmediata** (Kira explica el error en el momento) y de **evaluación de reconocimiento de sintaxis**.

El agujero que había que tapar: completar el juego no prueba nada, porque disparándole a todos los fragmentos tarde o temprano se acierta. Por eso el indicador **no es si completó, sino cuántos intentos le llevó cada pregunta**. Acertar de primera es conocimiento; acertar al tercero es descarte.

Implementado en `Assets/Scripts/Progreso/`:

- `QuestionAttempt` — el resultado de una pregunta: concepto, stage, intentos, segundos, qué opciones equivocadas eligió. El puntaje sale de los intentos: 1 → 100, 2 → 50, 3 → 25, 4 o más → 10. Nunca cero, porque equivocarse y corregirse también es aprender.
- `ScoreTracker` — singleton que se crea solo (`RuntimeInitializeOnLoadMethod`) y sobrevive los cambios de escena, así que no hay que ponerlo en cada stage. El indicador principal es `FirstTryRate`, el porcentaje de preguntas acertadas de primera.

**Todo vive solo en memoria: el juego no escribe ningún archivo.** Decisión de Kevin (24/09/2026): la exportación a CSV para el docente se armó y se sacó en el mismo día, por ser una pieza más para mantener y explicar sin que el proyecto la necesite todavía. El puntaje va adentro del juego y listo. Si más adelante hace falta el informe, se arma leyendo del `ScoreTracker` — está en el historial de git (commit `d3dafa6`), no hay que reescribirlo.

Cada `CodeBlasterEncounter` igual conviene que tenga cargados `concept` y `stageNumber`: no cuestan nada y son por donde se agruparía el día que se quiera ver el rendimiento por tema.

Pendiente: pantalla de resumen al terminar una stage. Hoy el puntaje solo se ve como "+100" al lado de la explicación de Kira.

**Fallback si se complica:** separar en dos momentos dentro de la misma escena — primero una zona de combate simple (esquivar/disparar sin preguntas de por medio), después una zona segura donde Kira pregunta con el menú de opción múltiple que ya existe (`fallbackQuizzes` en `StageData`). Mismo contenido educativo, mucha menos ingeniería.

## Fase 0 — Fundaciones (antes de tocar contenido de cualquier stage)

No es opcional ni se puede saltar — todo lo demás se apoya en esto.

- [x] **Git**: `git init`, `.gitignore` de Unity, primer commit y push a origin/main.
- [x] **CLAUDE.md actualizado** con el flujo de trabajo cerrado.
- [x] **ScriptableObjects de contenido**: `StageData`, `DialogueLine`, `QuizQuestion`, `CodeBlasterEncounter`. Scripts creados y compilando (sin `.assets` todavía).
- [x] **DialogueManager unificado**: `DialogueManager` + `DialogueManager2` fusionados en un solo `DialogueManager` con `enum DialogueSegment { Intro, Mid, Outro }`. Lee el contenido de un `StageData` (`introDialogue` / `midDialogue` / `outroDialogue`), ya no hardcodeado en `Awake()`. Intro congela al jugador + anima el dron + avance manual + handoff a `KiraFollower`; Mid/Outro sin congelar y solo auto-avance. Speaker por `enum` → "KIRA" cyan / "NX-7" amarillo. `KiraTriggerZone` y `EnemySpawner` actualizados; `DialogueManager2` borrado. Creado `Assets/Data/Stage1.asset` con las 5 líneas de intro + 4 de media migradas. Verificado en Play mode. **Cuadro de diálogo restilado** en `SampleScene` para combinar con el resto del juego: se sacó el borde naranja duro, panel HUD azul oscuro (`#141C2E`) con línea de acento cyan arriba, nombre del hablante en una placa (`FrameMap_7`) arriba a la izquierda, hint "ESPACIO" abajo a la derecha. Fuente: nombre y cuerpo en `PressStart2P` (la del juego, `CyberpunkCraftpixPixel` no tiene acentos ni `¿¡` y renderiza mal K/N/X). Como el `DialogueCanvas` es único, lo heredan las 6 stages.
- [x] **Arreglar bug del `PausaCanvas`**: el menú de pausa nunca se había terminado de armar (panel de 2×2 px, 9 botones duplicados sin texto ni `onClick`, título a escala 0.02). Rearmado desde cero en `SampleScene` con el kit visual del Menú Principal: panel `FrameMap_9` (tinte `#2A3450`), botones `FrameMap_7`, fuente `CyberpunkCraftpixPixel SDF`, texto `#C0C0C0`, dimmer al 55%. Botones: Reanudar / Reiniciar / Menú Principal, con `onClick` persistentes en la escena (`Reanudar` / `Reiniciar` / `IrAlMenu`). `PauseMenu.cs` suma `Reiniciar()` (recarga la escena activa). Verificado en Play mode. — El otro bug (Slice / Aseprite en el Tile Palette) se movió a Fase 2, ver abajo.
- [x] **Sistema de proyectiles + CodeBlasterTarget**: construido y probado en Play mode dentro del Stage 2. Piezas: `PlayerShooting` (apunta con el mouse, dispara con click izquierdo, solo si `PlayerMovement.HasWeapon`), `Projectile` (se mueve con un `CircleCast` barrido en vez de física — a la escala de este juego una bala rápida con collider normal atraviesa los blancos entre frames; ignora al jugador y los triggers del nivel, solo la frenan las paredes y los fragmentos), `CodeBlasterTarget` (fragmento tonto: muestra su texto y avisa que le pegaron, no sabe si es correcto), `CodeBlasterFight` (arma el encuentro leyendo todo del `CodeBlasterEncounter`) y `CodeBlasterUI` (pregunta fija arriba + respuesta de Kira abajo). El orden de los fragmentos en la órbita **se mezcla** para que la correcta no caiga siempre en el mismo lugar. El daño al errar ya está llamado vía la interfaz `IPlayerDamageable`, que todavía no implementa nadie — cuando exista el sistema de vida solo tiene que implementarla y ponerse en el jugador, sin tocar el combate.

## Fase 1 — Stage 1 como plantilla completa

- [ ] Stage 1 jugable de punta a punta con todos los sistemas nuevos probados: movimiento, Code Blaster, diálogo unificado (vía `StageData`), transición de escena.
  - [x] **Vida y corazones**: el `HudCanvas` del Stage 2 (3 corazones + `HealthUI`) se convirtió en el prefab compartido `Assets/Prefabs/UI/HudCanvas.prefab` y se instanció en `SampleScene`. Las dos stages lo heredan: tocar el prefab las cambia a las dos. El campo `player` del prefab va vacío a propósito — `HealthUI.Start()` busca el `PlayerHealth` de la escena solo. El jugador del Stage 1 ya tenía `PlayerHealth` (viene del `Player.prefab`), solo le faltaba la UI.
  - [x] **Dron kamikaze**: `KamikazeDrone` + `Assets/Prefabs/Combat/DronKamikaze.prefab` reemplazan al viejo `Enemigo.prefab` / `EnemyAI` en el `EnemySpawner` del Stage 1. El dron espera flotando en el `DroneSpawnPoint`, y cuando el jugador entra en su `activationRange` (6) se acomoda a la altura del cuerpo del jugador durante `aimDuration` (0.35 s) y sale derecho a velocidad constante (`speed` 3.5). Choca contra el jugador → explota y le saca un corazón; choca contra una pared → explota igual. En los dos casos desaparece. La explosión es un flash por código (blanco + se agranda + se desvanece, 0.22 s), no hay arte de explosión en el proyecto.
  - [x] **Costo de morir**: caerse al vacío cuesta un corazón (`FallRespawn.fallDamage`) y te devuelve al `respawnPoint` de esa zona. El daño de caída **saltea los segundos de gracia** a propósito: si no, caerse justo después de un golpe salía gratis. Quedarse sin corazones **reinicia la stage** (`PlayerHealth.Die()` recarga la escena activa después de `restartDelay`). Antes reaparecía en el último piso firme con la vida llena, o sea que perder no costaba nada; por eso se sacaron `respawnPoint`, `respawnInvulnerableTime` y el seguimiento de `lastSafeGround` de `PlayerHealth`, que ya no tenían a quién servir.
  - [x] **Salida al Stage 2**: el objeto `Exit` que puso Kevin (x −32,29, al fondo del corredor de abajo) lleva un `BoxCollider2D` en trigger y el script `StageExit`. Al tocarlo carga `Stage2`; si Kira está hablando en ese momento, **congela al jugador en la puerta** y espera a que el diálogo termine antes de irse — cruzar en mitad de una línea se comía la explicación, que es el contenido educativo. `Stage2` se agregó a **Build Settings** (índice 2), que faltaba y hacía que la carga fallara. `StageExit.transition` quedó vacío: carga directa, sin fundido (ver abajo).
  - [ ] Falta: encuentro Code Blaster del Stage 1 (preguntas conceptuales sobre Python). **Aplazado a propósito** el 02/10/2026 — Kevin dio el Stage 1 por cerrado por ahora con movimiento, vida, dron, diálogos y salida andando. Es lo único que falta para que el molde esté completo de verdad.

> **El fundido de escena del Stage 1 está apagado.** El root `Canvas` de `SampleScene`, que tiene el `SceneTransition`, está desactivado en la escena, así que `Start()` nunca corre y el fade no existe. Por eso `StageExit` carga la escena derecho. Si se quiere el fundido: activar ese Canvas y asignarlo en el campo `transition` del `Exit`.

> **Ojo con el reinicio de stage:** `PlayerHealth` es el mismo componente en las dos stages, así que morir en el Stage 2 también recarga el Stage 2. Y el `ScoreTracker` es `DontDestroyOnLoad`: los intentos de antes de morir **siguen contando** después del reintento. Si para la fundamentación conviene que un reintento arranque el puntaje de cero, hay que limpiarlo a mano al recargar.

> **Por qué el dron espera en vez de salir disparado:** el trigger que lo crea está en el piso de arriba (x −12,84) y el `DroneSpawnPoint` en el corredor de abajo, 11,6 unidades a la izquierda. Si arrancara al aparecer, se estrellaba contra la pared antes de que el jugador llegara a verlo.
>
> **Por qué se acomoda a la altura del jugador:** el pivote del jugador está en los pies y mide 0,32 de alto. A la altura cruda del marcador el dron le pasaba 0,07 por encima de la cabeza y no lo tocaba nunca. `matchPlayerHeight` evita tener que calibrar el marcador a mano cada vez que se mueve.

**Objetivo:** que el Stage 1 sea el molde que después se duplica para el resto. Stage 1 enseña información general de Python, no sintaxis todavía, así que las preguntas de Code Blaster acá pueden ser conceptuales ("¿qué es Python?", "¿para qué sirve programar?") — más simples de escribir que las técnicas de las stages siguientes. Buen lugar para probar el sistema antes de que el contenido se ponga más denso.

## Fase 2 — Pasada horizontal: versión rústica de Stages 2 a 6

- [ ] Stage 2 — nivel rústico + `StageData` placeholder (print, variables, tipos)
  - [x] Mapa armado (por Kevin), cámara `CameraFollow` igual que Stage 1, arma que el jugador agarra en el garage (`WeaponPickup` + animaciones armadas vía `PJ_Armed.overrideController`).
  - [x] ~~Primer encuentro Code Blaster~~ → **reemplazado por la terminal** (02/10/2026): consola en x −27,8 y puerta blindada en x −27,0, saliendo del garage. Ver "La mecánica educativa está en revisión" más abajo.
  - [x] **Diálogos**: `Assets/Data/Stage2.asset` con intro (6 líneas), media (5, la explicación del arma) y cierre (1). El `DialogueCanvas`, Kira y el `EventSystem` se **copiaron del Stage 1** para no rehacer el restilado. Kira sigue al jugador desde el arranque (`KiraFollower.followFromStart`), porque en esta stage ya viene con él.
  - [x] **Vida y enemigos**: tres corazones, `PlayerHealth` (que ahora sí contesta el `IPlayerDamageable` del Code Blaster), y tres `DroneEnemy` en los marcadores que dejó Kevin. Los drones se acercan, avisan, embisten y se alejan; se matan a tiros con `EnemyHealth`.
  - [x] **Arma visible**: sprite 7 del pack `Assets/ASSETS/Armas`, flotando en la puerta del garage, con diálogo de Kira al levantarla.
  - [x] **En Build Settings**: `Stage2` quedó en el índice 2 (02/10/2026), así que ya se llega jugando desde el Stage 1 por la puerta `Exit`.
  - [ ] Falta: más encuentros Code Blaster (hoy hay uno solo) y pantalla/flujo de fin de stage.

> **Trampa a recordar:** los sprites que se ponen en la capa de dibujo `Default` quedan **detrás** del nivel. Kira apareció invisible hasta que se la pasó a la capa `Personaje`. Todo lo que tenga que verse por delante del mapa va en `Personaje`.
- [ ] Stage 3 — nivel rústico + `StageData` placeholder (condicionales)
- [ ] Stage 4 — nivel rústico + `StageData` placeholder (bucles)
- [ ] Stage 5 — nivel rústico + `StageData` placeholder (listas y cadenas)
- [ ] Stage 6 — nivel rústico + `StageData` placeholder (funciones)
- [ ] **Pipeline de tilemap para Stages 2-6** (movido desde Fase 0): los tilesets vienen de archivos `.aseprite` (Aseprite Importer, 100 PPU, importados como sprite único). El botón **Slice del Sprite Editor está deshabilitado a propósito** para cualquier asset de scripted importer — no es un bug, es cómo funciona el importador. Para armar el Tile Palette: exportar cada tileset a PNG, importarlo con el **Texture Importer a 16 PPU**, cortar en grilla 16×16, y usar un **Grid nuevo con Cell Size (1,1,0)**. NO tocar el Grid de `SampleScene` (Cell Size 0.16) — el piso de Stage 1 ya está pintado sobre él con `CompositeCollider2D` y cambiarlo rompe el nivel.

Con el molde de Stage 1 probado, se arma una versión mínima jugable de cada stage restante: nivel con mapa ASCII convertido a tilemap, un par de enemigos, un `StageData` con contenido placeholder (2-3 preguntas alcanza por ahora).

El objetivo de esta fase NO es que quede pulido — es que el juego completo exista y se pueda jugar de principio a fin, aunque cada stage tenga poco contenido todavía.

Mapeo rápido de concepto → tipo de pregunta Code Blaster por stage:

| Stage | Concepto | Ejemplo de pregunta Code Blaster |
|---|---|---|
| 2 | print, variables, tipos | "¿Cuál imprime el texto correctamente?" — opciones con comillas mal puestas, `print` mal escrito |
| 3 | condicionales | "¿Cuál es la sintaxis correcta de un if?" — falta `:`, indentación mal, `=` en vez de `==` |
| 4 | bucles | "¿Cuál bucle imprime del 1 al 5?" — variantes de `range()` mal usadas |
| 5 | listas y cadenas | "¿Cómo accedés al primer elemento?" — índices mal, sintaxis de slice mal |
| 6 | funciones | "¿Cuál define la función correctamente?" — falta `def`, paréntesis mal, falta `:` |

## La mecánica educativa está en revisión (02/10/2026)

**Kevin rechazó el Code Blaster.** Su objeción, textual: *"se ve muy IA, un múltiple opción, algo que podría hacer en cualquier HTML, quiero algo que pueda ver en un juego realmente y no en una página web"*.

Ojo con el diagnóstico: la queja **no es pedagógica, es de forma**. Elegir entre cuatro fragmentos flotantes no se siente un videojuego, se siente un formulario con sprites encima. Y coincide con lo que ya estaba anotado más arriba: con la órbita rápida o los fragmentos finitos, apuntar se vuelve el desafío en vez de saber.

De esto se desprende una regla de diseño para lo que venga: **lo que hace que sea un juego no es cómo se responde, es qué pasa en el mundo cuando acertás.** Un cartel de "¡Correcto!" se puede hacer en cualquier página; una puerta blindada que tiembla y se mete en el techo mientras el jugador la mira, no.

### Escena de pruebas

`Assets/Scenes/Laboratorio.unity` — banco de pruebas, **a propósito fuera de Build Settings**. Cámara igual a la del juego (ortográfica, size 1.8, `CameraFollow`), luz global 2D, el `Player.prefab`, piso de 40 unidades con paredes, corazones y `EventSystem`. Sirve para probar mecánicas sin tocar ninguna stage.

### Prototipo 1: Terminal (hecho)

El jugador se acerca a una consola de NEXCORP, aprieta **E**, y **escribe con el teclado de verdad** la parte que falta de una línea de Python. Al acertar, la línea se ejecuta, imprime su salida, y **se abre una puerta blindada**.

- `TerminalChallenge` (ScriptableObject) — la consigna, la línea partida en `codeBefore` / hueco / `codeAfter`, las respuestas aceptadas, y **los errores previstos con el traceback real de Python y la explicación de Kira**. Esa lista es la mitad del valor educativo: escribir `Print` devuelve `NameError: name 'Print' is not defined` y Kira explica que Python distingue mayúsculas.
- `CodeTerminalUI` — se arma sola en runtime (no hay que montar Canvas a mano). Monitor CRT con líneas de barrido, parpadeo del tubo, cursor que titila y salida tecleada letra por letra.
- `CodeTerminal` — proximidad, tecleo, validación, puntaje, y un `UnityEvent onSolved` que es donde se engancha la consecuencia física.
- `PoweredDoor` — la puerta. Tiembla, forcejea y se mete en el techo con `SmoothStep` (arranca pesada, frena al final); recién ahí desactiva el collider.
- Contenido de prueba: `Assets/Data/Terminal/Lab_Print_01.asset` (print, con 5 errores previstos).

**Cambia qué se evalúa:** ya no es reconocer la respuesta entre cuatro, es acordarse de ella. Y el `ScoreTracker` guarda **lo que el alumno escribió de verdad**, no cuál de cuatro botones tocó — dato más rico que el del Code Blaster para la fundamentación.

**El riesgo a vigilar:** que el error de tipeo se sienta error de concepto. Por eso el hueco es de una palabra sola, se conserva lo escrito tras fallar (se corrige, no se rehace) y los errores comunes tienen respuesta propia.

### Decidido: la terminal reemplaza al Code Blaster (02/10/2026)

Kevin aprobó la terminal y pidió llevarla al Stage 2 sacando los fragmentos. Hecho:

- **Sacado de `Stage2.unity`**: `CodeBlasterFight_01` y `CodeBlasterCanvas`. Los **drones de patrulla quedan** — son combate, no cuestionario.
- **Puesto**: `ConsolaNexcorp` (x −27,8, justo saliendo del garage) + `PuertaBlindada` en el lugar exacto que marcó Kevin con el objeto `Puerta`, que se borró. `TerminalUI` en la escena.
- **Contenido**: `Assets/Data/Terminal/Stage2_Print_01.asset`. La ficción cierra el concepto con la mecánica: la cerradura vieja obedece al comando que le mandes, así que hay que escribir `print("ABRIR")`. Seis errores previstos, incluido escribir `abrir` (confundir *qué* querés con *cómo* decirlo).
- **Diálogos arreglados** en `Assets/Data/Stage2.asset`: las líneas mid [2] y [4] hablaban de dispararle a fragmentos. Ahora [2] explica que el arma es para los drones de patrulla y [4] presenta la puerta blindada ("Esa necesita que alguien le escriba lo que tiene que hacer").
- **La puerta abre animando los postigos** (`PoweredDoor.openFrames`, frames 0→1→2 de `Entry.png`) en vez de deslizarse: ahí no hay techo donde meterla. Las luces se apagan al terminar de abrir, porque la puerta se retrae del todo y si no quedaban dos cuadraditos verdes flotando en el aire.
- El collider de la puerta quedó en 0,55 × 1,03, **exactamente lo que marcó Kevin**. El dibujo es cuadrado (64×64) así que para no deformar las rayas de peligro se escaló parejo: se ve más ancho que el marcador aunque bloquee lo mismo.

**Todavía en el proyecto pero sin usar:** `CodeBlasterFight`, `CodeBlasterUI`, `CodeBlasterTarget`, `CodeBlasterEncounter` y `Assets/Data/Encuentros/Stage2_Print_01.asset`. No se borraron por si hay que volver atrás; ninguna escena los referencia.

> **Choque conocido:** la terminal usa ESC para salir y el `PauseMenu` también usa ESC para pausar. En el `Laboratorio` no hay `PauseMenu` así que no se nota, pero si la terminal se lleva a una stage real hay que resolverlo.

## Fase 3 — Selector de niveles y flujo completo

- [ ] Menú de selección de stage con gating (`nextStage` de `StageData` + variable `maxUnlockedStage` en un singleton `DontDestroyOnLoad`, sin persistencia en disco)
- [ ] Playtest completo de punta a punta, Stage 1 a 6, anotando qué se siente mal

## Fase 4 — Pulido profundo, por tandas

- [ ] Stages 2-3: diálogos reales, más preguntas Code Blaster, ajuste de dificultad, level design cuidado
- [ ] Stages 4-6: lo mismo

Orden sugerido: 2-3 primero (conceptos más simples, valida el patrón de pulido más rápido), después 4-6.

## Fase 5 — Audio y remate final

- [ ] Música ambiente + efectos (disparo/impacto/acierto/error)
- [ ] Pulido de UI
- [ ] Build final de Windows
- [ ] Bug bash general

## Qué cortar primero si el tiempo aprieta (en este orden)

1. Audio — ya está marcado como baja prioridad.
2. Variedad de enemigos por stage — quedarse con un tipo reskineado.
3. Cantidad de preguntas Code Blaster por stage — bajar a lo mínimo que cubra el concepto.
4. La mecánica mezclada Code Blaster completa → fallback al modo separado (combate simple + `fallbackQuizzes` en zona segura).
5. Selector de niveles con gating → linealizar directamente, sin menú de selección.

## Nota sobre el cronograma de 2 meses

Si se empuja fuerte, las Fases 0 a 3 (fundaciones + Stage 1 + pasada rústica de todo + selector) entran cómodo en las primeras 3-4 semanas — eso es el objetivo original de "un mes". Eso deja las 4-5 semanas restantes como colchón real para las Fases 4 y 5, que es donde el juego pasa de "funciona" a "es bueno de jugar". Ese colchón es lo que evita llegar corriendo al final.
