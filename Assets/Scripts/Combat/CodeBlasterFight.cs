using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Arma y dirige un encuentro Code Blaster: hace aparecer el dron, le pone
/// alrededor los fragmentos de codigo de la pregunta girando en orbita, y
/// decide que pasa cuando el jugador le pega a uno.
///
/// Todo el contenido (la pregunta, las opciones, cual es la correcta, lo que
/// dice Kira) sale del CodeBlasterEncounter, no de este script: por eso el
/// mismo componente sirve para las 6 stages cambiando solo el asset.
///
/// Se dispara al entrar el jugador en el trigger de este mismo objeto.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class CodeBlasterFight : MonoBehaviour
{
    [Header("Contenido")]
    public CodeBlasterEncounter encounter;

    [Header("Referencias de escena")]
    [Tooltip("Donde queda flotando el dron durante la pelea.")]
    public Transform dronePoint;

    public GameObject fragmentPrefab;
    public CodeBlasterUI ui;
    public CameraFollow cameraFollow;

    [Header("Cámara")]
    [Tooltip("Cuánto se aleja la cámara durante la pelea. 0 = no tocarla.")]
    public float combatZoom = 2.6f;

    [Header("Tiempos")]
    [Tooltip("Segundos sin poder disparar después de errarle a un fragmento.")]
    public float wrongLockTime = 1f;

    [Tooltip("Cuánto queda el comentario de Kira DESPUÉS de terminar de escribirse.")]
    public float feedbackTime = 1.5f;

    [Tooltip("Cuánto se queda la explicación en pantalla al acertar, ya escrita.")]
    public float victoryTime = 1.8f;

    private const string GenericWrong = "Ese no. Leelo de nuevo, con calma.";
    private const string GenericTimeout = "Se te fue el tiempo. Tranquilo, pensalo otra vez.";

    private readonly List<CodeBlasterTarget> fragments = new List<CodeBlasterTarget>();
    private GameObject drone;
    private PlayerMovement player;
    private PlayerShooting shooting;

    private bool triggered;
    private bool fightActive;
    private bool acceptingAnswers;
    private float orbitAngle;
    private float answerTimer;
    private Vector3 droneBasePos;

    // Registro para el informe: cuantas respuestas dio, cuales fueron las
    // equivocadas y cuanto tardo. Se arranca al empezar la pelea.
    private int answersGiven;
    private float questionStartTime;
    private List<string> wrongChoices = new List<string>();

    void OnTriggerEnter2D(Collider2D other)
    {
        if (triggered || !other.CompareTag("Player")) return;

        PlayerMovement pm = other.GetComponentInParent<PlayerMovement>();
        if (pm == null) return;

        triggered = true;
        StartFight(pm);
    }

    public void StartFight(PlayerMovement playerMovement)
    {
        if (fightActive) return;

        if (encounter == null || encounter.question == null || encounter.question.options == null
            || encounter.question.options.Length == 0)
        {
            Debug.LogWarning("CodeBlasterFight: el encuentro no tiene pregunta cargada.", this);
            return;
        }

        if (fragmentPrefab == null || dronePoint == null)
        {
            Debug.LogWarning("CodeBlasterFight: falta el prefab del fragmento o el punto del dron.", this);
            return;
        }

        player = playerMovement;
        shooting = player.GetComponent<PlayerShooting>();

        fightActive = true;
        droneBasePos = dronePoint.position;

        SpawnDrone();
        SpawnFragments();

        if (ui != null)
            ui.ShowQuestion(encounter.question.question);

        if (cameraFollow != null)
        {
            if (combatZoom > 0f) cameraFollow.SetZoom(combatZoom);
            cameraFollow.SetSecondaryTarget(dronePoint, 0.5f);
        }

        answersGiven = 0;
        wrongChoices.Clear();
        questionStartTime = Time.time;

        answerTimer = encounter.timeLimit;
        acceptingAnswers = true;
    }

    void SpawnDrone()
    {
        if (encounter.enemyPrefab == null) return;

        drone = Instantiate(encounter.enemyPrefab, droneBasePos, Quaternion.identity);

        // El dron de la pelea se queda flotando en su lugar. Si persiguiera al
        // jugador, los fragmentos girando a su alrededor se volverian imposibles
        // de leer y de apuntar.
        EnemyAI ai = drone.GetComponent<EnemyAI>();
        if (ai != null) ai.enabled = false;

        Rigidbody2D rb = drone.GetComponent<Rigidbody2D>();
        if (rb != null) rb.bodyType = RigidbodyType2D.Kinematic;
    }

    void SpawnFragments()
    {
        string[] options = encounter.question.options;
        int correct = encounter.question.correctIndex;

        // Mezclamos en que lugar de la orbita cae cada opcion. Si no, la correcta
        // siempre estaria en la misma posicion y el jugador podria acertar de
        // memoria en vez de por leer el codigo, que es justo lo que queremos medir.
        List<int> order = new List<int>();
        for (int i = 0; i < options.Length; i++) order.Add(i);
        for (int i = order.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            int tmp = order[i];
            order[i] = order[j];
            order[j] = tmp;
        }

        foreach (int option in order)
        {
            GameObject go = Instantiate(fragmentPrefab, droneBasePos, Quaternion.identity);
            CodeBlasterTarget target = go.GetComponent<CodeBlasterTarget>();
            if (target == null) continue;

            target.Setup(option, options[option], option == correct);
            target.OnHit += OnFragmentHit;
            fragments.Add(target);
        }

        PlaceFragments();
    }

    void Update()
    {
        if (!fightActive) return;

        FloatDrone();

        orbitAngle += encounter.fragmentOrbitSpeed * Time.deltaTime;
        PlaceFragments();

        if (acceptingAnswers && encounter.timeLimit > 0f)
        {
            answerTimer -= Time.deltaTime;
            if (answerTimer <= 0f)
                StartCoroutine(TimeRanOut());
        }
    }

    void FloatDrone()
    {
        if (drone == null) return;

        float y = droneBasePos.y + Mathf.Sin(Time.time * 1.5f) * 0.12f;
        drone.transform.position = new Vector3(droneBasePos.x, y, droneBasePos.z);
    }

    /// <summary>
    /// Reparte los fragmentos en una orbita ovalada alrededor del dron. Es
    /// ovalada y no redonda porque la camara muestra mucho mas ancho que alto.
    /// </summary>
    void PlaceFragments()
    {
        if (fragments.Count == 0) return;

        float step = 360f / fragments.Count;
        float rx = encounter.fragmentOrbitRadius;
        float ry = encounter.fragmentOrbitRadius * encounter.fragmentOrbitVerticalScale;

        for (int i = 0; i < fragments.Count; i++)
        {
            if (fragments[i] == null) continue;

            float angle = (orbitAngle + step * i) * Mathf.Deg2Rad;
            fragments[i].transform.position = droneBasePos + new Vector3(Mathf.Cos(angle) * rx, Mathf.Sin(angle) * ry, 0f);
        }
    }

    void OnFragmentHit(CodeBlasterTarget target)
    {
        if (!acceptingAnswers || target == null) return;

        acceptingAnswers = false;

        if (target.IsCorrect)
            StartCoroutine(AnsweredRight(target));
        else
            StartCoroutine(AnsweredWrong(target));
    }

    IEnumerator AnsweredRight(CodeBlasterTarget target)
    {
        answersGiven++;
        QuestionAttempt record = RecordResult();

        target.MarkCorrect();

        if (ui != null)
        {
            string line = TextOr(encounter.question.explanation, "¡Esa es!");
            ui.ShowFeedback(line + "  (+" + record.Points + ")");
        }

        // Los demas fragmentos se van; queda solo el correcto a la vista.
        foreach (CodeBlasterTarget other in fragments)
            if (other != null && other != target)
                Destroy(other.gameObject);

        yield return WaitForFeedback(victoryTime);

        if (target != null) Destroy(target.gameObject);
        if (drone != null) Destroy(drone);

        EndFight();
    }

    IEnumerator AnsweredWrong(CodeBlasterTarget target)
    {
        answersGiven++;
        if (target.label != null)
            wrongChoices.Add(target.label.text);

        target.FlashWrong(wrongLockTime);

        if (ui != null)
            ui.ShowFeedback(TextOr(encounter.question.wrongFeedback, GenericWrong));

        ApplyWrongDamage();

        // Pequeño castigo: unos segundos sin poder disparar, para que no se
        // pueda acertar a fuerza de dispararle a todo.
        if (shooting != null)
            shooting.BlockFor(wrongLockTime);

        yield return WaitForFeedback(feedbackTime);

        if (ui != null) ui.HideFeedback();

        answerTimer = encounter.timeLimit;
        acceptingAnswers = true;
    }

    IEnumerator TimeRanOut()
    {
        acceptingAnswers = false;

        answersGiven++;
        wrongChoices.Add("(se acabó el tiempo)");

        if (ui != null)
            ui.ShowFeedback(GenericTimeout);

        ApplyWrongDamage();

        yield return WaitForFeedback(feedbackTime);

        if (ui != null) ui.HideFeedback();

        answerTimer = encounter.timeLimit;
        acceptingAnswers = true;
    }

    /// <summary>
    /// Espera a que Kira termine de escribir y recien ahi cuenta el tiempo en
    /// pantalla. Si contaramos desde el principio, una frase larga se cortaria
    /// a la mitad — que es justo lo que pasaba antes.
    /// </summary>
    IEnumerator WaitForFeedback(float holdAfterTyping)
    {
        if (ui != null)
            while (ui.IsTyping)
                yield return null;

        yield return new WaitForSeconds(holdAfterTyping);
    }

    /// <summary>
    /// Guarda cómo le fue en esta pregunta. Se llama una sola vez, al acertar:
    /// recién ahí se sabe cuántos intentos le llevó, que es el dato que después
    /// separa al que sabía del que fue descartando.
    /// </summary>
    QuestionAttempt RecordResult()
    {
        QuestionAttempt record = new QuestionAttempt();
        record.encounterId = encounter.encounterId;
        record.concept = encounter.concept;
        record.stageNumber = encounter.stageNumber;
        record.question = encounter.question.question;
        record.attempts = Mathf.Max(1, answersGiven);
        record.seconds = Time.time - questionStartTime;
        record.wrongChoices = new List<string>(wrongChoices);

        if (ScoreTracker.Instance != null)
            ScoreTracker.Instance.Record(record);

        return record;
    }

    /// <summary>
    /// Le pega al jugador si hay un sistema de vida puesto. Hoy no existe
    /// ninguno, asi que no pasa nada; el dia que exista, esto ya esta conectado.
    /// </summary>
    void ApplyWrongDamage()
    {
        if (player == null || encounter.damageOnWrong <= 0) return;

        IPlayerDamageable health = player.GetComponentInParent<IPlayerDamageable>();
        if (health != null)
            health.TakeDamage(encounter.damageOnWrong);
    }

    void EndFight()
    {
        fightActive = false;
        acceptingAnswers = false;

        foreach (CodeBlasterTarget target in fragments)
        {
            if (target == null) continue;
            target.OnHit -= OnFragmentHit;
            Destroy(target.gameObject);
        }
        fragments.Clear();

        if (ui != null) ui.Hide();

        if (cameraFollow != null)
        {
            cameraFollow.ResetZoom();
            cameraFollow.ClearSecondaryTarget();
        }
    }

    static string TextOr(string value, string fallback)
    {
        return string.IsNullOrEmpty(value) ? fallback : value;
    }
}
