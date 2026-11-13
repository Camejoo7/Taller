using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class DialogueMoment
{
    public string id;

    public string title;

    public bool freezePlayer = true;

    public bool kiraEntrance;

    public List<DialogueLine> lines = new List<DialogueLine>();
}

[CreateAssetMenu(fileName = "StageData", menuName = "CodeBreak/Stage Data")]
public class StageData : ScriptableObject
{
    [Header("Identidad")]
    public int stageNumber;

    public string stageTitle;

    public string sceneName;

    [TextArea(2, 4)]

    public string conceptSummary;

    [Header("Diálogos del juego (momentos)")]
    public List<DialogueMoment> moments = new List<DialogueMoment>();

    [Header("Diálogo (sistema viejo, sin usar)")]
    public List<DialogueLine> introDialogue = new List<DialogueLine>();

    public bool introAnimatesKiraEntrance;

    public List<DialogueLine> midDialogue = new List<DialogueLine>();

    public List<DialogueLine> outroDialogue = new List<DialogueLine>();

    [Header("Contenido educativo")]
    public List<CodeBlasterEncounter> encounters = new List<CodeBlasterEncounter>();

    public List<QuizQuestion> fallbackQuizzes = new List<QuizQuestion>();

    [Header("Progresión")]
    public StageData nextStage;

    public DialogueMoment FindMoment(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        for (int i = 0; i < moments.Count; i++)
            if (moments[i] != null && moments[i].id == id) return moments[i];
        return null;
    }
}
