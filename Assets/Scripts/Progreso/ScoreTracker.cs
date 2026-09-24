using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Junta el resultado de todas las preguntas de la sesión y lleva el puntaje.
///
/// Vive solo en memoria: no escribe archivos ni guarda nada en el disco. Si en
/// algún momento hace falta un informe para el docente, se arma leyendo de acá.
///
/// Se crea solo al arrancar el juego y sobrevive a los cambios de escena, así
/// que no hay que acordarse de ponerlo en cada stage.
/// </summary>
public class ScoreTracker : MonoBehaviour
{
    public static ScoreTracker Instance { get; private set; }

    private readonly List<QuestionAttempt> attempts = new List<QuestionAttempt>();

    public IList<QuestionAttempt> Attempts { get { return attempts.AsReadOnly(); } }

    public int TotalQuestions { get { return attempts.Count; } }

    public int TotalPoints
    {
        get
        {
            int sum = 0;
            foreach (QuestionAttempt a in attempts) sum += a.Points;
            return sum;
        }
    }

    public int FirstTryCount
    {
        get
        {
            int n = 0;
            foreach (QuestionAttempt a in attempts) if (a.FirstTry) n++;
            return n;
        }
    }

    /// <summary>
    /// El indicador principal: qué porcentaje de preguntas acertó de primera.
    /// Es el número que mejor separa saber de tantear, porque completar el juego
    /// no prueba nada — disparándole a todos los fragmentos se acierta igual.
    /// </summary>
    public float FirstTryRate
    {
        get { return attempts.Count == 0 ? 0f : (float)FirstTryCount / attempts.Count; }
    }

    /// <summary>Respuestas dadas en total, contando las equivocadas.</summary>
    public int TotalShots
    {
        get
        {
            int n = 0;
            foreach (QuestionAttempt a in attempts) n += Mathf.Max(1, a.attempts);
            return n;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap()
    {
        if (Instance != null) return;

        GameObject go = new GameObject("ScoreTracker");
        Instance = go.AddComponent<ScoreTracker>();
        DontDestroyOnLoad(go);
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void Record(QuestionAttempt attempt)
    {
        if (attempt == null) return;
        attempts.Add(attempt);
    }

    public void ResetSession()
    {
        attempts.Clear();
    }
}
