using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Qué clase de renglón es: cada uno se dibuja distinto en el papel.</summary>
public enum BookBlockKind
{
    Titulo,
    Etiqueta,
    Texto,
    Codigo,
    Salida,
    Error,
    Kira,
    Espacio
}

/// <summary>
/// Un pedazo de una página: un título, un párrafo, un bloque de código...
///
/// Las páginas se arman apilando bloques en vez de con campos fijos ("qué es",
/// "ejemplo", "error") porque la portada y algunos temas no tienen la misma
/// forma que el resto: la de condicionales lleva una tabla de signos, la de
/// variables una de tipos.
/// </summary>
[Serializable]
public class BookBlock
{
    public BookBlockKind kind = BookBlockKind.Texto;

    [TextArea(1, 8)]
    [Tooltip("Código, Salida y Error se muestran tal cual (sin etiquetas de " +
             "formato), así que ahí se puede escribir < y > sin miedo.\n" +
             "En Texto y Kira se puede usar <b>negrita</b>.\n" +
             "La fuente es ancha: el código entra hasta ~30 letras por renglón.")]
    public string text;

    public BookBlock() { }

    public BookBlock(BookBlockKind kind, string text)
    {
        this.kind = kind;
        this.text = text;
    }
}

/// <summary>Un tema del libro: ocupa el libro abierto, página izquierda y derecha.</summary>
[Serializable]
public class BookChapter
{
    [Tooltip("Tiene que ser igual al 'concept' de los TerminalChallenge de " +
             "ese tema (print, variables, condicionales, bucles, listas, " +
             "funciones). Así la terminal sabe a qué página mandar.")]
    public string id;

    [Tooltip("Cómo se nombra el tema en los avisos: 'la página de ___'.")]
    public string title;

    [Tooltip("Lo que dice la pestaña que asoma del costado del libro.")]
    public string tabLabel;

    [Tooltip("Si está tildado, viene escrita desde que Kira entrega el libro " +
             "(la portada). Las demás se desbloquean al llegar a su zona.")]
    public bool alwaysUnlocked;

    [Header("Aviso al desbloquearla (vacío = el de siempre)")]
    public string toastHeader;
    public string toastText;

    [Header("Páginas")]
    public List<BookBlock> left = new List<BookBlock>();
    public List<BookBlock> right = new List<BookBlock>();
}

/// <summary>
/// Todo lo que está escrito en el libro de Kira. Es un asset para que el texto
/// se corrija desde el Inspector sin tocar código: el contenido es lo que más
/// va a cambiar (revisión de Kevin, de su docente, de las pruebas con alumnos).
/// </summary>
[CreateAssetMenu(fileName = "LibroDeKira", menuName = "CodeBreak/Libro de Kira")]
public class KiraBookContent : ScriptableObject
{
    [Tooltip("En orden: la primera es la portada.")]
    public List<BookChapter> chapters = new List<BookChapter>();

    [Header("Avisos")]
    [Tooltip("Ojo con las mayúsculas acentuadas: la fuente dibuja la É, la " +
             "Ó... como si fueran minúsculas. Mejor 'Kira anotó' que 'KIRA ANOTÓ'.")]
    public string toastHeader = "Kira anotó:";
    public string toastHint = "TAB  abrir";

    [Tooltip("Lo que agrega Kira en la terminal cuando el alumno falla dos " +
             "veces. {0} es el título del tema.")]
    public string terminalHint = "¿Trabado? Apretá TAB: está en la página de {0}.";

    [Header("Pie del libro")]
    public string footerHint = "A D  pasar página        TAB  cerrar";

    public BookChapter Find(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        for (int i = 0; i < chapters.Count; i++)
            if (chapters[i] != null && string.Equals(chapters[i].id, id, StringComparison.OrdinalIgnoreCase))
                return chapters[i];
        return null;
    }
}
