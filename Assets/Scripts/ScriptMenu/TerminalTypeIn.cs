using System.Collections;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Text))]
public class TerminalTypeIn : MonoBehaviour
{
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

            if (full[i - 1] != ' ') yield return new WaitForSecondsRealtime(charDelay);
        }

        text.maxVisibleCharacters = 99999;
        typed = true;
    }

    void Update()
    {
        if (!typed || !showCursor) return;

        bool on = Mathf.Repeat(Time.unscaledTime, blinkPeriod * 2f) < blinkPeriod;
        string wanted = full + (on ? cursorChar : "<alpha=#00>" + cursorChar);
        if (text.text != wanted) text.text = wanted;
    }
}
