using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public enum DialogueSegment { Intro, Mid, Outro }

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

    public static void Snapshot() { saved = new string[played.Count]; played.CopyTo(saved); }

    public static void RestoreSnapshot() { played.Clear(); played.UnionWith(saved); }

    public static bool WasPlayed(string key) { return played.Contains(key); }
    public static void MarkPlayed(string key) { played.Add(key); }
}

public class DialogueManager : MonoBehaviour
{
    [Header("Contenido")]
    public StageData stageData;

    [Header("UI")]
    public GameObject dialogueCanvas;
    public TextMeshProUGUI speakerNameText;
    public TextMeshProUGUI dialogueText;

    public Image kiraAvatar;

    [Header("Retratos de los otros")]
    public Sprite nxPortrait;
    public Sprite custodioPortrait;

    [Header("Kira")]
    public GameObject kiraDrone;
    public Transform kiraSpawnBelow;
    public Transform kiraTargetPos;

    [Header("Configuración")]
    public float typingSpeed = 0.03f;

    public float commentTypingSpeed = 0.008f;
    public float autoAdvanceIntro = 3f;
    public float autoAdvanceMid = 4f;
    public float autoAdvanceOutro = 3.5f;

    public float sceneFallbackAdvance = 15f;

    [Header("Al terminar la intro (sistema viejo)")]
    public UnityEvent onIntroEnded;

    static readonly Color KiraColor = new Color(0f, 1f, 1f);
    static readonly Color NxColor = new Color(1f, 1f, 0f);
    static readonly Color CustodioColor = new Color(1f, 0.33f, 0.28f);
    static readonly Color HintColor = new Color(0.341f, 0.816f, 0.855f);

    private List<DialogueLine> lines;
    private int currentLine;
    private bool isTyping;
    private bool dialogueActive;
    private bool entering;
    private float autoAdvanceTimer;
    private PlayerMovement playerMovement;

    private bool freezes;
    private bool manual;
    private float autoTime;
    private bool handoffKira;
    private Action onEnded;

    private string pendingId;
    private PlayerMovement pendingPlayer;
    private Action pendingEnded;

    private Sprite kiraPortrait;
    private TextMeshProUGUI hint;

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

    public void PlayMoment(string id)
    {
        StartMoment(id);
    }

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
                pendingId = null;
                pendingPlayer = null;
                pendingEnded = null;
                EndDialogue();
            }
            else
            {
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

        dialogueText.text = line;
        dialogueText.maxVisibleCharacters = 0;
        dialogueText.ForceMeshUpdate();
        int total = dialogueText.textInfo.characterCount;

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

        if (KiraBook.BlocksInput) return;

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

        if (freezes && playerMovement != null)
            StartCoroutine(ReleaseNextFrame(playerMovement));

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

        if (player != null && !(dialogueActive && freezes))
            player.SetCanMove(true);
    }
}
