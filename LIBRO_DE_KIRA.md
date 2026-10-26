# El libro de Kira — borrador de las páginas

> **Estado:** borrador escrito por Claude el 16/10/2026. **Falta que Kevin lo revise** (o se lo muestre a su docente). Es el primer ítem del libro en la Fase 2B del `ROADMAP.md`.
>
> ⚠️ **26/10/2026: el libro ya está en el juego, y el texto que vale ahora es el de `Assets/Data/Libro/LibroDeKira.asset`** (se corrige desde el Inspector, sin programar). Este archivo queda como borrador original. Diferencias: los subtítulos van en minúscula ("Qué es": la fuente dibuja la É mayúscula como minúscula), y el ejemplo de funciones usa `saludar("Central City")` en vez de `saludar("KIRA")`, porque `"KIRA"` es justo la respuesta de la terminal de funciones.
>
> Todo lo que está acá es texto para cambiar: tono de Kira, largo, ejemplos, qué error se muestra en cada página.

## Cómo está armado cada tema

El libro se ve **abierto, con dos páginas** (el pack trae la tapa y una página izquierda y una derecha por separado). Cada tema ocupa las dos:

| Página izquierda | Página derecha |
|---|---|
| **Título** | **Imprime:** lo que sale en pantalla |
| **Qué es:** 2-3 frases | **Ojo con esto:** el error más típico, con el mensaje real de Python y por qué pasa |
| **Ejemplo:** código corto | **Kira:** un comentario suyo (humor + un dato útil) |

Reglas que se respetaron (de la Fase 2B):
- **El ejemplo nunca es la respuesta de la terminal.** Se parece, pero hay que trasladar la idea. La única excepción inevitable es `print`: la página de print tiene que mostrar la palabra print, y esa terminal es la de calentamiento.
- **Líneas de código de 30 caracteres o menos.** La fuente del juego (PressStart2P) es muy ancha: en un tamaño que se lea bien entran ~30 letras por renglón en cada página.
- Los mensajes de error son los **reales de Python 3.10+**, escritos igual que en las terminales del juego.

---

## Portada — se entrega en el Stage 1

**Idea para la historia (propuesta, a confirmar):** NEXCORP controla todo lo digital, así que Kira no puede guardar nada en el implante sin que la rastreen. NX-7 encuentra una libreta vieja en la cueva y Kira le dicta. Eso explica dos cosas: por qué un libro de **papel** en el 2077 (y que el pack sea de cuero y papel deja de ser un problema de estilo para ser parte de la historia) y por qué **las páginas se van llenando de a una**: Kira dicta cada tema cuando llegan a su zona.

Nombre posible: *El cuaderno*, *Manual prohibido*, *Libro de Kira*.

**Izquierda — ¿Qué es programar?**

Programar es darle instrucciones a una máquina, en un idioma que entienda. Python es uno de esos idiomas: se lee casi como inglés y se usa para páginas web, inteligencia artificial, ciencia y videojuegos.

**Derecha — Tres reglas que valen para todo el libro**

1. Python distingue mayúsculas: `print` y `Print` son cosas distintas.
2. El texto va entre comillas: `"hola"`. Sin comillas, Python cree que es el nombre de algo.
3. Cuando algo falla, Python te dice qué pasó. Leé el error: es una pista, no un reto.

**Kira:** "Papel. En el 2077. Lo único que NEXCORP no puede escanear. Yo dicto, vos anotás. Y con letra prolija, que después lo tengo que leer yo."

---

## 1 · print — zona 1

**Izquierda**

**Qué es:** `print` muestra algo en la pantalla. Lo que pongas entre los paréntesis, sale impreso. Si ponés varias cosas separadas por comas, las imprime en fila con un espacio en el medio.

**Ejemplo:**
```python
print("Hola, NX-7")
print(51)
print("Años:", 51)
```

**Derecha**

**Imprime:**
```
Hola, NX-7
51
Años: 51
```

**Ojo con esto:**
```python
Print("Hola")
```
```
NameError: name 'Print' is not defined
```
Con P mayúscula, Python no la reconoce. `print` va todo en minúscula.

**Kira:** "Es la primera palabra que aprende todo programador del planeta. Hasta yo empecé por acá, y eso que soy una IA brillante."

---

## 2 · Variables y tipos — zona 2

**Izquierda**

**Qué es:** una variable es una caja con nombre donde guardás un dato. El `=` guarda: lo de la derecha entra en la caja de la izquierda. Después, usás el nombre y Python busca lo que hay adentro.

**Ejemplo:**
```python
nombre = "KIRA"
edad = 51
print(nombre, edad)
```

**Los tipos de dato:**
- `"KIRA"` → texto (`str`): entre comillas
- `51` → número entero (`int`): sin comillas
- `3.5` → número con decimales (`float`): con punto, no con coma
- `True` / `False` → verdadero o falso (`bool`)

**Derecha**

**Imprime:**
```
KIRA 51
```

**Ojo con esto:**
```python
nombre = KIRA
```
```
NameError: name 'KIRA' is not defined
```
Sin comillas, Python cree que KIRA es el nombre de **otra variable**, y esa no existe. El texto, siempre entre comillas.

**Kira:** "El `=` no es el igual de matemática: quiere decir 'guardá esto acá'. Por eso en Python `vidas = vidas + 1` tiene todo el sentido del mundo."

---

## 3 · Condicionales — zona 3

**Izquierda**

**Qué es:** con `if` el programa decide. Si la condición es verdadera (`True`), corre lo que está adentro. Si es falsa (`False`), lo saltea. Con `else` le decís qué hacer en el otro caso.

**Ejemplo:**
```python
temperatura = 35
if temperatura > 30:
    print("Hace calor")
else:
    print("Está fresco")
```

**Para comparar:**
| Signo | Quiere decir |
|---|---|
| `>` | mayor que |
| `<` | menor que |
| `==` | igual a |
| `!=` | distinto de |
| `>=` | mayor o igual |
| `<=` | menor o igual |

**Derecha**

**Imprime:**
```
Hace calor
```

**Ojo con esto:**
```python
if temperatura = 30:
```
```
SyntaxError: invalid syntax. Maybe you meant '==' or ':=' instead of '='?
```
Un solo `=` **guarda**. Para preguntar si son iguales van dos: `==`. Y no te olvides los dos puntos `:` al final ni los espacios al principio de la línea de adentro: así Python sabe qué está dentro del `if`.

**Kira:** "Fijate que `30 > 30` da False: 'mayor' no incluye al igual. Si querés que entre el 30, es `>=`. Las máquinas son literales, no les pidas que adivinen."

> Nota para Kevin: la terminal de esta zona es `energia = ___` con `if energia > 50`. El comentario de Kira prepara justo para la trampa del 50, sin darle el número.

---

## 4 · Bucles — zona 4

**Izquierda**

**Qué es:** un `for` repite instrucciones. Con `range(n)` se repite **n veces**, y la variable va contando las vueltas. El contador arranca en 0, así que nunca llega al número que pusiste.

**Ejemplo:**
```python
for vuelta in range(3):
    print("Salto", vuelta)
```

**Derecha**

**Imprime:**
```
Salto 0
Salto 1
Salto 2
```
Tres vueltas: 0, 1 y 2.

**Ojo con esto:**
```python
for vuelta in range(3):
print("Salto")
```
```
IndentationError: expected an indented block after 'for' statement on line 1
```
Lo que se repite va **corrido para adentro** (4 espacios). Sin eso, Python no sabe qué es parte del bucle.

**Kira:** "Sí, los programadores cuentan desde cero. No, no tiene mucho sentido. Sí, te vas a acostumbrar."

> Nota para Kevin: la terminal usa `range(4)` con nodos. Acá el ejemplo es `range(3)`, para que tenga que contar los nodos y trasladar la idea.

---

## 5 · Listas y cadenas — zona 5

**Izquierda**

**Qué es:** una lista guarda varios datos en orden, entre corchetes `[ ]` y separados por comas. Cada uno tiene un número de posición, el **índice**, y el índice arranca en 0. `len()` te dice cuántos hay.

**Ejemplo:**
```python
equipo = ["NX-7", "KIRA"]
print(equipo[0])
print(equipo[1])
print(len(equipo))
```

**Los textos también:** una cadena es una fila de letras y se cuenta igual.
```python
nombre = "KIRA"
print(nombre[0])
```
→ imprime `K`

**Derecha**

**Imprime:**
```
NX-7
KIRA
2
```

**Ojo con esto:**
```python
print(equipo[2])
```
```
IndexError: list index out of range
```
La lista tiene 2 elementos: sus índices son 0 y 1. El 2 no existe. **El último índice es siempre uno menos que la cantidad.**

**Kira:** "El primero es el 0, el segundo es el 1… Si una lista tiene 5 cosas, la última es la `[4]`. Anotalo. Ah, no, ya está anotado."

> Nota para Kevin: la terminal pide el cuarto contenedor (índice 3). El ejemplo usa una lista de 2 y la regla "uno menos que la cantidad", sin el caso de 4.

---

## 6 · Funciones — zona 6

**Izquierda**

**Qué es:** una función es un pedazo de código con nombre. La escribís una vez con `def` y la usás (la **llamás**) cuantas veces quieras. Lo que va entre paréntesis es el dato que recibe. Ojo: `def` solo la crea; no pasa nada hasta que la llamás.

**Ejemplo:**
```python
def saludar(nombre):
    print("Hola,", nombre)

saludar("NX-7")
saludar("KIRA")
```

**Derecha**

**Imprime:**
```
Hola, NX-7
Hola, KIRA
```

**Ojo con esto:**
```python
saludar()
```
```
TypeError: saludar() missing 1 required positional argument: 'nombre'
```
La función espera un dato (`nombre`) y no le pasaste ninguno. Lo que va entre paréntesis al definirla es lo que tenés que darle al llamarla.

**Kira:** "`print`, `len` y `range` también son funciones: alguien las escribió una vez y desde entonces todo el mundo las llama. Ahora vos también podés hacer las tuyas."

---

## El pack de arte: Complete UI Book Styles Pack (crusenho)

- En `Assets/ASSETS/Libro/` está la versión **gratis, "Travel Book Lite"**: un solo estilo, cuero rojo con esquinas doradas y páginas de papel crema. El pack completo trae además los estilos *Holo* y *Tablet*, que van más con el cyberpunk. Con la idea de la libreta de papel, el estilo Travel queda justificado por la historia.
- Piezas que sirven: `BookCover01a` (la tapa, de fondo), `BookPageLeft01a` y `BookPageRight01a` (las páginas), `IconArrow01a` (pasar de página), `IconCross01a` (cerrar), `Marker01a` (marcador o pestaña del tema), `Alert01a` animado (para avisar que hay una página nueva).
- Vienen para **9-slice**, así que se estiran sin deformarse.
- ⚠️ **La licencia pide crédito:** nombrar a *crusenho* o poner el link del pack. Hay que agregarlo a los créditos del juego.
