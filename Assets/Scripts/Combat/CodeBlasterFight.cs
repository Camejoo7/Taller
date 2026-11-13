using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class CodeBlasterFight : MonoBehaviour
{
    [Header("Contenido")]
    public CodeBlasterEncounter encounter;

    [Header("Referencias de escena")]
    public Transform dronePoint;

    public GameObject fragmentPrefab;
    public CodeBlasterUI ui;
    public CameraFollow cameraFollow;

    public DialogueManager dialogueManager;

    [Header("Cámara")]
    public float combatZoom = 2.6f;

    [Header("Tiempos")]
    public float wrongLockTime = 1f;

    public float feedbackTime = 1.5f;

    public float victoryTime = 1.8f;

    private const string GenericWrong = "Ese no. Leelo de nuevo, con calma.";
    private const string GenericTimeout = "Se te fue el tiempo. Tranquilo, pensalo otra vez.";

    private readonly List<CodeBlasterTarget> fragments = new List<CodeBlasterTarget>();
    private GameObject drone;
    private PlayerMovement player;
    private PlayerShooting shooting;

    private bool triggered;
    private bool pendingStart;
    private PlayerMovement pendingPlayer;
    private bool fightActive;
    private bool acceptingAnswers;
    private float orbitAngle;
    private float answerTimer;
    private Vector3 droneBasePos;

    private int answersGiven;
    private float questionStartTime;
    private List<string> wrongChoices = new List<string>();

    void OnTriggerEnter2D(Collider2D other)
    {
        if (triggered || !other.CompareTag("Player")) return;

        PlayerMovement pm = other.GetComponentInParent<PlayerMovement>();
        if (pm == null) return;

        triggered = true;

        if (dialogueManager != null && dialogueManager.IsActive)
        {
            pendingStart = true;
            pendingPlayer = pm;
            return;
        }

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

        EnemyAI ai = drone.GetComponent<EnemyAI>();
        if (ai != null) ai.enabled = false;

        Rigidbody2D rb = drone.GetComponent<Rigidbody2D>();
        if (rb != null) rb.bodyType = RigidbodyType2D.Kinematic;
    }

    void SpawnFragments()
    {
        string[] options = encounter.question.options;
        int correct = encounter.question.correctIndex;

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
        if (pendingStart)
        {
            if (dialogueManager != null && dialogueManager.IsActive) return;

            pendingStart = false;
            StartFight(pendingPlayer);
            return;
        }

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

    IEnumerator WaitForFeedback(float holdAfterTyping)
    {
        if (ui != null)
            while (ui.IsTyping)
                yield return null;

        yield return new WaitForSeconds(holdAfterTyping);
    }

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
