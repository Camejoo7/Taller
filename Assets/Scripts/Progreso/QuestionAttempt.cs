using System;
using System.Collections.Generic;

/// <summary>
/// El registro de como le fue al jugador en UNA pregunta.
///
/// La clave de todo el sistema es <see cref="attempts"/>: completar el juego no
/// prueba nada, porque tarde o temprano se le pega al fragmento correcto. Lo que
/// distingue saber de tantear es cuantos disparos hicieron falta. Acertar de
/// primera es conocimiento; acertar al tercero es descarte.
/// </summary>
[Serializable]
public class QuestionAttempt
{
    public string encounterId;

    /// <summary>Concepto que evalúa la pregunta (print, condicionales, bucles...).</summary>
    public string concept;

    public int stageNumber;
    public string question;

    /// <summary>Respuestas dadas hasta acertar, contando la correcta.</summary>
    public int attempts;

    public float seconds;

    /// <summary>Texto de las opciones equivocadas que eligió, en orden.</summary>
    public List<string> wrongChoices = new List<string>();

    public bool FirstTry { get { return attempts <= 1; } }

    /// <summary>
    /// Puntaje de la pregunta. La tabla es a propósito simple y explicable:
    /// premia acertar de primera y castiga el tanteo, sin llegar nunca a cero
    /// (responder mal y corregirse también es aprender).
    /// </summary>
    public int Points
    {
        get
        {
            if (attempts <= 1) return 100;
            if (attempts == 2) return 50;
            if (attempts == 3) return 25;
            return 10;
        }
    }
}
