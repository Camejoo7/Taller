using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

/// <summary>
/// Una consola de NEXCORP plantada en el nivel. El jugador se le acerca, aprieta
/// E, y escribe con el teclado de verdad la parte que falta de una línea de
/// Python. Al acertar, la línea se ejecuta y **algo pasa en el mundo** — ese
/// enganche (<see cref="onSolved"/>) es el punto de la mecánica: si acertar
/// solo mostrara un cartel de "¡Correcto!", esto sería un formulario con
/// pixeles encima.
///
/// Escribir en vez de elegir entre opciones cambia qué se evalúa: no es
/// reconocer la respuesta entre cuatro, es acordarse de ella. Y el costo es que
/// un error de tipeo se siente como un error de concepto, por eso el hueco es
/// corto (una palabra) y los errores previstos tienen explicación propia.
/// </summary>
public class CodeTerminal : MonoBehaviour
{
    [Header("Contenido")]
    public TerminalChallenge challenge;

    [Header("Pantalla")]
    [Tooltip("Si se deja vacío, busca una en la escena.")]
    public CodeTerminalUI ui;

    [Header("Acercarse")]
    [Tooltip("A qué distancia aparece el cartelito de 'E'.")]
    public float activationDistance = 0.9f;

    [Tooltip("Opcional: el cartel que dice que se puede usar. Se prende y apaga solo.")]
    public GameObject promptIndicator;

    [Header("Qué pasa al resolverla")]
    [Tooltip("Acá se engancha lo que tiene que pasar en el mundo: abrir una " +
             "puerta, prender una luz, extender un puente.")]
    public UnityEvent onSolved;

    [Tooltip("Se puede volver a usar después de resuelta.")]
    public bool reusable = false;

    [Header("Tiempos")]
    public float outputCharDelay = 0.02f;
    public float pauseAfterSuccess = 1.6f;

    private Transform player;
    private PlayerMovement movement;
    private MonoBehaviour shooting;

    private bool open;
    private bool solved;
    private string typed = "";
    private int attempts;
    private float startedAt;
    private float caretTimer;
    private bool caretOn = true;
    private bool busy;
    private QuestionAttempt record;

    void Start()
    {
        // El hijo "Visual" del jugador también tiene el tag Player, así que
        // buscamos el componente y subimos a su GameObject.
        movement = FindFirstObjectByType<PlayerMovement>();
        if (movement != null)
        {
            player = movement.transform;
            shooting = movement.GetComponent<PlayerShooting>();
        }

        if (ui == null) ui = FindFirstObjectByType<CodeTerminalUI>();
        if (promptIndicator != null) promptIndicator.SetActive(false);
    }

    void Update()
    {
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

        if (near && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            OpenTerminal();
    }

    // ------------------------------------------------------------- apertura

    void OpenTerminal()
    {
        if (ui == null)
        {
            Debug.LogWarning("CodeTerminal: no hay CodeTerminalUI en la escena.", this);
            return;
        }

        open = true;
        busy = false;
        typed = "";
        attempts = 0;
        startedAt = Time.time;

        record = new QuestionAttempt();
        record.encounterId = challenge.challengeId;
        record.concept = challenge.concept;
        record.stageNumber = challenge.stageNumber;
        record.question = challenge.prompt;

        // Sin esto, las teclas A y D que el jugador escribe también lo hacen
        // caminar, y el click del mouse le dispara a la pantalla.
        if (movement != null) movement.SetCanMove(false);
        if (shooting != null) shooting.enabled = false;
        if (promptIndicator != null) promptIndicator.SetActive(false);

        Keyboard.current.onTextInput += OnTextInput;

        ui.Open(challenge);
        ui.SetTyped(challenge, typed, true);
    }

    void CloseTerminal()
    {
        open = false;
        Keyboard.current.onTextInput -= OnTextInput;

        if (movement != null) movement.SetCanMove(true);
        if (shooting != null) shooting.enabled = true;

        ui.Close();
    }

    void OnDisable()
    {
        if (open && Keyboard.current != null)
            Keyboard.current.onTextInput -= OnTextInput;
    }

    // --------------------------------------------------------------- tecleo

    void OnTextInput(char c)
    {
        if (!open || busy) return;

        // Los de control (enter, backspace, tab) llegan por acá también y no son
        // texto: se manejan como teclas más abajo.
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

        caretTimer += Time.deltaTime;
        if (caretTimer >= 0.45f)
        {
            caretTimer = 0f;
            caretOn = !caretOn;
            ui.SetTyped(challenge, typed, caretOn);
        }
    }

    // ------------------------------------------------------------ ejecución

    IEnumerator Submit()
    {
        busy = true;
        attempts++;

        string answer = typed.Trim();
        ui.SetTyped(challenge, typed, false);

        // El eco de la línea, como cualquier consola.
        ui.PushLine(">>> " + challenge.BuildLine(answer), ui.CodeColor);
        yield return new WaitForSeconds(0.25f);

        if (challenge.IsCorrect(answer))
            yield return StartCoroutine(Succeed());
        else
            yield return StartCoroutine(Fail(answer));

        busy = false;
    }

    IEnumerator Succeed()
    {
        solved = true;

        // La salida del programa, tecleada. Que lo que escribiste IMPRIMA algo
        // es lo que convierte el ejercicio en programar.
        if (!string.IsNullOrEmpty(challenge.expectedOutput))
            yield return StartCoroutine(ui.TypeLine(challenge.expectedOutput, ui.OutputColor, outputCharDelay));

        ui.SayKira(challenge.kiraOnSuccess);
        ui.ShowStamp(true);

        record.attempts = attempts;
        record.seconds = Time.time - startedAt;
        if (ScoreTracker.Instance != null) ScoreTracker.Instance.Record(record);

        yield return new WaitForSeconds(pauseAfterSuccess);

        CloseTerminal();

        // Lo último: que el mundo reaccione. La puerta se abre con el jugador
        // mirando, no detrás de la pantalla de la terminal.
        if (onSolved != null) onSolved.Invoke();
    }

    IEnumerator Fail(string answer)
    {
        WrongAnswer known = challenge.FindKnownMistake(answer);

        string error = known != null && !string.IsNullOrEmpty(known.pythonError)
            ? known.pythonError
            : string.Format(challenge.fallbackError, answer);

        string kira = known != null && !string.IsNullOrEmpty(known.kiraSays)
            ? known.kiraSays
            : challenge.fallbackKiraSays;

        record.wrongChoices.Add(answer);

        yield return StartCoroutine(ui.TypeLine(error, ui.ErrorColor, outputCharDelay));
        ui.SayKira(kira);
        ui.ShowStamp(false);

        yield return new WaitForSeconds(0.3f);

        // Le dejamos lo que escribió para que lo corrija en vez de rehacerlo
        // desde cero: equivocarse en una letra no debería costar la línea entera.
        ui.SetTyped(challenge, typed, true);
    }
}
