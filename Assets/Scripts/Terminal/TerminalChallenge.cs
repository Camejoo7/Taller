using System;
using UnityEngine;

[Serializable]
public class WrongAnswer
{
    public string answer;

    public string pythonError;

    [TextArea(2, 4)]

    public string kiraSays;
}

[CreateAssetMenu(fileName = "Terminal_", menuName = "CodeBreak/Desafío de Terminal")]
public class TerminalChallenge : ScriptableObject
{
    [Header("Identidad (para el puntaje)")]
    public string challengeId;

    public string concept = "print";

    public int stageNumber = 1;

    [Header("La consigna")]
    [TextArea(2, 4)]

    public string prompt = "Completá la línea para que imprima Hola";

    [Header("La línea de código")]
    [TextArea(1, 6)]

    public string codeBefore = "";

    [TextArea(1, 6)]

    public string codeAfter = "(\"Hola\")";

    public int blankWidth = 5;

    [Header("Respuesta")]
    public string[] acceptedAnswers = new string[] { "print" };

    public bool caseSensitive = true;

    [Header("Respuesta numérica (opcional)")]
    public bool acceptAnyNumberAbove = false;

    public float numberAbove = 50f;

    public string numberTooLowError = "(sin salida: {0} > 50 es False)";

    [TextArea(2, 4)]

    public string numberTooLowKiraSays = "{0} no es mayor que 50: la condición dio False y el print no corrió.";

    [Header("Al acertar")]
    public string expectedOutput = "Hola";

    [TextArea(2, 4)]
    public string kiraOnSuccess = "Ahí está. Eso es Python corriendo de verdad.";

    [Header("Al errar")]
    public WrongAnswer[] knownMistakes;

    public string fallbackError = "SyntaxError: invalid syntax";

    public string fallbackErrorForWords = "";

    [TextArea(2, 4)]
    public string fallbackKiraSays = "Eso no es. Mirá bien la línea y probá otra vez.";

    public string BuildLine(string answer)
    {
        return codeBefore + answer + codeAfter;
    }

    public bool IsMultiLine
    {
        get
        {
            return (codeBefore != null && codeBefore.Contains("\n")) ||
                   (codeAfter != null && codeAfter.Contains("\n"));
        }
    }

    public string FallbackError(string answer)
    {
        string given = answer == null ? "" : answer.Trim();
        if (!string.IsNullOrEmpty(fallbackErrorForWords) && IsBareWord(given))
            return string.Format(fallbackErrorForWords, given);
        return string.Format(fallbackError, given);
    }

    static bool IsBareWord(string s)
    {
        if (string.IsNullOrEmpty(s)) return false;
        if (!char.IsLetter(s[0]) && s[0] != '_') return false;
        for (int i = 1; i < s.Length; i++)
            if (!char.IsLetterOrDigit(s[i]) && s[i] != '_') return false;
        return true;
    }

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

    public WrongAnswer FindKnownMistake(string answer)
    {
        if (knownMistakes == null || answer == null) return null;

        string given = answer.Trim();

        for (int i = 0; i < knownMistakes.Length; i++)
        {
            WrongAnswer w = knownMistakes[i];
            if (w == null || w.answer == null) continue;

            if (string.Equals(given, w.answer.Trim(), StringComparison.Ordinal))
                return w;
        }

        return null;
    }

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

    static bool TryParseNumber(string s, out double number)
    {
        return double.TryParse(s, System.Globalization.NumberStyles.AllowLeadingSign | System.Globalization.NumberStyles.AllowDecimalPoint,
                               System.Globalization.CultureInfo.InvariantCulture, out number);
    }
}
