using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Los tres segmentos del sistema viejo (uno de cada uno por stage). Los
/// diálogos del juego ahora son "momentos" (ver <see cref="DialogueMoment"/>),
/// pero esto sigue andando para lo que todavía lo use.
/// - Intro: congela al jugador, anima la entrada del dron de Kira (si el
///   StageData lo pide), permite avance manual, y al terminar arranca el
///   KiraFollower.
/// - Mid: no congela, sin animación de dron, solo avance automático.
/// - Outro: congela al jugador, solo avance automático.
/// </summary>
public enum DialogueSegment { Intro, Mid, Outro }

/// <summary>
/// Qué momentos de diálogo ya se vieron. Vive en memoria, como el progreso del
/// libro: morir recarga la escena, y sin esto Kira repetiría todo lo que ya
/// dijo. Volver al Menú Principal lo borra: es otra partida.
/// </summary>
public static class DialogueProgress
{
    static readonly HashSet<string> played = new HashSet<string>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetOnPlay()
    {
        played.Clear();
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "MainMenu") played.Clear();
    }

    static string[] saved = new string[0];

    /// <summary>Guarda qué momentos ya salieron al entrar a un stage (lo llama <see cref="RunProgress"/>).</summary>
    public static void Snapshot() { saved = new string[played.Count]; played.CopyTo(saved); }

    /// <summary>Botón Reiniciar de la pausa: los momentos de este stage vuelven a salir.</summary>
    public static void RestoreSnapshot() { played.Clear(); played.UnionWith(saved); }

    public static bool WasPlayed(string key) { return played.Contains(key); }
    public static void MarkPlayed(string key) { played.Add(key); }
}

/// <summary>
/// Manager único de diálogo. Lee el contenido de un StageData.
///
/// Los diálogos del juego son "momentos" (<see cref="StartMoment"/>), de dos
/// clases:
/// - Escena (freezePlayer): frena al jugador y se avanza con Espacio. Espacio
///   completa la línea que se está escribiendo y, ya completa, pasa a la que
///   sigue. No hay forma de saltarla entera.
/// - Comentario: no frena y avanza solo. Si el jugador abre una terminal, se
///   corta: la terminal usa la misma parte de la pantalla.
/// Cada momento sale una sola vez por partida (<see cref="DialogueProgress"/>).
/// </summary>
public class DialogueManager : MonoBehaviour
{
    [Header("Contenido")]
    public StageData stageData;

    [Header("UI")]
    public GameObject dialogueCanvas;
    public TextMeshProUGUI speakerNameText;
    public TextMeshProUGUI dialogueText;
    [Tooltip("El retrato. Arranca con el de Kira; cuando habla otro, se cambia el dibujo.")]
    public Image kiraAvatar;

    [Header("Retratos de los otros")]
    public Sprite nxPortrait;
    public Sprite custodioPortrait;

    [Header("Kira")]
    public GameObject kiraDrone;
    public Transform kiraSpawnBelow;
    public Transform kiraTargetPos;

    [Header("Configuración")]
    [Tooltip("Segundos por letra en una escena (el jugador está frenado y lee tranquilo).")]
    public float typingSpeed = 0.03f;

    [Tooltip("Segundos por letra en un comentario. Más rápido: el jugador está " +
             "en medio de la acción y tiene que alcanzar a leerlo.")]
    public float commentTypingSpeed = 0.008f;
    public float autoAdvanceIntro = 3f;
    public float autoAdvanceMid = 4f;
    public float autoAdvanceOutro = 3.5f;

    [Tooltip("En una escena (momento que frena) se avanza con Espacio. Si nadie " +
             "lo aprieta, la línea pasa sola después de estos segundos. 0 = nunca.")]
    public float sceneFallbackAdvance = 15f;

    [Header("Al terminar la intro (sistema viejo)")]
    public UnityEvent onIntroEnded;

    static readonly Color KiraColor = new Color(0f, 1f, 1f);            // cyan
    static readonly Color NxColor = new Color(1f, 1f, 0f);              // amarillo
    static readonly Color CustodioColor = new Color(1f, 0.33f, 0.28f);  // rojo
    static readonly Color HintColor = new Color(0.341f, 0.816f, 0.855f);

    private List<DialogueLine> lines;
    private int currentLine;
    private bool isTyping;
    private bool dialogueActive;
    private bool entering;
    private float autoAdvanceTimer;
    private PlayerMovement playerMovement;

    // Cómo se comporta el diálogo que está corriendo.
    private bool freezes;
    private bool manual;
    private float autoTime;
    private bool handoffKira;
    private Action onEnded;

    // Un momento que se pidió mientras Kira todavía hablaba.
    private string pendingId;
    private PlayerMovement pendingPlayer;
    private Action pendingEnded;

    private Sprite kiraPortrait;
    private TextMeshProUGUI hint;

    /// <summary>Si Kira está hablando ahora mismo. Lo consulta el combate para
    /// no arrancar una pelea encima de un diálogo.</summary>
    public bool IsActive => dialogueActive;

    private float CurrentAutoAdvance
    {
        get
        {
            float ov = lines[currentLine].autoAdvanceOverride;
            return ov > 0f ? ov : autoTime;
        }
    }

    void Awake()
    {
        if (kiraAvatar != null) kiraPortrait = kiraAvatar.sprite;
    }

    void Start()
    {
        // Si la escena se recargó (el jugador murió) y Kira ya había hecho su
        // entrada, esa escena no vuelve a salir: que aparezca directamente
        // siguiendo al jugador en vez de quedarse escondida.
        if (stageData == null || kiraDrone == null) return;

        foreach (DialogueMoment m in stageData.moments)
        {
            if (!m.kiraEntrance || !DialogueProgress.WasPlayed(KeyOf(m.id))) continue;

            PlayerMovement player = FindFirstObjectByType<PlayerMovement>();
            KiraFollower follower = kiraDrone.GetComponent<KiraFollower>();
            if (player != null && follower != null)
            {
                kiraDrone.SetActive(true);
                kiraDrone.transform.position = player.transform.position + new Vector3(0.6f, 0.5f, 0f);
                follower.StartFollowing(player.transform);
            }
            break;
        }
    }

    string KeyOf(string momentId)
    {
        return (stageData != null ? stageData.name : "") + ":" + momentId;
    }

    // ------------------------------------------------------------- momentos

    /// <summary>Para enganchar desde un UnityEvent (el jefe al caer, por ejemplo).</summary>
    public void PlayMoment(string id)
    {
        StartMoment(id);
    }

    /// <summary>
    /// Dispara un momento del StageData. Devuelve false si no existe o si ya
    /// salió en esta partida. <paramref name="ended"/> se llama cuando termina.
    /// </summary>
    public bool StartMoment(string id, PlayerMovement player = null, Action ended = null)
    {
        if (stageData == null)
        {
            Debug.LogWarning("DialogueManager: no hay StageData asignado.", this);
            return false;
        }

        DialogueMoment m = stageData.FindMoment(id);
        if (m == null || m.lines == null || m.lines.Count == 0)
        {
            Debug.LogWarning("DialogueManager: el momento '" + id + "' no existe o está vacío en " + stageData.name + ".", this);
            return false;
        }

        if (DialogueProgress.WasPlayed(KeyOf(id))) return false;

        if (dialogueActive)
        {
            if (!freezes)
            {
                // Lo nuevo le gana a un comentario que venía de antes: si no,
                // Kira terminaría hablando de algo que ya quedó atrás.
                pendingId = null;
                pendingPlayer = null;
                pendingEnded = null;
                EndDialogue();
            }
            else
            {
                // Una escena no se corta: lo nuevo espera a que termine.
                pendingId = id;
                pendingPlayer = player;
                pendingEnded = ended;
                return true;
            }
        }

        DialogueProgress.MarkPlayed(KeyOf(id));
        Begin(m.lines, m.freezePlayer, m.freezePlayer,
              m.freezePlayer ? sceneFallbackAdvance : autoAdvanceMid,
              m.kiraEntrance, m.kiraEntrance, player, ended);
        return true;
    }

    // ------------------------------------------------- segmentos (sistema viejo)

    /// <summary>
    /// Dispara un segmento del StageData (intro / mid / outro).
    /// </summary>
    public void StartDialogue(DialogueSegment which, PlayerMovement player = null)
    {
        if (dialogueActive) return;
        if (stageData == null)
        {
            Debug.LogWarning("DialogueManager: no hay StageData asignado.", this);
            return;
        }

        List<DialogueLine> segmentLines =
              which == DialogueSegment.Intro ? stageData.introDialogue
            : which == DialogueSegment.Mid ? stageData.midDialogue
            : stageData.outroDialogue;

        if (segmentLines == null || segmentLines.Count == 0) return;

        bool isIntro = which == DialogueSegment.Intro;
        float auto = isIntro ? autoAdvanceIntro
                   : which == DialogueSegment.Mid ? autoAdvanceMid
                   : autoAdvanceOutro;

        Action ended = null;
        if (isIntro) ended = () => { if (onIntroEnded != null) onIntroEnded.Invoke(); };

        Begin(segmentLines, which != DialogueSegment.Mid, isIntro, auto,
              isIntro && stageData.introAnimatesKiraEntrance, isIntro, player, ended);
    }

    // ---------------------------------------------------------------- común

    void Begin(List<DialogueLine> newLines, bool freeze, bool manualAdvance, float autoAdvance,
               bool kiraEnters, bool kiraHandoff, PlayerMovement player, Action ended)
    {
        lines = newLines;
        freezes = freeze;
        manual = manualAdvance;
        autoTime = autoAdvance;
        handoffKira = kiraHandoff;
        onEnded = ended;

        playerMovement = player != null ? player : FindFirstObjectByType<PlayerMovement>();
        if (freezes && playerMovement != null)
            playerMovement.SetCanMove(false);

        dialogueActive = true;
        currentLine = 0;
        autoAdvanceTimer = 0f;
        dialogueCanvas.SetActive(true);
        EnsureHint();

        if (kiraEnters && kiraDrone != null && kiraSpawnBelow != null && kiraTargetPos != null)
        {
            // Mientras el dron sube, el cuadro espera vacío.
            ShowSpeaker(lines[0].speaker);
            dialogueText.text = "";
            StartCoroutine(KiraEnterFromBelow());
        }
        else
        {
            ShowCurrentLine();
        }
    }

    IEnumerator KiraEnterFromBelow()
    {
        entering = true;
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
        entering = false;
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

    void ShowSpeaker(Speaker speaker)
    {
        string who = speaker == Speaker.Kira ? "KIRA"
                   : speaker == Speaker.NX7 ? "NX-7"
                   : "CUSTODIO";
        Color color = speaker == Speaker.Kira ? KiraColor
                    : speaker == Speaker.NX7 ? NxColor
                    : CustodioColor;
        Sprite portrait = speaker == Speaker.Kira ? kiraPortrait
                        : speaker == Speaker.NX7 ? nxPortrait
                        : custodioPortrait;

        speakerNameText.text = who;
        speakerNameText.color = color;

        if (kiraAvatar != null)
        {
            kiraAvatar.gameObject.SetActive(portrait != null);
            kiraAvatar.sprite = portrait;
            // El de Kira se dibuja como siempre (ocupa el recuadro entero); los
            // otros tienen otra forma y estirados quedarían deformes.
            kiraAvatar.preserveAspect = speaker != Speaker.Kira;
        }
    }

    void ShowCurrentLine()
    {
        DialogueLine line = lines[currentLine];
        ShowSpeaker(line.speaker);

        autoAdvanceTimer = 0f;
        StopCoroutine("TypeLine");
        StartCoroutine("TypeLine", line.text);
    }

    IEnumerator TypeLine(string line)
    {
        isTyping = true;

        // El texto entra entero y se va destapando: así las palabras no saltan
        // de renglón a medio escribir.
        dialogueText.text = line;
        dialogueText.maxVisibleCharacters = 0;
        dialogueText.ForceMeshUpdate();
        int total = dialogueText.textInfo.characterCount;

        // Por tiempo y no una letra por cuadro: así la velocidad no depende
        // de los cuadros por segundo y puede ir más rápido que uno por cuadro.
        float perChar = freezes ? typingSpeed : commentTypingSpeed;
        float elapsed = 0f;
        int shown = 0;
        while (shown < total)
        {
            elapsed += Time.deltaTime;
            shown = perChar <= 0f ? total : Mathf.Min(total, Mathf.FloorToInt(elapsed / perChar) + 1);
            dialogueText.maxVisibleCharacters = shown;
            yield return null;
        }

        dialogueText.maxVisibleCharacters = 99999;
        isTyping = false;
    }

    void Update()
    {
        if (!dialogueActive || entering) return;

        // Espacio y Enter son del libro mientras está abierto.
        if (KiraBook.BlocksInput) return;

        // Un comentario no compite con la terminal: si se abre una, se corta.
        if (!freezes && CodeTerminal.AnyOpen)
        {
            EndDialogue();
            return;
        }

        if (manual && Keyboard.current != null &&
            (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.enterKey.wasPressedThisFrame))
        {
            if (isTyping)
            {
                StopCoroutine("TypeLine");
                dialogueText.maxVisibleCharacters = 99999;
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
            float limit = CurrentAutoAdvance;
            if (limit > 0f && autoAdvanceTimer >= limit)
                AdvanceLine();
        }
    }

    void LateUpdate()
    {
        if (hint == null) return;

        // El cartel de ESPACIO aparece cuando la línea ya está entera y hay
        // que apretar para seguir. Titila en tiempo real.
        bool show = dialogueActive && manual && !entering && !isTyping &&
                    Mathf.Repeat(Time.unscaledTime, 1f) < 0.65f;
        if (hint.enabled != show) hint.enabled = show;
    }

    void EnsureHint()
    {
        if (hint != null || dialogueText == null) return;

        GameObject go = new GameObject("Hint", typeof(RectTransform));
        go.transform.SetParent(dialogueText.transform.parent, false);

        hint = go.AddComponent<TextMeshProUGUI>();
        hint.font = dialogueText.font;
        hint.fontSize = 13f;
        hint.color = HintColor;
        hint.alignment = TextAlignmentOptions.BottomRight;
        hint.raycastTarget = false;
        hint.text = "ESPACIO";
        hint.enabled = false;

        RectTransform rt = hint.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(1f, 0f);
        rt.anchoredPosition = new Vector2(-18f, 10f);
        rt.sizeDelta = new Vector2(240f, 20f);
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
        entering = false;
        StopCoroutine("TypeLine");
        isTyping = false;
        dialogueCanvas.SetActive(false);

        // Se suelta al jugador un cuadro después: el Espacio que cerró la
        // última línea es el mismo que lo haría saltar.
        if (freezes && playerMovement != null)
            StartCoroutine(ReleaseNextFrame(playerMovement));

        // Tras su entrada, el dron acaba de aparecer: que empiece a seguir al jugador.
        if (handoffKira && kiraDrone != null && playerMovement != null)
        {
            KiraFollower follower = kiraDrone.GetComponent<KiraFollower>();
            if (follower != null)
                follower.StartFollowing(playerMovement.transform);
        }

        Action ended = onEnded;
        onEnded = null;
        if (ended != null) ended();

        if (pendingId != null)
        {
            string id = pendingId;
            PlayerMovement player = pendingPlayer;
            Action pending = pendingEnded;
            pendingId = null;
            pendingPlayer = null;
            pendingEnded = null;
            StartMoment(id, player, pending);
        }
    }

    IEnumerator ReleaseNextFrame(PlayerMovement player)
    {
        yield return null;

        // Salvo que justo haya arrancado otra escena que también lo frena.
        if (player != null && !(dialogueActive && freezes))
            player.SetCanMove(true);
    }
}
