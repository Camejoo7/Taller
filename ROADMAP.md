# CodeBreak — Roadmap de Desarrollo

> Generado a partir de una sesión de planificación con Kevin. Actualizalo a medida que el proyecto avanza — es un mapa, no una ley. Si algo cambia, editá este archivo y contáselo a Claude Code al arrancar la sesión siguiente.

## 🔄 CAMBIO DE PLAN — 02/10/2026: no hay 6 stages, hay UN mapa grande

**Decisión de Kevin, al cierre de la sesión del 02/10:** se cancelan las stages 3 a 6. En vez de seis niveles chicos, uno por concepto, el **Stage 2 pasa a ser un mapa gigante con muchas terminales repartidas**, y entre todas cubren los conceptos que iban a estar desparramados en las seis stages.

Textual: *"este stage 2 va a ser un mapa gigante, con un montón de estas terminales por el mapa, para aprender todos los conceptos que se iban a dar en todos los stage... así que vamos a estar reutilizando esas terminales"*.

**Por qué cierra bien:** la terminal ya es una pieza reutilizable por diseño — cada `CodeTerminal` apunta a un `TerminalChallenge` (ScriptableObject) y dispara un `UnityEvent onSolved`. Agregar un ejercicio nuevo es crear un `.asset` y poner una consola, no programar nada. El sistema estaba listo para esto antes de que se decidiera.

**Qué queda de pie:** Stage 1 sigue siendo la intro (historia, movimiento, dron kamikaze, salida). Stage 2 se convierte en el juego.

**Qué se cae:** las stages 3-6, el selector de niveles con gating entre stages, y todo lo que decía "una stage por concepto". El gating ahora lo hacen **las puertas dentro del mismo mapa**: cada terminal abre la suya. Eso ya está probado y es mejor que un menú, porque el progreso se ve en el mundo.

**La consecuencia técnica más importante, y lo primero a hacer la sesión que viene:** hoy la consola del Stage 2 es un objeto armado a mano en la escena (`ConsolaNexcorp` = terminal animado + `PromptE` + `CodeTerminal`), más la `PuertaBlindada` y el `SectorSinEnergia` por separado. Si van a ser muchas, **hay que convertir ese conjunto en prefab** para poder soltarlo y solo asignar el `TerminalChallenge` y el `onSolved`. Armar la segunda a mano ya sería trabajo tirado.

## 📍 Dónde estamos ahora

**Fase actual: Fase 2 — El mapa grande del Stage 2** (Fase 0 cerrada; Stage 1 jugable)

Antes de aceptar o proponer cualquier tarea nueva, mirá la lista de la fase actual más abajo. Si en algún momento Claude Code (o vos mismo) propone algo de una fase más adelante mientras todavía quedan ítems sin marcar en la fase actual, es una señal de alerta — no está prohibido saltar el orden, pero hacerlo a propósito y no por perderse.

## Decisiones tomadas (para no repetir la charla)

| Decisión | Resultado |
|---|---|
| Plazo | 2 meses reales, con meta interna de avanzar fuerte las primeras 3-4 semanas |
| ~~Cantidad de stages~~ | **Cambiado el 02/10/2026**: ya no son 6 stages. Stage 1 (intro) + Stage 2 (un mapa grande con todas las terminales). Ver arriba |
| Densidad | Stage 2 tiene que dar para toda la currícula: print/variables/tipos, condicionales, bucles, listas/cadenas y funciones |
| Prioridad de trabajo | Ancho de mapa primero: ir extendiendo el Stage 2 zona por zona, cada una con su terminal y su puerta |
| ~~Relación enemigos-aprendizaje~~ | El Code Blaster quedó **congelado** (ver abajo). Hoy son dos cosas separadas: los drones son combate, las terminales son el aprendizaje |
| Combate | Con disparo / proyectiles |
| Audio | Ambiente + efectos, pero al final — baja prioridad |
| Progresión | **Puertas dentro del mismo mapa**: cada terminal abre la suya. Sin selector de niveles y sin guardado de sesión |
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

## Fase 2 — El mapa grande del Stage 2

- [ ] Stage 2 — el juego entero: un mapa que se va extendiendo, con una terminal por concepto
  - [x] Mapa armado (por Kevin), cámara `CameraFollow` igual que Stage 1, arma que el jugador agarra en el garage (`WeaponPickup` + animaciones armadas vía `PJ_Armed.overrideController`).
  - [x] ~~Primer encuentro Code Blaster~~ → **reemplazado por la terminal** (02/10/2026): consola en x −27,8 y puerta blindada en x −27,0, saliendo del garage. Ver "La mecánica educativa está en revisión" más abajo.
  - [x] **Diálogos**: `Assets/Data/Stage2.asset` con intro (6 líneas), media (5, la explicación del arma) y cierre (1). El `DialogueCanvas`, Kira y el `EventSystem` se **copiaron del Stage 1** para no rehacer el restilado. Kira sigue al jugador desde el arranque (`KiraFollower.followFromStart`), porque en esta stage ya viene con él.
  - [x] **Vida y enemigos**: tres corazones, `PlayerHealth` (que ahora sí contesta el `IPlayerDamageable` del Code Blaster), y tres `DroneEnemy` en los marcadores que dejó Kevin. Los drones se acercan, avisan, embisten y se alejan; se matan a tiros con `EnemyHealth`.
  - [x] **Arma visible**: sprite 7 del pack `Assets/ASSETS/Armas`, flotando en la puerta del garage, con diálogo de Kira al levantarla.
  - [x] **En Build Settings**: `Stage2` quedó en el índice 2 (02/10/2026), así que ya se llega jugando desde el Stage 1 por la puerta `Exit`.
  - [x] **Prefabear el conjunto terminal + puerta + cortina** (03/10/2026): `Assets/Prefabs/Terminal/TerminalPuerta.prefab`. El origen es la puerta; adentro van `ConsolaNexcorp`, `PuertaBlindada` y `SectorSinEnergia`, con el `onSolved` (puerta `Open` + cortina `Reveal`) ya enganchado dentro del prefab. **Para agregar un ejercicio: soltar el prefab, asignar el `challenge` y ajustar el `rightEdge` de la cortina hasta donde llega esa zona.** Lo demás viene armado.
    - El `challenge` del prefab va **vacío a propósito**, para que una copia nueva no repita el ejercicio de print sin que nadie se dé cuenta. Si queda vacío, `CodeTerminal` lo avisa con un warning en consola.
    - El `ui` también va vacío: `CodeTerminal.Start()` busca el `CodeTerminalUI` de la escena solo (un prefab no puede guardar referencias a objetos de la escena).
    - ⚠️ **Los bordes de `PowerCurtain` ahora son relativos al objeto, ya no de mundo** (se ignoran su escala y rotación). Sin ese cambio la oscuridad se quedaba en su lugar aunque movieras el prefab. La instancia del Stage 2 se convirtió con los bordes ya recalculados, así que en el mundo quedó igual que antes.
  - [ ] Extender el mapa con una zona nueva por concepto (ver la tabla de abajo), cada una con su terminal y su puerta.
  - [ ] Escribir los `TerminalChallenge` de cada concepto, con sus errores previstos y la explicación de Kira.
  - [ ] Pantalla / flujo de final del mapa.

> **Trampa a recordar:** los sprites que se ponen en la capa de dibujo `Default` quedan **detrás** del nivel. Kira apareció invisible hasta que se la pasó a la capa `Personaje`. Todo lo que tenga que verse por delante del mapa va en `Personaje`.
- [ ] **Pipeline de tilemap para el mapa grande** (movido desde Fase 0): los tilesets vienen de archivos `.aseprite` (Aseprite Importer, 100 PPU, importados como sprite único). El botón **Slice del Sprite Editor está deshabilitado a propósito** para cualquier asset de scripted importer — no es un bug, es cómo funciona el importador. Para armar el Tile Palette: exportar cada tileset a PNG, importarlo con el **Texture Importer a 16 PPU**, cortar en grilla 16×16, y usar un **Grid nuevo con Cell Size (1,1,0)**. NO tocar el Grid de `SampleScene` (Cell Size 0.16) — el piso de Stage 1 ya está pintado sobre él con `CompositeCollider2D` y cambiarlo rompe el nivel.

El objetivo de esta fase es que el mapa **se pueda jugar de punta a punta**, aunque cada zona tenga poco contenido todavía. Se crece zona por zona: mapa → terminal → puerta → siguiente zona.

Mapeo de concepto → qué pide la terminal. Lo que se escribe es el **hueco** de una línea de Python, y la ficción de cada zona tiene que justificar por qué esa línea abre esa puerta:

| Zona | Concepto | Qué escribe el jugador | Hecho |
|---|---|---|---|
| 1 | print | `print("ABRIR")` — la cerradura vieja obedece al comando que le mandes | ✅ `Stage2_Print_01.asset` |
| 2 | variables y tipos | Guardar un valor y usarlo después (una clave, un código de acceso) | ⬜ |
| 3 | condicionales | Un `if` que decide si la puerta se abre según una lectura del sensor | ⬜ |
| 4 | bucles | Un `for`/`while` que repita algo N veces — reiniciar N nodos, recorrer una lista de puertas | ⬜ |
| 5 | listas y cadenas | Sacar un elemento de una lista o un pedazo de una cadena (un código escondido en una trama) | ⬜ |
| 6 | funciones | Definir una función y llamarla — "la cerradura necesita que le enseñes a abrirse" | ⬜ |

> **Lo que hace que sea un juego no es cómo se responde, es qué pasa en el mundo cuando acertás.** Está escrito más abajo y vale para cada terminal nueva: no alcanza con un cartel de "¡Correcto!". La puerta que tiembla, las luces que se encienden, el sector que recupera la energía — eso es lo que no se puede hacer en una página web. Si una terminal nueva no tiene una consecuencia física, le falta la mitad.

### Variantes de terminal aprobadas (03/10/2026)

La terminal de completar el hueco es la base y va a aparecer muy seguido. Para que no se vuelva repetitiva, Kevin aprobó estas variantes. **Al armar la zona o la terminal de cualquier concepto, Claude tiene que sugerir la que mejor encaje**, y se prueban primero en el `Laboratorio`.

| # | Variante | Qué pasa | Encaja con | Inspiración |
|---|---|---|---|---|
| 1 | **Hackear variables del mundo** | La terminal muestra una variable real (`largo_puente = 2`) y el estudiante **modifica el valor**: el puente se estira. Hay más de un valor válido (con 3 no llega, con 20 atraviesa la pared), y `"8"` en vez de `8` da el `TypeError` real. Recicla el prefab `TerminalPuerta`: en vez de escribir código, se modifica | variables y tipos | *Hack 'n' Slash* |
| 2 | **Código roto (debugging)** | La línea viene escrita con un bug (falta `:`, mala indentación, sin comillas). La puerta intenta abrirse, tira el traceback y se traba; el estudiante la arregla. Es la misma familia que la 1: modificar código existente | cualquiera | *Gidget* (con Kira como "robot falible con personalidad", que en los estudios hizo completar más niveles) |
| 3 | **Predecí la salida** | La cerradura pide lo que imprime un código cerrado: `for i in range(3): print(i * 2)` → clave `024` | repaso, sobre todo bucles | investigación sobre code tracing |
| 4 | **Kira programable** ⭐ (la que más le gustó) | Kira pasa por donde el jugador no puede: `kira.mover(4)`, `kira.activar()` | funciones y argumentos | *Duskers* |
| 6 | **Torreta con `if`** | `if objetivo == "___": disparar()`. Con `"dron"` limpia el pasillo; si te equivocás, te apunta a vos. **El error también hace algo en el mundo** | condicionales | — |
| 7 | **Hackear un dron aturdido** | Le disparás, queda chispeando, aparece la "E", y `dron.bando = "aliado"` lo da vuelta contra los suyos. Une combate y código | variables / atributos | — |

Descartadas en la misma charla (no proponerlas primero): escalera que se arma vuelta por vuelta del `for`, interruptores tediosos antes del bucle / función que queda como habilidad, grafiti al revés con slicing, y bloques de código físicos estilo Parsons (ese roza el multiple choice del Code Blaster).

**Lo que falta en el código para habilitarlas:**
- **1 y 6:** que `CodeTerminal` le pase **el valor escrito** al mundo, no solo `onSolved`.
- **1 y 2:** que la terminal arranque con texto prellenado que se pueda borrar y editar.

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

### La pantalla de la terminal, vestida con el pack "Consola" (02/10/2026)

El `CodeTerminalUI` dibujaba el monitor por código: un rectángulo verde con cuatro barras de marco. Ahora el texto vive **adentro de un monitor dibujado** (`ASSETS/Consola/1 Monitor/2.png`), que es justo lo que separa "un juego" de "una página web" según la regla de diseño de más arriba.

- **El sprite y el recorte del vidrio son campos del Inspector.** `monitorSprite` + `glassRect` (xMin, yMin, xMax, yMax en fracciones 0-1). Cambiar de monitor es cambiar un sprite y cuatro números — los del 1.png, 2.png y 3.png están medidos y anotados en el tooltip. El layout de adentro está tuneado contra un espacio de diseño fijo de 1154×650 que después se escala entero, así que mover `monitorHeight` no desacomoda el texto.
- **`monitorHeight` = 1010 es 2.png a 5x exacto.** El pixel art se ve nítido en escala entera y borroneado en cualquier otra; los PNG se importaron con filtro Point y sin compresión.
- **Encendido de CRT**: el vidrio se abre desde una raya horizontal (0,18 s) y recién ahí entra el texto. Más: viñeta en las esquinas (el tubo es curvo y pierde luz), una banda ancha y tenue que baja, y el parpadeo que ya estaba.
- **`ACCESS GRANTED` / `ACCESS DENIED`** (`3 Other/`) se estampan en el vidrio al acertar o errar, llamados desde `CodeTerminal.Succeed()` / `Fail()`. Caen en el hueco entre el traceback y la línea de Kira **a propósito**: el veredicto no puede tapar el error, que es lo que el alumno tiene que leer.

> **Trampa que costó dos vueltas:** el proyecto renderiza en **espacio lineal**, así que los alfas chicos rinden mucho más de lo que uno espera. El flash rojo del error a 0,22 dejaba la pantalla entera roja e ilegible; quedó en **0,09**, y además se movió **debajo** del texto en la jerarquía. Si se agrega cualquier otro tinte a pantalla completa, probarlo antes de confiar en el número.

> **Ojo al verificar en Play mode por MCP:** el Editor arranca *pausado* (`EditorApplication.isPaused`), así que `Time.deltaTime` es 0 y cualquier animación por corrutina queda congelada en el primer frame — parece un bug del código y no lo es. Hay que despausar y poner `Application.runInBackground = true` antes de sacar screenshots.

**La fuente del pack quedó afuera.** `ASSETS/Consola/Font.txt` no trae un `.ttf`: es un link a *Future Millennium* en dafont. La terminal sigue en `PressStart2P`, que ya es la del juego y tiene acentos y `¿¡`. Si Kevin baja el `.ttf`, armar el TMP Font Asset y asignarlo al campo `font` es un minuto.

**Basura del pack:** `ASSETS/Consola/__MACOSX/` son resource forks de macOS (`._archivo.png`), no sirven para nada y conviene borrar la carpeta antes de commitear.

### La consola en el mapa (02/10/2026)

El objeto del mundo era un placeholder feo: un `Square.png` de Unity teñido de gris como base, el `Screen1` plano encima y una "E" de TextMeshPro suelta. Rehecho:

- **El mueble**: `INDUSTRIA/4 Animated objects/Screen2` — un terminal con pedestal **propio**, 4 frames ya cortados, 32×42 px a 100 PPU. Se eligió por la densidad de pixel: mide 0,56 de alto a escala 1,6 contra los 0,38 del jugador, y sus pixeles son del mismo tamaño que los del nivel. Animado con el nuevo `SpriteFlipbook` (cicla frames en un SpriteRenderer; montar un Animator + Controller para una pantalla que parpadea es más archivos de los que el objeto se merece). Se borró el cubo gris: el dibujo ya trae su base.
- **El cartel de E**: nuevo `InteractPrompt`. Dibuja una **tecla** — cuerpo oscuro, borde que late, brillo arriba, sombra abajo — que entra con un rebote (se pasa de 1 y vuelve), flota y respira. Se arma sola en runtime y se prende/apaga con `SetActive`, así que `CodeTerminal` no cambió ni una línea. La textura es de 16×16 para que sus pixeles midan lo mismo que los del nivel.

> **El bug del jugador tapado era de orden de dibujo, no de posición.** `Player/Visual` estaba en la capa `Personaje` con orden **0**, igual que la base de la consola, y la pantalla en **1** — o sea, por delante del jugador. Se subió el jugador a **orden 10 en `Player.prefab`**, así que vale para las dos stages y cualquier objeto nuevo que se ponga en orden 0 queda detrás de él solo. Es la regla a seguir de acá en más: **props del mundo en orden 0 o menos, jugador en 10, carteles de interfaz en 20+.**

### Kira deja de parecer un gif pegado (02/10/2026)

Kevin: *"el movimiento de KIRA es muy estático, se mueve sí, pero siempre en la misma posición, parece un gif pegado en ese punto de la pantalla"*. Tenía razón y había tres causas, las tres en `KiraFollower`:

- **El offset era fijo `(-1, 1)` y nunca cambiaba de lado.** Siempre a la izquierda: yendo a la derecha iba atrás, pero yendo a la izquierda iba **adelante**, guiando al jugador.
- **`followSpeed` era 3 y el jugador corre a 3.** Idénticos, así que nunca se quedaba atrás: lo acompañaba clavada en el mismo punto de pantalla.
- **Bug: nunca miraba para el otro lado.** El flip era `if (jugador.x > kira.x)`, pero como el offset la ponía siempre a la izquierda esa condición daba true *siempre*; el `else` no corría nunca y Kira miraba a la derecha toda la partida.

Reescrito. Lo que la hace parecer viva no es moverse más, es **llegar tarde**:

| | qué hace |
|---|---|
| `smoothTime` 0,38 | `SmoothDamp` en vez de `MoveTowards`: acelera, se queda atrás al correr, sobrepasa un poco y se asienta. En 0 vuelve a ser un sprite clavado |
| `sideSwitchSpeed` 2,2 | Se pone siempre del lado contrario al que mira el jugador, y cruza con pachorra cuando se da vuelta |
| `crossLift` 0,35 | Mientras cruza se levanta, así le pasa **por arriba** haciendo un arco en vez de atravesarlo |
| `bankAngle` 16° | Se tumba hacia donde va. Un dron que nunca se inclina se lee como calcomanía |
| `driftAmount` 0,1 | Dos senos de frecuencias que no son múltiplos entre sí (0,83 y 1,17), así el ciclo tarda muchísimo en repetirse y no se lee como loop |

`followSpeed` cambió de significado: ahora es el **techo** de velocidad del `SmoothDamp`, y tiene que ser mayor que la del jugador o no lo alcanza nunca. Se subió de 3 a **8** en las dos escenas.

> **Ojo con la inclinación y el espejado:** Kira se da vuelta con `localScale.x = ±1`. Al espejar con escala negativa, una rotación se ve al revés, así que el ángulo se niega cuando mira a la izquierda. Si no, se tumba para el lado contrario al que viaja y queda rarísimo.

Verificado en Play mode: yendo a la derecha queda a la izquierda mirando a la derecha; mandándolo a la izquierda **cruza** a la derecha y da vuelta la mirada. Agarrada a mitad del cruce estaba en `rel x = −0,15` (casi encima del jugador), a `1,19` de alto en vez de 1,0 (el arco) e inclinada −8,3°.

### El sector de atrás de la puerta está sin energía (02/10/2026)

Kevin notó que desde la consola se ve entero el pasillo que sigue, y que eso le baja el impacto a abrir la puerta. En vez de solo taparlo, se hizo que **el sector esté sin energía**: resolver la terminal ya no abre una puerta, **prende el sector**. La luz entra barriendo de izquierda a derecha y la puerta se mete.

- `PowerCurtain` — un sprite oscuro con degradé que tapa de x −26,75 (el borde derecho de la puerta) hasta x 3,5, de y −8 a 3. `Reveal()` corre el borde izquierdo hacia la derecha con `SmoothStep` en 1,2 s y después se apaga solo. Está enganchado al mismo `onSolved` que ya abría la puerta: hoy ese evento tiene dos oyentes, `PuertaBlindada.Open` y `SectorSinEnergia.Reveal`.
- **Capa `Personaje` orden 8**: por encima de los tilemaps (que están en `Default`) y de los drones (`Personaje` orden 1), pero por debajo del jugador (orden 10).
- **No usa luces 2D a propósito**, por lo de más abajo. Es un sprite, así que no toca la iluminación de nadie.
- El material se fuerza a **`Sprite-Unlit-Default`**: es una tapa, no algo del mundo, y si algún día la stage recibe luces no tiene que iluminarse.

> **⚠️ Trampa cara: el alfa del color de un SpriteRenderer NO sirve para esto.** Los shaders de sprite de URP trabajan con alfa premultiplicado, así que un color oscuro con alfa apenas por debajo de 1 (probado con 0,965) **no tapa absolutamente nada** — se ve igual que si el objeto no existiera, aunque `isVisible` diga true y los bounds estén perfectos. Lo que despista es que un **rojo saturado con el mismo alfa sí se ve**, así que parece un problema de color o de orden de dibujo y no lo es. La regla: `color.a` siempre en 1, y la transparencia del borde se hace con el **canal alfa de la textura**, que funciona bien.

> **Ojo al verificar por MCP:** el editor se vuelve a pausar solo entre llamadas, no solo al entrar a Play. Con `deltaTime` en 0 las corrutinas quedan congeladas y parece que el código no anda. Antes de medir cualquier animación, chequear `EditorApplication.isPaused` **en la misma llamada**, no al principio de la sesión.

### Drones del Stage 2: bajada de dificultad (02/10/2026)

Kevin: *"están salados, un poquito más lento el ataque quizás"*. Tocado en `Assets/Prefabs/Combat/Dron.prefab` y en los tres de la escena, más los valores por defecto del script:

| | antes | ahora | por qué |
|---|---|---|---|
| `pauseBeforeLunge` | 0,35 | **0,6** | El aviso. Una persona tarda ~0,25 s en reaccionar, así que 0,35 dejaba 0,1 s útiles; ahora quedan 0,35 s, que a velocidad 5 son 1,75 unidades para correrse |
| `lungeSpeed` | 5 | **2,5** | Embestía **más rápido de lo que corre el jugador**, así que no se podía zafar corriendo |
| `lungeDuration` | 0,45 | **0,55** | Avanza 1,375. Para tocar a un jugador quieto necesita 1,285 (`lungeRange` 1,6 menos los radios de los dos colliders, 0,19 + 0,125): **llega justo**. Un jugador que corre hace 1,65 en ese tiempo, o sea que **escapa** |
| `retreatDuration` | 0,8 | **1,3** | El respiro para disparar. Estaba **por debajo** del 1,1 que traía el script, que es justo lo que su propio comentario advertía que no se bajara |
| `approachSpeed` | 1,6 | **1,3** | Menos presión entre ataques |
| `activationRange` | 6 | **5** | Se despiertan menos drones a la vez |

Sin tocar: `contactDamage` 1, vida del dron 3, `invulnerableTime` 1,2 del jugador.

> **⚠️ El jugador corre a 3 y salta con `jumpForce` 6,7 — NO a 5 y 10.** Esos son los valores por defecto que están escritos en `PlayerMovement.cs`, pero el `Player.prefab` tiene otros serializados, y el serializado es el que manda. En la primera pasada de esta tanda se tunearon los drones contra el 5 y quedaron mal (la embestida a 3,8 seguía siendo más rápida que el jugador). Antes de balancear cualquier cosa contra la velocidad del jugador, **leer el valor del prefab, no el del script**. Con gravedad ×3, `jumpForce` 6,7 da un salto de ~0,76 de alto.

**Además, el ataque ahora se ve venir.** El único aviso era que el dron se frenaba, y a este tamaño eso no se nota: el ataque parecía salir de la nada. Ahora parpadea en rojo (`telegraphColor`) durante la pausa. Solo le toca el color mientras avisa y un frame al salir, para no pisarle a `EnemyHealth` el parpadeo blanco del balazo. Si Kevin lo quiere más evidente, es ese campo del Inspector.

### El jugador se pegaba a las paredes (02/10/2026)

Síntoma de Kevin: *"cuando corro hacia adelante y salto chocándome con una pared no salta, o salta un poquito pero queda pegado; para saltar la primera pared tengo que quedarme quieto, saltar y moverme en el aire"*. Eran **dos problemas distintos** encimados, los dos arreglados en `Player.prefab`, así que valen para las dos stages.

1. **La fricción, que era la causa principal.** El `CapsuleCollider2D` del jugador no tenía `PhysicsMaterial2D`, así que usaba la fricción por defecto de **0,4**. Como `Update()` reescribe `linearVelocity.x` todos los frames, tener la tecla apretada contra una pared genera una fuerza normal constante, y la fricción se come la velocidad vertical del salto. Medido en Stage 2 contra una pared de 0,64 de alto, con salto libre de ~0,76: **con fricción 0,4 el salto subía 0,187 unidades; con 0 sube 0,711** (lo que tarda en pasar la pared y caer encima). Nuevo asset `Assets/Physics/Jugador.physicsMaterial2D` con fricción 0 y rebote 0, asignado al collider del prefab. Poner la fricción en cero es seguro justamente porque el movimiento setea la velocidad a mano: el jugador no patina, porque con `moveX` en 0 la velocidad se pone en 0 explícitamente.

2. **La detección de piso borraba su propio resultado.** `OnCollisionStay2D` se llama **una vez por cada collider** que te toca, y el código terminaba en `isGrounded = false` si *ese* collider no era piso. Estando parado en el piso y tocando un collider aparte — la **puerta blindada** o un **dron** — el callback del segundo borraba el piso que había informado el primero, y el salto no salía. (Contra las paredes del nivel no pasaba, porque piso y paredes son el mismo `CompositeCollider2D`.) Ahora los callbacks solo **suman** a `groundedThisStep`, y `FixedUpdate` publica el resultado y arranca de cero — así ningún collider puede pisar lo que informó otro. Se borró el `OnCollisionExit2D`, que ponía `isGrounded = false` sin fijarse en nada.

> **Trampa que ese cambio destapó:** un `Rigidbody2D` quieto **se duerme**, y dormido **deja de tirar `OnCollisionStay2D`**. Con el código viejo no se notaba porque `isGrounded` quedaba latcheado en true; con el recálculo por frame, el jugador parado quieto se quedaba sin piso y no podía saltar. Lo agarró la verificación (`IsGrounded=False` con un contacto de piso presente en la lista). Se arregla con `rb.sleepMode = RigidbodySleepMode2D.NeverSleep` en `Start()` — en código y no en el Inspector, para que no se pierda si alguien toca el prefab.

> **Queda sin tocar:** el `Rigidbody2D` está en `Discrete`. Con un collider de 0,25 × 0,32 y gravedad 3, una caída larga podría atravesar piso fino. No es lo que reportó Kevin y no se vio pasar, pero si algún día alguien se cae por el piso, empezar por ahí.

> **⚠️ Stage2 no tiene ninguna luz 2D, y no hay que agregarle una suelta.** Se probó ponerle un `Light2D` de punto a la pantalla para que brillara y **se fue toda la escena a negro** menos el círculo iluminado: mientras no hay ninguna luz, URP dibuja todo a brillo pleno; apenas aparece una, el renderer empieza a iluminar de verdad y lo que queda fuera del radio no recibe nada. Se sacó. Si alguna vez se quiere brillo en los objetos, primero hay que agregar un `Light2D` **Global** a la stage y recién después las puntuales — y hacerlo en las dos stages a la vez, porque el Stage 1 tampoco tiene.

**Todavía en el proyecto pero sin usar:** `CodeBlasterFight`, `CodeBlasterUI`, `CodeBlasterTarget`, `CodeBlasterEncounter` y `Assets/Data/Encuentros/Stage2_Print_01.asset`. No se borraron por si hay que volver atrás; ninguna escena los referencia.

> **Choque conocido:** la terminal usa ESC para salir y el `PauseMenu` también usa ESC para pausar. En el `Laboratorio` no hay `PauseMenu` así que no se nota, pero si la terminal se lleva a una stage real hay que resolverlo.

## Fase 3 — Flujo completo

- [ ] Playtest de punta a punta: Menú → Stage 1 → Stage 2 entero → final, anotando qué se siente mal
- [ ] Pantalla de resumen al terminar, leyendo del `ScoreTracker` (el `FirstTryRate` es el indicador que sostiene la fundamentación)

> El **selector de niveles con gating quedó cancelado** junto con las stages 3-6. El gating lo hacen las puertas del mapa: no se puede avanzar sin resolver la terminal de la zona. Es mejor que un menú porque el progreso se ve en el mundo en vez de en una lista.

## Fase 4 — Pulido profundo, por tandas

- [ ] Zonas 1-3 del mapa: diálogos reales de Kira, errores previstos bien escritos en cada `TerminalChallenge`, ajuste de dificultad, level design cuidado
- [ ] Zonas 4-6: lo mismo

Orden sugerido: las primeras zonas antes (conceptos más simples, valida el patrón de pulido más rápido).

## Fase 5 — Audio y remate final

- [ ] Música ambiente + efectos (disparo/impacto/acierto/error)
- [ ] Pulido de UI
  - [x] **Menú principal** (03/10/2026, adelantado a pedido de Kevin):
    - **Bug arreglado:** el canvas estaba en `Constant Pixel Size`, así que **a 1280×720 el botón Salir quedaba cortado**. Pasó a `Scale With Screen Size`, referencia 1920×1080, match 1 (por alto). A 1080p se ve idéntico; a 1366×768 (notebooks de liceo) ahora entra todo.
    - Botones (`MenuButtonFX`): se agrandan, el texto se pone verde terminal y aparece un `>` titilando. **El hover del mouse y la selección con teclado son lo mismo**, así nunca hay dos botones marcados. Para eso los botones se pasaron a escala 1 con su tamaño real y pivote al medio (estaban deformados con escala 2,47 × 0,85 y pivote arriba, y crecían para abajo), y los textos ahora son **hijos** de su botón.
    - `MenuSelection` en el Canvas: arranca con Jugar marcado y nunca se queda sin selección, así que flechas/WASD + Enter andan siempre.
    - `NeonFlicker` en el título: cada 2,5–6,5 s, una ráfaga de parpadeos y a veces un corrimiento de costado (interferencia).
    - `TerminalTypeIn` en el subtítulo: "APRENDE PYTHON" se escribe letra por letra después del fundido y queda un `_` titilando.
    - **Kira** (`MenuKira`, imagen de UI con los cuadros de `Scan` y `Walk_scan`): entra volando, pasea eligiendo puntos (prefiere los costados), se queda escaneando, **se acerca al botón que marcás**, y **sale disparada por la derecha al apretar Jugar** (`FlyAway` en el `onClick`). Va detrás del cartel en el orden de dibujo, así que cuando cruza por el medio pasa por atrás.
    - Todo usa tiempo sin escalar: si se vuelve al menú desde la pausa con `timeScale` 0, igual se mueve.
    - Quedaría para más adelante: música del menú y un sonido al seleccionar o apretar.
- [ ] Build final de Windows
- [ ] Bug bash general

## Qué cortar primero si el tiempo aprieta (en este orden)

1. Audio — ya está marcado como baja prioridad.
2. Variedad de enemigos — quedarse con un tipo reskineado.
3. **Zonas del mapa**: con menos conceptos cubiertos el juego sigue siendo jugable y defendible. Cortar por el final (funciones, listas) y quedarse con print, variables y condicionales, que es lo que un noveno año usa más.
4. Decoración y level design de las zonas tardías — que sean corredores simples.

> Ojo que esta lista mejoró con el cambio de plan: antes, cortar una stage significaba cortar una escena entera con todo su armado. Ahora cortar es **no agregar una zona más al mapa**, y el juego sigue cerrando solo. Es mucho menos riesgoso llegar justo de tiempo.

## Nota sobre el cronograma de 2 meses

Reescrita el 02/10/2026 con el cambio de plan. Antes esto decía "pasada rústica de las 6 stages + selector". Ahora el camino es más corto y más seguro: **el juego ya existe de punta a punta** (menú → Stage 1 → Stage 2 → terminal → puerta). Lo que falta es ancho, no sistemas.

Eso quiere decir que a partir de acá casi todo el trabajo es **contenido** — zonas de mapa y `TerminalChallenge` — que es trabajo predecible y que se puede cortar en cualquier momento sin romper nada. El riesgo de llegar con algo a medio terminar bajó muchísimo.
