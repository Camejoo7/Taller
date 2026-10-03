using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Escribe un texto letra por letra y deja un cursor "_" titilando al final,
/// como si lo estuviera tipeando alguien en una terminal.
///
/// Usa maxVisibleCharacters en vez de ir armando el string: el texto completo
/// ya está maquetado desde el principio, así que no se corre ni se reacomoda
/// mientras aparece.
/// </summary>
[RequireComponent(typeof(TMP_Text))]
public class TerminalTypeIn : MonoBehaviour
{
    [Tooltip("Espera antes de empezar. El menú arranca con un fundido de 1 s: " +
             "si escribe durante el fundido, nadie lo ve.")]
    public float startDelay = 0.9f;

    public float charDelay = 0.07f;

    [Header("Cursor")]
    public bool showCursor = true;
    public string cursorChar = "_";
    public float blinkPeriod = 0.5f;

    private TMP_Text text;
    private string full;
    private bool typed;

    void Awake()
    {
        text = GetComponent<TMP_Text>();
        full = text.text;
    }

    void OnEnable()
    {
        typed = false;
        text.text = full;
        text.maxVisibleCharacters = 0;
        StartCoroutine(Type());
    }

    IEnumerator Type()
    {
        yield return new WaitForSecondsRealtime(startDelay);

        for (int i = 1; i <= full.Length; i++)
        {
            text.maxVisibleCharacters = i;
            // Los espacios no "suenan": pasan sin pausa, como al tipear de verdad.
            if (full[i - 1] != ' ') yield return new WaitForSecondsRealtime(charDelay);
        }

        text.maxVisibleCharacters = 99999;
        typed = true;
    }

    void Update()
    {
        if (!typed || !showCursor) return;

        // El cursor va siempre en el string y se esconde con alfa 0, en vez de
        // agregarlo y sacarlo: así el texto no cambia de ancho al titilar.
        bool on = Mathf.Repeat(Time.unscaledTime, blinkPeriod * 2f) < blinkPeriod;
        string wanted = full + (on ? cursorChar : "<alpha=#00>" + cursorChar);
        if (text.text != wanted) text.text = wanted;
    }
}
