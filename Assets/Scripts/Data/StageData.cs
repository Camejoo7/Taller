using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Todo el contenido de una stage en un solo asset. Duplicar una stage
/// es duplicar este asset y cambiar datos, no código.
/// </summary>
[CreateAssetMenu(fileName = "StageData", menuName = "CodeBreak/Stage Data")]
public class StageData : ScriptableObject
{
    [Header("Identidad")]
    [Tooltip("Número de stage. Lo usa el selector de niveles para el gating.")]
    public int stageNumber;

    [Tooltip("Título mostrable, ej: 'Stage 1: Información general de Python'.")]
    public string stageTitle;

    [Tooltip("Nombre de la escena a cargar para esta stage.")]
    public string sceneName;

    [TextArea(2, 4)]
    [Tooltip("Qué enseña la stage. Nota interna, no se muestra al jugador.")]
    public string conceptSummary;

    [Header("Diálogo")]
    [Tooltip("Diálogo de entrada con Kira. Reemplaza el hardcode de DialogueManager.Awake.")]
    public List<DialogueLine> introDialogue = new List<DialogueLine>();

    [Tooltip("Si el dron de Kira sube desde abajo durante el diálogo de intro.")]
    public bool introAnimatesKiraEntrance;

    [Tooltip("Diálogo de media-stage. Reemplaza DialogueManager2.")]
    public List<DialogueLine> midDialogue = new List<DialogueLine>();

    [Tooltip("Diálogo de cierre, antes de pasar a la siguiente stage.")]
    public List<DialogueLine> outroDialogue = new List<DialogueLine>();

    [Header("Contenido educativo")]
    [Tooltip("Encuentros Code Blaster de la stage.")]
    public List<CodeBlasterEncounter> encounters = new List<CodeBlasterEncounter>();

    [Tooltip("Preguntas para el modo seguro (fallback del roadmap: combate simple + preguntas en zona segura).")]
    public List<QuizQuestion> fallbackQuizzes = new List<QuizQuestion>();

    [Header("Progresión")]
    [Tooltip("Siguiente stage para el gating del selector. Null si es la última.")]
    public StageData nextStage;
}
