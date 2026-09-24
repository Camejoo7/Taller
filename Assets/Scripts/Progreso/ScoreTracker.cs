using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

/// <summary>
/// Junta el resultado de todas las preguntas de la sesión y arma el informe.
///
/// Se crea solo al arrancar el juego y sobrevive a los cambios de escena, así
/// que no hay que acordarse de ponerlo en cada stage.
///
/// Guarda dos cosas distintas a propósito:
/// - El **puntaje del jugador**, que es lo que se le muestra a él.
/// - El **resumen por concepto**, que es para el docente: dice qué temas falló
///   el grupo, que es más útil para dar clase que una nota por alumno.
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
    /// Es el número que mejor separa saber de tantear.
    /// </summary>
    public float FirstTryRate
    {
        get { return attempts.Count == 0 ? 0f : (float)FirstTryCount / attempts.Count; }
    }

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

    /// <summary>
    /// El informe se guarda solo al cerrar el juego, para que el docente no
    /// dependa de que alguien se acuerde de pedirlo. En el Editor también corre
    /// al salir de Play, así que cada prueba deja su archivo.
    /// Si no hubo preguntas respondidas, SaveReport no escribe nada.
    /// </summary>
    void OnApplicationQuit()
    {
        SaveReport();
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

    /// <summary>Resumen agrupado por concepto — la parte que le sirve al docente.</summary>
    public List<ConceptSummary> SummaryByConcept()
    {
        Dictionary<string, ConceptSummary> map = new Dictionary<string, ConceptSummary>();

        foreach (QuestionAttempt a in attempts)
        {
            string key = string.IsNullOrEmpty(a.concept) ? "(sin concepto)" : a.concept;

            if (!map.ContainsKey(key))
            {
                ConceptSummary fresh = new ConceptSummary();
                fresh.concept = key;
                map[key] = fresh;
            }

            ConceptSummary s = map[key];
            s.questions++;
            s.shots += Mathf.Max(1, a.attempts);
            if (a.FirstTry) s.firstTry++;
        }

        return new List<ConceptSummary>(map.Values);
    }

    /// <summary>
    /// Arma el informe en CSV con punto y coma, que es lo que abre bien el Excel
    /// en español. Primero el resumen, después el detalle pregunta por pregunta.
    /// </summary>
    public string BuildReport()
    {
        StringBuilder sb = new StringBuilder();

        sb.AppendLine("CodeBreak - Informe de sesión");
        sb.AppendLine("Fecha;" + System.DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
        sb.AppendLine("Preguntas respondidas;" + TotalQuestions);
        sb.AppendLine("Acertadas de primera;" + FirstTryCount);
        sb.AppendLine("Porcentaje de primer intento;" + Mathf.RoundToInt(FirstTryRate * 100f) + "%");
        sb.AppendLine("Respuestas totales dadas;" + TotalShots);
        sb.AppendLine("Puntaje;" + TotalPoints);
        sb.AppendLine();

        sb.AppendLine("Resumen por concepto (para el docente)");
        sb.AppendLine("Concepto;Preguntas;Acertadas de primera;% primer intento;Respuestas totales");
        foreach (ConceptSummary s in SummaryByConcept())
        {
            sb.AppendLine(Clean(s.concept) + ";" + s.questions + ";" + s.firstTry + ";"
                          + Mathf.RoundToInt(s.FirstTryRate * 100f) + "%;" + s.shots);
        }
        sb.AppendLine();

        sb.AppendLine("Detalle");
        sb.AppendLine("Stage;Concepto;Pregunta;Intentos;Acertó de primera;Segundos;Puntos;Opciones equivocadas elegidas");
        foreach (QuestionAttempt a in attempts)
        {
            sb.AppendLine(a.stageNumber + ";" + Clean(a.concept) + ";" + Clean(a.question) + ";"
                          + a.attempts + ";" + (a.FirstTry ? "SÍ" : "NO") + ";"
                          + a.seconds.ToString("0.0") + ";" + a.Points + ";"
                          + Clean(string.Join(" | ", a.wrongChoices.ToArray())));
        }

        return sb.ToString();
    }

    /// <summary>
    /// Escribe el informe al disco y devuelve la ruta, o null si no pudo.
    /// Va a persistentDataPath porque es la única carpeta donde un juego ya
    /// instalado tiene permiso de escribir en Windows.
    /// </summary>
    public string SaveReport()
    {
        if (TotalQuestions == 0) return null;

        try
        {
            string folder = Path.Combine(Application.persistentDataPath, "Informes");
            Directory.CreateDirectory(folder);

            string file = "CodeBreak_" + System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".csv";
            string full = Path.Combine(folder, file);

            // UTF8 con BOM: sin esto el Excel en español rompe las tildes y las ñ.
            File.WriteAllText(full, BuildReport(), new UTF8Encoding(true));

            Debug.Log("CodeBreak: informe guardado en " + full);
            return full;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("CodeBreak: no se pudo guardar el informe — " + e.Message);
            return null;
        }
    }

    static string Clean(string value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        return value.Replace(";", ",").Replace("\n", " ").Replace("\r", " ");
    }

    public class ConceptSummary
    {
        public string concept;
        public int questions;
        public int firstTry;
        public int shots;

        public float FirstTryRate { get { return questions == 0 ? 0f : (float)firstTry / questions; } }
    }
}
