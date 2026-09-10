# CodeBreak — Roadmap de Desarrollo

> Generado a partir de una sesión de planificación con Kevin. Actualizalo a medida que el proyecto avanza — es un mapa, no una ley. Si algo cambia, editá este archivo y contáselo a Claude Code al arrancar la sesión siguiente.

## 📍 Dónde estamos ahora

**Fase actual: Fase 0 — Fundaciones** (5 de 6 ítems completos)

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

## La mecánica central: Code Blaster

Un dron o enemigo aparece en pantalla y muestra 3-4 fragmentos de código flotando a su alrededor, como si fueran las balas que vienen hacia el jugador. Kira hace una pregunta sobre el concepto de la stage, y el jugador le dispara al fragmento que la responde correctamente. Acertar destruye al enemigo. Errar le hace daño al jugador o el fragmento rebota.

**Por qué funciona para este proyecto:** reutiliza el `EnemySpawner` / `EnemyAI` que ya existen, no requiere evaluar código Python real (eso sería construir un intérprete, un proyecto en sí mismo), y el combate ES la verificación de aprendizaje — no son dos sistemas separados compitiendo por el tiempo de desarrollo.

**Fallback si se complica:** separar en dos momentos dentro de la misma escena — primero una zona de combate simple (esquivar/disparar sin preguntas de por medio), después una zona segura donde Kira pregunta con el menú de opción múltiple que ya existe (`fallbackQuizzes` en `StageData`). Mismo contenido educativo, mucha menos ingeniería.

## Fase 0 — Fundaciones (antes de tocar contenido de cualquier stage)

No es opcional ni se puede saltar — todo lo demás se apoya en esto.

- [x] **Git**: `git init`, `.gitignore` de Unity, primer commit y push a origin/main.
- [x] **CLAUDE.md actualizado** con el flujo de trabajo cerrado.
- [x] **ScriptableObjects de contenido**: `StageData`, `DialogueLine`, `QuizQuestion`, `CodeBlasterEncounter`. Scripts creados y compilando (sin `.assets` todavía).
- [x] **DialogueManager unificado**: `DialogueManager` + `DialogueManager2` fusionados en un solo `DialogueManager` con `enum DialogueSegment { Intro, Mid, Outro }`. Lee el contenido de un `StageData` (`introDialogue` / `midDialogue` / `outroDialogue`), ya no hardcodeado en `Awake()`. Intro congela al jugador + anima el dron + avance manual + handoff a `KiraFollower`; Mid/Outro sin congelar y solo auto-avance. Speaker por `enum` → "KIRA" cyan / "NX-7" amarillo. `KiraTriggerZone` y `EnemySpawner` actualizados; `DialogueManager2` borrado. Creado `Assets/Data/Stage1.asset` con las 5 líneas de intro + 4 de media migradas. Verificado en Play mode. **Cuadro de diálogo restilado** en `SampleScene` para combinar con el resto del juego: se sacó el borde naranja duro, panel HUD azul oscuro (`#141C2E`) con línea de acento cyan arriba, nombre del hablante en una placa (`FrameMap_7`) arriba a la izquierda, hint "ESPACIO" abajo a la derecha. Fuente: nombre y cuerpo en `PressStart2P` (la del juego, `CyberpunkCraftpixPixel` no tiene acentos ni `¿¡` y renderiza mal K/N/X). Como el `DialogueCanvas` es único, lo heredan las 6 stages.
- [x] **Arreglar bug del `PausaCanvas`**: el menú de pausa nunca se había terminado de armar (panel de 2×2 px, 9 botones duplicados sin texto ni `onClick`, título a escala 0.02). Rearmado desde cero en `SampleScene` con el kit visual del Menú Principal: panel `FrameMap_9` (tinte `#2A3450`), botones `FrameMap_7`, fuente `CyberpunkCraftpixPixel SDF`, texto `#C0C0C0`, dimmer al 55%. Botones: Reanudar / Reiniciar / Menú Principal, con `onClick` persistentes en la escena (`Reanudar` / `Reiniciar` / `IrAlMenu`). `PauseMenu.cs` suma `Reiniciar()` (recarga la escena activa). Verificado en Play mode. — El otro bug (Slice / Aseprite en el Tile Palette) se movió a Fase 2, ver abajo.
- [ ] **Sistema de proyectiles + CodeBlasterTarget**: el jugador dispara, el proyectil detecta contra qué fragmento de código pegó, dispara el evento correcto/incorrecto. — *Kevin lo ve como mecánica que recién aparece en Stage 2; a definir si se construye acá como fundación o se mueve a Fase 2.*

## Fase 1 — Stage 1 como plantilla completa

- [ ] Stage 1 jugable de punta a punta con todos los sistemas nuevos probados: movimiento, Code Blaster, diálogo unificado (vía `StageData`), transición de escena.

**Objetivo:** que el Stage 1 sea el molde que después se duplica para el resto. Stage 1 enseña información general de Python, no sintaxis todavía, así que las preguntas de Code Blaster acá pueden ser conceptuales ("¿qué es Python?", "¿para qué sirve programar?") — más simples de escribir que las técnicas de las stages siguientes. Buen lugar para probar el sistema antes de que el contenido se ponga más denso.

## Fase 2 — Pasada horizontal: versión rústica de Stages 2 a 6

- [ ] Stage 2 — nivel rústico + `StageData` placeholder (print, variables, tipos)
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
