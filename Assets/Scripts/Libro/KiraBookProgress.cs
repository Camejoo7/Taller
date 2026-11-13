using System;
using System.Collections.Generic;
using UnityEngine;

public static class KiraBookProgress
{
    static readonly HashSet<string> unlocked = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    static readonly HashSet<string> read = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    public static bool HasBook { get; private set; }

    public static bool HasNews { get; private set; }

    public static event Action<string> Unlocked;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetOnPlay()
    {
        unlocked.Clear();
        read.Clear();
        HasBook = false;
        HasNews = false;
        Unlocked = null;
    }

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

    static string[] savedUnlocked = new string[0];
    static string[] savedRead = new string[0];
    static bool savedHasBook, savedHasNews;

    public static void Snapshot()
    {
        savedUnlocked = new string[unlocked.Count]; unlocked.CopyTo(savedUnlocked);
        savedRead = new string[read.Count]; read.CopyTo(savedRead);
        savedHasBook = HasBook;
        savedHasNews = HasNews;
    }

    public static void RestoreSnapshot()
    {
        unlocked.Clear(); unlocked.UnionWith(savedUnlocked);
        read.Clear(); read.UnionWith(savedRead);
        HasBook = savedHasBook;
        HasNews = savedHasNews;
    }

    public static void ResetAll()
    {
        unlocked.Clear();
        read.Clear();
        HasBook = false;
        HasNews = false;
        Snapshot();
    }

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
