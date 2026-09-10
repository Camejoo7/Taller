using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Segmento de diálogo dentro de una stage. Define el comportamiento:
/// - Intro: congela al jugador, anima la entrada del dron de Kira (si el
///   StageData lo pide), permite avance manual, y al terminar arranca el
///   KiraFollower.
/// - Mid: no congela, sin animación de dron, solo avance automático.
/// - Outro: congela al jugador, solo avance automático.
/// </summary>
public enum DialogueSegment { Intro, Mid, Outro }

/// <summary>
/// Manager único de diálogo. Lee el contenido de un StageData
/// (intro / mid / outro) en vez de tenerlo hardcodeado.
/// Reemplaza al viejo par DialogueManager + DialogueManager2.
/// </summary>
public class DialogueManager : MonoBehaviour
{
    [Header("Contenido")]
    public StageData stageData;

    [Header("UI")]
    public GameObject dialogueCanvas;
    public TextMeshProUGUI speakerNameText;
    public TextMeshProUGUI dialogueText;
    public Image kiraAvatar;

    [Header("Kira")]
    public GameObject kiraDrone;
    public Transform kiraSpawnBelow;
    public Transform kiraTargetPos;

    [Header("Configuración")]
    public float typingSpeed = 0.03f;
    public float autoAdvanceIntro = 3f;
    public float autoAdvanceMid = 4f;
    public float autoAdvanceOutro = 3.5f;

    static readonly Color KiraColor = new Color(0f, 1f, 1f);   // cyan
    static readonly Color NxColor = new Color(1f, 1f, 0f);     // amarillo

    private List<DialogueLine> lines;
    private DialogueSegment segment;
    private int currentLine;
    private bool isTyping;
    private bool dialogueActive;
    private float autoAdvanceTimer;
    private PlayerMovement playerMovement;

    private bool FreezesPlayer => segment == DialogueSegment.Intro || segment == DialogueSegment.Outro;
    private bool AllowsManualAdvance => segment == DialogueSegment.Intro;
    private bool AnimatesKiraEntrance =>
        segment == DialogueSegment.Intro && stageData != null && stageData.introAnimatesKiraEntrance;

    private float CurrentAutoAdvance
    {
        get
        {
            float baseTime = segment == DialogueSegment.Intro ? autoAdvanceIntro
                           : segment == DialogueSegment.Mid ? autoAdvanceMid
                           : autoAdvanceOutro;
            float ov = lines[currentLine].autoAdvanceOverride;
            return ov > 0f ? ov : baseTime;
        }
    }

    /// <summary>
    /// Dispara un segmento de diálogo del StageData. <paramref name="player"/>
    /// solo hace falta para los segmentos que congelan al jugador (Intro / Outro).
    /// </summary>
    public void StartDialogue(DialogueSegment which, PlayerMovement player = null)
    {
        if (dialogueActive) return;
        if (stageData == null)
        {
            Debug.LogWarning("DialogueManager: no hay StageData asignado.", this);
            return;
        }

        segment = which;
        lines = which == DialogueSegment.Intro ? stageData.introDialogue
              : which == DialogueSegment.Mid ? stageData.midDialogue
              : stageData.outroDialogue;

        if (lines == null || lines.Count == 0) return;

        playerMovement = player;
        if (FreezesPlayer && playerMovement != null)
            playerMovement.SetCanMove(false);

        dialogueActive = true;
        currentLine = 0;
        autoAdvanceTimer = 0f;
        dialogueCanvas.SetActive(true);

        if (AnimatesKiraEntrance && kiraDrone != null && kiraSpawnBelow != null && kiraTargetPos != null)
            StartCoroutine(KiraEnterFromBelow());
        else
            ShowCurrentLine();
    }

    IEnumerator KiraEnterFromBelow()
    {
        kiraDrone.SetActive(true);
        kiraDrone.transform.position = kiraSpawnBelow.position;

        float duration = 1.5f;
        float elapsed = 0f;
        Vector3 start = kiraSpawnBelow.position;
        Vector3 end = kiraTargetPos.position;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            kiraDrone.transform.position = Vector3.Lerp(start, end, t);
            yield return null;
        }

        kiraDrone.transform.position = end;

        StartCoroutine(KiraFloat());
        yield return new WaitForSeconds(0.4f);
        ShowCurrentLine();
    }

    IEnumerator KiraFloat()
    {
        if (kiraDrone == null || kiraTargetPos == null) yield break;

        Vector3 basePos = kiraTargetPos.position;
        float floatSpeed = 1.2f;
        float floatAmount = 0.15f;

        while (dialogueActive)
        {
            float newY = basePos.y + Mathf.Sin(Time.time * floatSpeed) * floatAmount;
            kiraDrone.transform.position = new Vector3(basePos.x, newY, basePos.z);
            yield return null;
        }
    }

    void ShowCurrentLine()
    {
        DialogueLine line = lines[currentLine];

        speakerNameText.text = line.speaker == Speaker.Kira ? "KIRA" : "NX-7";
        speakerNameText.color = line.speaker == Speaker.Kira ? KiraColor : NxColor;

        // El retrato es de Kira: mostrarlo solo cuando habla ella.
        if (kiraAvatar != null)
            kiraAvatar.gameObject.SetActive(line.speaker == Speaker.Kira);

        autoAdvanceTimer = 0f;
        StopCoroutine("TypeLine");
        StartCoroutine("TypeLine", line.text);
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
    }

    void Update()
    {
        if (!dialogueActive) return;

        if (AllowsManualAdvance && Keyboard.current != null &&
            (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.enterKey.wasPressedThisFrame))
        {
            if (isTyping)
            {
                StopCoroutine("TypeLine");
                dialogueText.text = lines[currentLine].text;
                isTyping = false;
                autoAdvanceTimer = 0f;
            }
            else
            {
                AdvanceLine();
            }
            return;
        }

        if (!isTyping)
        {
            autoAdvanceTimer += Time.deltaTime;
            if (autoAdvanceTimer >= CurrentAutoAdvance)
                AdvanceLine();
        }
    }

    void AdvanceLine()
    {
        currentLine++;
        if (currentLine < lines.Count)
            ShowCurrentLine();
        else
            EndDialogue();
    }

    void EndDialogue()
    {
        dialogueActive = false;
        dialogueCanvas.SetActive(false);

        if (FreezesPlayer && playerMovement != null)
            playerMovement.SetCanMove(true);

        // Tras la intro, el dron acaba de aparecer: que empiece a seguir al jugador.
        if (segment == DialogueSegment.Intro && kiraDrone != null && playerMovement != null)
        {
            KiraFollower follower = kiraDrone.GetComponent<KiraFollower>();
            if (follower != null)
                follower.StartFollowing(playerMovement.transform);
        }
    }
}
