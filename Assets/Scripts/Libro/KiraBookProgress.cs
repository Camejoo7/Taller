using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Qué páginas del libro ya escribió Kira. Vive en memoria, igual que el
/// <see cref="ScoreTracker"/>: sobrevive a los cambios de escena y a morir
/// (que recarga la escena), y arranca de cero cada vez que se abre el juego.
/// </summary>
public static class KiraBookProgress
{
    static readonly HashSet<string> unlocked = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    static readonly HashSet<string> read = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>Si el jugador ya tiene el libro. Se lo da Kira en el Stage 1.</summary>
    public static bool HasBook { get; private set; }

    /// <summary>
    /// Si Kira escribió algo desde la última vez que se abrió el libro. El
    /// cartel de "TAB LIBRO" solo aparece mientras esto es true: avisa que hay
    /// algo nuevo, no que el libro existe.
    /// </summary>
    public static bool HasNews { get; private set; }

    /// <summary>Avisa cuando se escribe una página nueva (para el cartelito).</summary>
    public static event Action<string> Unlocked;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetOnPlay()
    {
        // Por si el Editor tiene apagada la recarga de dominio al darle Play:
        // sin esto, las páginas de la partida anterior seguirían escritas.
        unlocked.Clear();
        read.Clear();
        HasBook = false;
        HasNews = false;
        Unlocked = null;
    }

    /// <summary>
    /// Escribe una página (y entrega el libro si todavía no lo tenía, así se
    /// puede probar el Stage 2 sin pasar por el Stage 1). Devuelve false si
    /// ya estaba escrita.
    /// </summary>
    public static bool Unlock(string id)
    {
        bool gaveBook = !HasBook;
        HasBook = true;

        bool added = !string.IsNullOrEmpty(id) && unlocked.Add(id);
        if (!added && !gaveBook) return false;

        HasNews = true;
        if (Unlocked != null) Unlocked(id);
        return true;
    }

    /// <summary>
    /// Escribe varias páginas sin avisar. Es para probar el juego (CodeBreak →
    /// Probar el jefe): con Unlock saldrían todos los avisos en fila.
    /// </summary>
    public static void UnlockQuietly(IEnumerable<string> ids)
    {
        HasBook = true;
        foreach (string id in ids) unlocked.Add(id);
    }

    /// <summary>El jugador abrió el libro: lo nuevo ya lo vio.</summary>
    public static void ClearNews()
    {
        HasNews = false;
    }

    public static bool IsUnlocked(BookChapter chapter)
    {
        if (chapter == null || !HasBook) return false;
        return chapter.alwaysUnlocked || unlocked.Contains(chapter.id);
    }

    public static bool IsRead(BookChapter chapter)
    {
        return chapter != null && read.Contains(chapter.id);
    }

    public static void MarkRead(BookChapter chapter)
    {
        if (chapter != null && !string.IsNullOrEmpty(chapter.id)) read.Add(chapter.id);
    }
}
