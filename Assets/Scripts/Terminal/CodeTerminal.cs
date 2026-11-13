using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class CodeTerminal : MonoBehaviour
{
    [Header("Contenido")]
    public TerminalChallenge challenge;

    [Header("Pantalla")]
    public CodeTerminalUI ui;

    [Header("Acercarse")]
    public float activationDistance = 0.9f;

    public GameObject promptIndicator;

    [Header("Qué pasa al resolverla")]
    public UnityEvent onSolved;

    public bool reusable = false;

    [Header("Jefe")]
    public bool freezeTime = false;

    [Header("Tiempos")]
    public float outputCharDelay = 0.02f;
    public float pauseAfterSuccess = 1.6f;

    private Transform player;
    private PlayerMovement movement;
    private MonoBehaviour shooting;

    public static bool AnyOpen { get; private set; }

    public static int ClosedFrame { get; private set; } = -1;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetOnPlay() { AnyOpen = false; }

    private bool open;
    private bool solved;
    private string typed = "";
    private int attempts;
    private float startedAt;
    private float caretTimer;
    private bool caretOn = true;
    private bool busy;
    private QuestionAttempt record;
    private int bookOpensAtStart;
    private float timeScaleBefore = 1f;

    void Start()
    {
        movement = FindFirstObjectByType<PlayerMovement>();
        if (movement != null)
        {
            player = movement.transform;
            shooting = movement.GetComponent<PlayerShooting>();
        }

        if (ui == null) ui = FindFirstObjectByType<CodeTerminalUI>();
        if (promptIndicator != null) promptIndicator.SetActive(false);

        if (challenge == null)
            Debug.LogWarning("CodeTerminal: '" + name + "' no tiene TerminalChallenge asignado.", this);
    }

    void Update()
    {
        if (KiraBook.BlocksInput) return;

        if (PauseMenu.IsPaused) return;

        if (open)
        {
            TickOpen();
            return;
        }

        if (player == null || challenge == null) return;
        if (solved && !reusable)
        {
            if (promptIndicator != null && promptIndicator.activeSelf)
                promptIndicator.SetActive(false);
            return;
        }

        bool near = Vector2.Distance(transform.position, player.position) <= activationDistance;

        if (promptIndicator != null && promptIndicator.activeSelf != near)
            promptIndicator.SetActive(near);

        bool free = movement == null || movement.CanMove;

        if (near && free && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            OpenTerminal();
    }

    void OpenTerminal()
    {
        if (ui == null)
        {
            Debug.LogWarning("CodeTerminal: no hay CodeTerminalUI en la escena.", this);
            return;
        }

        open = true;
        AnyOpen = true;
        busy = false;
        typed = "";
        attempts = 0;

        startedAt = Time.unscaledTime;

        record = new QuestionAttempt();
        record.encounterId = challenge.challengeId;
        record.concept = challenge.concept;
        record.stageNumber = challenge.stageNumber;
        record.question = challenge.prompt;

        KiraBook.SetContext(challenge.concept);
        bookOpensAtStart = KiraBook.TimesOpened;

        if (movement != null) movement.SetCanMove(false);
        if (shooting != null) shooting.enabled = false;
        if (promptIndicator != null) promptIndicator.SetActive(false);

        Keyboard.current.onTextInput += OnTextInput;

        if (freezeTime)
        {
            timeScaleBefore = Time.timeScale;
            Time.timeScale = 0f;
        }

        ui.Open(challenge);
        ui.SetTyped(challenge, typed, true);
    }

    void CloseTerminal()
    {
        open = false;
        AnyOpen = false;
        ClosedFrame = Time.frameCount;
        Keyboard.current.onTextInput -= OnTextInput;
        KiraBook.ClearContext();

        if (freezeTime) Time.timeScale = timeScaleBefore;

        if (movement != null) movement.SetCanMove(true);
        if (shooting != null) shooting.enabled = true;

        ui.Close();
    }

    void OnDisable()
    {
        if (open && Keyboard.current != null)
            Keyboard.current.onTextInput -= OnTextInput;
        if (open) AnyOpen = false;

        if (open && freezeTime) Time.timeScale = timeScaleBefore;
    }

    void OnTextInput(char c)
    {
        if (!open || busy || KiraBook.BlocksInput) return;

        if (c < ' ') return;
        if (typed.Length >= 24) return;

        typed += c;
        ui.SetTyped(challenge, typed, true);
        caretTimer = 0f;
        caretOn = true;
    }

    void TickOpen()
    {
        if (busy) return;

        Keyboard kb = Keyboard.current;
        if (kb == null) return;

        if (kb.backspaceKey.wasPressedThisFrame && typed.Length > 0)
        {
            typed = typed.Substring(0, typed.Length - 1);
            ui.SetTyped(challenge, typed, true);
            caretTimer = 0f;
            caretOn = true;
        }

        if (kb.escapeKey.wasPressedThisFrame)
        {
            CloseTerminal();
            return;
        }

        if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame)
        {
            if (typed.Trim().Length > 0)
                StartCoroutine(Submit());
            return;
        }

        caretTimer += Time.unscaledDeltaTime;
        if (caretTimer >= 0.45f)
        {
            caretTimer = 0f;
            caretOn = !caretOn;
            ui.SetTyped(challenge, typed, caretOn);
        }
    }

    IEnumerator Submit()
    {
        busy = true;
        attempts++;

        string answer = typed.Trim();
        ui.SetTyped(challenge, typed, false);

        if (challenge.IsMultiLine)
            ui.PushLine("> ejecutando...", ui.CodeColor);
        else
            ui.PushLine(">>> " + challenge.BuildLine(answer), ui.CodeColor);

        yield return new WaitForSecondsRealtime(0.25f);

        if (challenge.IsCorrect(answer))
            yield return StartCoroutine(Succeed());
        else
            yield return StartCoroutine(Fail(answer));

        busy = false;
    }

    IEnumerator Succeed()
    {
        solved = true;

        if (!string.IsNullOrEmpty(challenge.expectedOutput))
            yield return StartCoroutine(ui.TypeLine(challenge.expectedOutput, ui.OutputColor, outputCharDelay));

        ui.SayKira(challenge.kiraOnSuccess);
        ui.ShowStamp(true);

        record.attempts = attempts;
        record.seconds = Time.unscaledTime - startedAt;
        record.bookOpens = KiraBook.TimesOpened - bookOpensAtStart;
        if (ScoreTracker.Instance != null) ScoreTracker.Instance.Record(record);

        yield return new WaitForSecondsRealtime(pauseAfterSuccess);

        while (KiraBook.BlocksInput) yield return null;

        CloseTerminal();

        if (onSolved != null) onSolved.Invoke();
    }

    IEnumerator Fail(string answer)
    {
        WrongAnswer known = challenge.FindKnownMistake(answer) ?? challenge.NumberTooLow(answer);

        string error = known != null && !string.IsNullOrEmpty(known.pythonError)
            ? known.pythonError
            : challenge.FallbackError(answer);

        string kira = known != null && !string.IsNullOrEmpty(known.kiraSays)
            ? known.kiraSays
            : challenge.fallbackKiraSays;

        if (attempts >= 2)
        {
            string hint = KiraBook.HintFor(challenge.concept);
            if (hint != null) kira = string.IsNullOrEmpty(kira) ? hint : kira + "\n" + hint;
        }

        record.wrongChoices.Add(answer);

        yield return StartCoroutine(ui.TypeLine(error, ui.ErrorColor, outputCharDelay));
        ui.SayKira(kira);
        ui.ShowStamp(false);

        yield return new WaitForSecondsRealtime(0.3f);

        ui.SetTyped(challenge, typed, true);
    }
}
