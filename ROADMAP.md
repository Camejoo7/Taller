# CodeBreak — Roadmap de Desarrollo

> Generado a partir de una sesión de planificación con Kevin. Actualizalo a medida que el proyecto avanza — es un mapa, no una ley. Si algo cambia, editá este archivo y contáselo a Claude Code al arrancar la sesión siguiente.

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

**Fallback si se complica:** separar en dos momentos dentro de la misma escena — primero una zona de combate simple (esquivar/disparar sin preguntas de por medio), después una zona segura donde Kira pregunta con el menú de opción múltiple que ya existe. Mismo contenido educativo, mucha menos ingeniería.

## Fase 0 — Fundaciones (antes de tocar contenido de cualquier stage)

No es opcional ni se puede saltar — todo lo demás se apoya en esto.

1. **Git**: `git init`, `.gitignore` de Unity, primer commit.
2. **CLAUDE.md actualizado** con el flujo de trabajo cerrado (ver archivo adjunto).
3. **ScriptableObjects de contenido**: `StageData`, `DialogueLine`, `QuizQuestion`, `CodeBlasterEncounter`. Esto es lo que permite después duplicar stages cambiando datos, no código.
4. **DialogueManager unificado**: fusionar `DialogueManager` + `DialogueManager2` en un solo manager que lea de `StageData`, en vez de diálogo hardcodeado en `Awake()`.
5. **Arreglar bugs pendientes**: `PausaCanvas` que no renderiza durante el gameplay, y el bloqueo del botón Slice en el Tile Palette por el importador de Aseprite.
6. **Sistema de proyectiles + CodeBlasterTarget**: el jugador dispara, el proyectil detecta contra qué fragmento de código pegó, dispara el evento correcto/incorrecto.

## Fase 1 — Stage 1 como plantilla completa

**Objetivo:** que el Stage 1 sea jugable de punta a punta con todos los sistemas nuevos probados — movimiento, Code Blaster, diálogo unificado, transición de escena. Este stage es el molde que después se duplica para el resto.

Stage 1 enseña información general de Python, no sintaxis todavía, así que las preguntas de Code Blaster acá pueden ser conceptuales ("¿qué es Python?", "¿para qué sirve programar?") — más simples de escribir que las técnicas de las stages siguientes. Buen lugar para probar el sistema antes de que el contenido se ponga más denso.

## Fase 2 — Pasada horizontal: versión rústica de Stages 2 a 6

Con el molde de Stage 1 probado, se arma una versión mínima jugable de cada stage restante: nivel con mapa ASCII convertido a tilemap, un par de enemigos, un `StageData` con contenido placeholder (2-3 preguntas alcanza por ahora).

El objetivo de esta fase NO es que quede pulido — es que el juego completo exista y se pueda jugar de principio a fin, aunque cada stage tenga poco contenido todavía. Esto da algo mostrable mucho antes, y deja ver los 6 stages en contexto antes de invertir tiempo puliendo cualquiera en particular.

Mapeo rápido de concepto → tipo de pregunta Code Blaster por stage:

| Stage | Concepto | Ejemplo de pregunta Code Blaster |
|---|---|---|
| 2 | print, variables, tipos | "¿Cuál imprime el texto correctamente?" — opciones con comillas mal puestas, `print` mal escrito |
| 3 | condicionales | "¿Cuál es la sintaxis correcta de un if?" — falta `:`, indentación mal, `=` en vez de `==` |
| 4 | bucles | "¿Cuál bucle imprime del 1 al 5?" — variantes de `range()` mal usadas |
| 5 | listas y cadenas | "¿Cómo accedés al primer elemento?" — índices mal, sintaxis de slice mal |
| 6 | funciones | "¿Cuál define la función correctamente?" — falta `def`, paréntesis mal, falta `:` |

## Fase 3 — Selector de niveles y flujo completo

Menú de selección de stage con gating: no se puede entrar a la Stage N+1 sin haber completado la N. Como no hay guardado de sesión, alcanza con una variable `maxUnlockedStage` en un singleton que vive mientras el juego está abierto (`DontDestroyOnLoad`), sin persistencia en disco.

En este punto se juega el juego de punta a punta al menos una vez, Stage 1 a 6, anotando qué se siente mal — ritmo, dificultad, algo que se rompe.

## Fase 4 — Pulido profundo, por tandas

Se vuelve a cada stage y se le mete contenido real: diálogos de Kira bien escritos, más variedad de preguntas Code Blaster, ajuste de dificultad de enemigos, level design más cuidado.

Orden sugerido: Stages 2-3 primero (conceptos más simples, valida el patrón de pulido más rápido), después 4-6.

## Fase 5 — Audio y remate final

Música ambiente, efectos de disparo/impacto/acierto/error, pulido de UI, build final de Windows, bug bash general.

## Qué cortar primero si el tiempo aprieta (en este orden)

1. Audio — ya está marcado como baja prioridad.
2. Variedad de enemigos por stage — quedarse con un tipo reskineado.
3. Cantidad de preguntas Code Blaster por stage — bajar a lo mínimo que cubra el concepto.
4. La mecánica mezclada Code Blaster completa → fallback al modo separado (combate simple + preguntas en zona segura).
5. Selector de niveles con gating → linealizar directamente, sin menú de selección.

## Nota sobre el cronograma de 2 meses

Si se empuja fuerte, las Fases 0 a 3 (fundaciones + Stage 1 + pasada rústica de todo + selector) entran cómodo en las primeras 3-4 semanas — eso es el objetivo original de "un mes". Eso deja las 4-5 semanas restantes como colchón real para las Fases 4 y 5, que es donde el juego pasa de "funciona" a "es bueno de jugar". Ese colchón es lo que evita llegar corriendo al final.
