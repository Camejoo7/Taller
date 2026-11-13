using System;
using System.Collections.Generic;
using UnityEngine;

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

[Serializable]
public class BookBlock
{
    public BookBlockKind kind = BookBlockKind.Texto;

    [TextArea(1, 8)]

    public string text;

    public BookBlock() { }

    public BookBlock(BookBlockKind kind, string text)
    {
        this.kind = kind;
        this.text = text;
    }
}

[Serializable]
public class BookChapter
{
    public string id;

    public string title;

    public string tabLabel;

    public bool alwaysUnlocked;

    [Header("Aviso al desbloquearla (vacío = el de siempre)")]
    public string toastHeader;
    public string toastText;

    [Header("Páginas")]
    public List<BookBlock> left = new List<BookBlock>();
    public List<BookBlock> right = new List<BookBlock>();
}

[CreateAssetMenu(fileName = "LibroDeKira", menuName = "CodeBreak/Libro de Kira")]
public class KiraBookContent : ScriptableObject
{
    public List<BookChapter> chapters = new List<BookChapter>();

    [Header("Avisos")]
    public string toastHeader = "Kira anotó:";
    public string toastHint = "TAB  abrir";

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
