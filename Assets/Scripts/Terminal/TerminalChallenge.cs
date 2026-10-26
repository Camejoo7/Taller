using System;
using UnityEngine;

/// <summary>
/// Una respuesta equivocada que ya sabemos que los alumnos van a dar, con el
/// error que tiraría Python de verdad y lo que Kira contesta.
///
/// Anticipar los errores comunes es lo que separa "incorrecto, probá de nuevo"
/// de una corrección que enseña algo. Si el alumno escribe Print con mayúscula,
/// merece enterarse de que Python distingue mayúsculas, no un cartel rojo.
/// </summary>
[Serializable]
public class WrongAnswer
{
    [Tooltip("Lo que el alumno escribe. Se compara igual que la respuesta buena.")]
    public string answer;

    [Tooltip("El error tal cual lo tiraría Python. Es la mitad del valor " +
             "educativo: aprender a leer un traceback es aprender a programar.")]
    public string pythonError;

    [TextArea(2, 4)]
    [Tooltip("Lo que dice Kira para explicar el error.")]
    public string kiraSays;
}

/// <summary>
/// El contenido de un desafío de terminal: qué se pregunta, qué línea hay que
/// completar, qué se acepta como buena y qué pasa con cada error previsto.
///
/// Es un ScriptableObject para que las preguntas se escriban desde el Inspector
/// y no haya que tocar código para agregar contenido, igual que el
/// <see cref="CodeBlasterEncounter"/>.
/// </summary>
[CreateAssetMenu(fileName = "Terminal_", menuName = "CodeBreak/Desafío de Terminal")]
public class TerminalChallenge : ScriptableObject
{
    [Header("Identidad (para el puntaje)")]
    public string challengeId;

    [Tooltip("Concepto que evalúa: print, variables, condicionales...")]
    public string concept = "print";

    public int stageNumber = 1;

    [Header("La consigna")]
    [TextArea(2, 4)]
    [Tooltip("Lo que la terminal le pide al jugador.")]
    public string prompt = "Completá la línea para que imprima Hola";

    [Header("La línea de código")]
    [TextArea(1, 6)]
    [Tooltip("Lo que va ANTES del hueco. Puede quedar vacío.\n" +
             "Puede tener varias líneas (Enter): la pantalla las numera y el " +
             "hueco queda en la línea donde caiga. Es lo que usa el jefe.")]
    public string codeBefore = "";

    [TextArea(1, 6)]
    [Tooltip("Lo que va DESPUÉS del hueco. También puede tener varias líneas.")]
    public string codeAfter = "(\"Hola\")";

    [Tooltip("Cuántos guiones bajos se muestran mientras el hueco está vacío.")]
    public int blankWidth = 5;

    [Header("Respuesta")]
    [Tooltip("Todo lo que se acepta como correcto. La primera es la canónica.")]
    public string[] acceptedAnswers = new string[] { "print" };

    [Tooltip("Python distingue mayúsculas, así que lo normal acá es que SÍ " +
             "importen. Destildarlo solo si la pregunta no es de sintaxis.")]
    public bool caseSensitive = true;

    [Header("Respuesta numérica (opcional)")]
    [Tooltip("Si está tildado, también vale CUALQUIER número mayor que " +
             "'numberAbove'. Para cuando el alumno elige el dato y el if decide.")]
    public bool acceptAnyNumberAbove = false;

    public float numberAbove = 50f;

    [Tooltip("Error para un número que no alcanza. {0} es el número.")]
    public string numberTooLowError = "(sin salida: {0} > 50 es False)";

    [TextArea(2, 4)]
    [Tooltip("Lo que dice Kira con un número que no alcanza. {0} es el número.")]
    public string numberTooLowKiraSays = "{0} no es mayor que 50: la condición dio False y el print no corrió.";

    [Header("Al acertar")]
    [Tooltip("Lo que el programa imprime. Se muestra como salida real de Python, " +
             "que es lo que hace que se sienta programar y no responder un test.")]
    public string expectedOutput = "Hola";

    [TextArea(2, 4)]
    public string kiraOnSuccess = "Ahí está. Eso es Python corriendo de verdad.";

    [Header("Al errar")]
    [Tooltip("Errores previstos, con su explicación. Se buscan antes del genérico.")]
    public WrongAnswer[] knownMistakes;

    [Tooltip("Error genérico para lo que no cayó en la lista de arriba. " +
             "{0} se reemplaza por lo que escribió el alumno.")]
    public string fallbackError = "SyntaxError: invalid syntax";

    [Tooltip("Opcional: error para cuando escribe una PALABRA suelta que no " +
             "está en la lista (sin comillas, sin números). En Python eso casi " +
             "siempre es un NameError. {0} es la palabra. Vacío = se usa el de arriba.")]
    public string fallbackErrorForWords = "";

    [TextArea(2, 4)]
    public string fallbackKiraSays = "Eso no es. Mirá bien la línea y probá otra vez.";

    /// <summary>La línea completa, con la respuesta del jugador puesta en el hueco.</summary>
    public string BuildLine(string answer)
    {
        return codeBefore + answer + codeAfter;
    }

    /// <summary>Si el código tiene varias líneas (los ejercicios del jefe).</summary>
    public bool IsMultiLine
    {
        get
        {
            return (codeBefore != null && codeBefore.Contains("\n")) ||
                   (codeAfter != null && codeAfter.Contains("\n"));
        }
    }

    /// <summary>El error para una respuesta que no estaba prevista.</summary>
    public string FallbackError(string answer)
    {
        string given = answer == null ? "" : answer.Trim();
        if (!string.IsNullOrEmpty(fallbackErrorForWords) && IsBareWord(given))
            return string.Format(fallbackErrorForWords, given);
        return string.Format(fallbackError, given);
    }

    /// <summary>Un nombre suelto, como los de las variables: letras, números y _.</summary>
    static bool IsBareWord(string s)
    {
        if (string.IsNullOrEmpty(s)) return false;
        if (!char.IsLetter(s[0]) && s[0] != '_') return false;
        for (int i = 1; i < s.Length; i++)
            if (!char.IsLetterOrDigit(s[i]) && s[i] != '_') return false;
        return true;
    }

    /// <summary>El hueco vacío, para mostrar antes de que escriba nada.</summary>
    public string Blank
    {
        get { return new string('_', Mathf.Max(1, blankWidth)); }
    }

    public bool IsCorrect(string answer)
    {
        if (acceptedAnswers == null || answer == null) return false;

        string given = answer.Trim();

        double number;
        if (acceptAnyNumberAbove && TryParseNumber(given, out number) && number > numberAbove)
            return true;

        for (int i = 0; i < acceptedAnswers.Length; i++)
        {
            string ok = acceptedAnswers[i];
            if (ok == null) continue;
            ok = ok.Trim();

            if (caseSensitive)
            {
                if (string.Equals(given, ok, StringComparison.Ordinal)) return true;
            }
            else
            {
                if (string.Equals(given, ok, StringComparison.OrdinalIgnoreCase)) return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Busca si lo que escribió es uno de los errores previstos. Devuelve null
    /// si no lo es, y ahí se usa el genérico.
    /// </summary>
    public WrongAnswer FindKnownMistake(string answer)
    {
        if (knownMistakes == null || answer == null) return null;

        string given = answer.Trim();

        for (int i = 0; i < knownMistakes.Length; i++)
        {
            WrongAnswer w = knownMistakes[i];
            if (w == null || w.answer == null) continue;

            // Los errores previstos se comparan siempre con mayúsculas y
            // minúsculas exactas: justamente queremos poder distinguir "Print"
            // de "print" para explicar por qué uno falla.
            if (string.Equals(given, w.answer.Trim(), StringComparison.Ordinal))
                return w;
        }

        return null;
    }

    /// <summary>
    /// Si la respuesta es un número que no alcanza, arma el error con ese
    /// número adentro. Null si no aplica.
    /// </summary>
    public WrongAnswer NumberTooLow(string answer)
    {
        double number;
        if (!acceptAnyNumberAbove || answer == null || !TryParseNumber(answer.Trim(), out number))
            return null;

        string shown = answer.Trim();
        return new WrongAnswer
        {
            answer = shown,
            pythonError = string.Format(numberTooLowError, shown),
            kiraSays = string.Format(numberTooLowKiraSays, shown)
        };
    }

    /// <summary>Un número como lo escribe Python: punto decimal, sin comillas ni comas.</summary>
    static bool TryParseNumber(string s, out double number)
    {
        return double.TryParse(s, System.Globalization.NumberStyles.AllowLeadingSign | System.Globalization.NumberStyles.AllowDecimalPoint,
                               System.Globalization.CultureInfo.InvariantCulture, out number);
    }
}
