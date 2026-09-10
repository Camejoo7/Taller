using System.Collections;
using TMPro;
using UnityEngine;

public class DialogueManager2 : MonoBehaviour
{
    [Header("UI")]
    public GameObject dialogueCanvas;
    public TextMeshProUGUI speakerNameText;
    public TextMeshProUGUI dialogueText;

    [Header("Configuración")]
    public float typingSpeed = 0.03f;
    public float autoAdvanceTime = 4f;

    private string[] lines;
    private string[] speakers;
    private int currentLine = 0;
    private bool isTyping = false;
    private bool dialogueActive = false;

    void Awake()
    {
        speakers = new string[]
        {
            "KIRA",
            "KIRA",
            "...",
            "KIRA"
        };

        lines = new string[]
        {
            "¡Dron adelante, esquivalo!",
            "Vamos, movete. Mientras escapás de este infierno te cuento algo útil: Python es un lenguaje de programación creado en 1991. 35 años antes de mi creación, para que te des una idea de lo antiguo que es. ¿Y sabés qué? Sigue siendo relevante. Hay lenguajes más jóvenes que ya nadie usa.",
            "¿Por qué Python?",
            "Pregunta inteligente cuando te persiguen robots asesinos. Python es un lenguaje muy humano, en inglés, muy legible, lo que significa que podés entender lo que escribís."
        };
    }

    public void StartDialogue()
    {
        dialogueActive = true;
        currentLine = 0;
        dialogueCanvas.SetActive(true);
        ShowCurrentLine();
    }

    void ShowCurrentLine()
    {
        speakerNameText.text = speakers[currentLine];

        if (speakers[currentLine] == "KIRA")
            speakerNameText.color = new Color(0f, 1f, 1f);
        else
            speakerNameText.color = new Color(1f, 1f, 0f);

        StopCoroutine("TypeLine");
        StartCoroutine("TypeLine", lines[currentLine]);
    }

    IEnumerator TypeLine(string line)
    {
        isTyping = true;
        dialogueText.text = "";
        foreach (char c in line)
        {
            dialogueText.text += c;
            yield return new WaitForSeconds(typingSpeed);
        }
        isTyping = false;
        StartCoroutine(AutoAdvance());
    }

    IEnumerator AutoAdvance()
    {
        yield return new WaitForSeconds(autoAdvanceTime);
        AdvanceLine();
    }

    void AdvanceLine()
    {
        currentLine++;
        if (currentLine < lines.Length)
            ShowCurrentLine();
        else
            EndDialogue();
    }

    void EndDialogue()
    {
        dialogueActive = false;
        dialogueCanvas.SetActive(false);
    }
}