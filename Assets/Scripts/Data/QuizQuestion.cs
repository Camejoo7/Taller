using System;
using UnityEngine;

/// <summary>
/// Una pregunta de opción múltiple. Sirve para el modo Code Blaster
/// (cada opción es un fragmento de código flotante) y para el fallback
/// de "zona segura" con menú de opción múltiple.
/// Clase embebida: se edita dentro de StageData / CodeBlasterEncounter,
/// no es un asset propio.
/// </summary>
[Serializable]
public class QuizQuestion
{
    [TextArea(2, 4)]
    [Tooltip("Lo que pregunta Kira.")]
    public string question;

    [Tooltip("3 o 4 opciones: fragmentos de código o respuestas.")]
    public string[] options;

    [Tooltip("Índice dentro de 'options' de la respuesta correcta.")]
    public int correctIndex;

    [TextArea(2, 4)]
    [Tooltip("Lo que dice Kira cuando la pregunta se resuelve.")]
    public string explanation;
}
