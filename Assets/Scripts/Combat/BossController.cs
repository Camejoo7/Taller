using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class BossController : MonoBehaviour
{
    [System.Serializable]
    public class Phase
    {
        public float heatTime = 12f;

        public float pause = 1.4f;

        public int shotsPerVolley = 1;

        public float shotSpeed = 3.2f;

        public bool bombs = false;

        public float bombEvery = 0.6f;

        public float moveSpeed = 1.4f;

        public TerminalChallenge challenge;
    }

    [Header("Nombre")]
    public string bossName = "EL CUSTODIO";

    [Header("Dibujo (pack DRONES, dron 5)")]
    public SpriteRenderer body;

    public Sprite[] idleFrames;

    public Sprite[] openFrames;

    public int stunnedFrame = 3;

    [Header("La arena")]
    public float startX = 125.6f;

    public float leftX = 126.5f;
    public float rightX = 133.5f;

    public float hoverY = -0.9f;

    public float stunnedX = 132.6f;
    public float stunnedY = -1.75f;

    public float groundY = -3.16f;

    public Transform cameraFocus;

    public float cameraZoom = 2.7f;

    public Transform respawnPoint;

    public PoweredDoor arenaDoor;

    [Header("La consola")]
    public CodeTerminal console;

    public SpriteRenderer consoleScreen;

    [Header("Ataques")]
    public BossShot shotPrefab;
    public BossShot bombPrefab;

    public float heatPerShot = 0.35f;

    public Phase[] phases = new Phase[3];

    [Header("Barra")]
    public BossHealthBar bar;

    [Header("Al ganarle")]
    public UnityEvent onDefeated = new UnityEvent();

    [Header("Diálogo de entrada")]
    public DialogueManager dialogue;

    public string momentBeforeEntrance = "2.8";

    public string momentAfterEntrance = "2.8b";

    enum State { Waiting, Intro, Attacking, Stunned, Hurt, Dead }

    private State state = State.Waiting;
    private int phase;
    private float heat;
    private Transform player;
    private PlayerHealth playerHealth;
    private CameraFollow cameraFollow;
    private Collider2D bodyCollider;
    private SpriteFlipbook consoleFlipbook;
    private int idleIndex;
    private float animTimer;
    private bool customFrames;

    Phase Current { get { return phases[Mathf.Clamp(phase, 0, phases.Length - 1)]; } }

    void Start()
    {
        PlayerMovement pm = FindFirstObjectByType<PlayerMovement>();
        if (pm != null)
        {
            player = pm.transform;
            playerHealth = pm.GetComponent<PlayerHealth>();
        }
        if (Camera.main != null) cameraFollow = Camera.main.GetComponent<CameraFollow>();
        if (body != null) bodyCollider = body.GetComponent<Collider2D>();

        if (console != null)
        {
            console.onSolved.AddListener(OnConsoleSolved);
            console.reusable = true;
            console.freezeTime = true;
        }
        if (consoleScreen != null) consoleFlipbook = consoleScreen.GetComponent<SpriteFlipbook>();
        ConsoleOn(false);

        transform.position = new Vector3((leftX + rightX) * 0.5f, hoverY + 4f, 0f);
    }

    void OnDestroy()
    {
        if (PlayerHealth.RespawnOverride == (System.Action<PlayerHealth>)OnPlayerDied)
            PlayerHealth.RespawnOverride = null;
    }

    void Update()
    {
        if (state == State.Waiting)
        {
            if (player != null && player.position.x > startX)
                StartCoroutine(Intro());
            return;
        }

        Animate();

        if (state == State.Attacking)
        {
            heat += Time.deltaTime / Current.heatTime;
            if (bar != null) bar.SetHeat(heat);
            if (heat >= 1f) Stun();
        }
    }

    public void TakeShot()
    {
        if (state != State.Attacking) return;
        heat += heatPerShot / Current.heatTime;
        StartCoroutine(Flash(new Color(1f, 0.8f, 0.6f), 0.05f));
    }

    IEnumerator Intro()
    {
        state = State.Intro;

        if (arenaDoor != null)
        {
            arenaDoor.gameObject.SetActive(true);
            arenaDoor.Close();
        }

        if (cameraFollow != null)
        {
            if (cameraFocus != null) cameraFollow.SetSecondaryTarget(cameraFocus, 0.85f);
            cameraFollow.SetZoom(cameraZoom);
        }

        PlayerHealth.RespawnOverride = OnPlayerDied;

        yield return PlayMoment(momentBeforeEntrance);

        if (bar != null)
        {
            bar.Show(bossName, phases.Length);
            bar.SetStatus("El programa que tuvo encadenada a KIRA 51 años.");
        }

        yield return MoveTo((leftX + rightX) * 0.5f, hoverY, 2.2f);
        yield return new WaitForSeconds(0.6f);

        yield return PlayMoment(momentAfterEntrance);
        yield return new WaitForSeconds(0.6f);

        BeginPhase();
    }

    IEnumerator PlayMoment(string id)
    {
        if (string.IsNullOrEmpty(id)) yield break;
        if (dialogue == null) dialogue = FindFirstObjectByType<DialogueManager>();
        if (dialogue == null) yield break;

        if (!dialogue.StartMoment(id)) yield break;
        while (dialogue.IsActive) yield return null;
    }

    void BeginPhase()
    {
        heat = 0f;
        state = State.Attacking;
        if (bar != null)
        {
            bar.SetHeat(0f);
            bar.SetStatus("Esquivá sus ataques. Atacando se recalienta, y si le disparás, más rápido.");
        }
        StartCoroutine(AttackLoop());
    }

    IEnumerator AttackLoop()
    {
        while (state == State.Attacking)
        {
            Phase p = Current;
            float x = Random.Range(leftX, rightX);
            yield return MoveTo(x, hoverY, p.moveSpeed);
            yield return Volley(p);
            yield return new WaitForSeconds(p.pause);

            if (p.bombs)
            {
                yield return BombRun(p);
                yield return new WaitForSeconds(p.pause);
            }
        }
    }

    IEnumerator Volley(Phase p)
    {
        yield return Telegraph(0.45f);
        if (player == null || shotPrefab == null) yield break;

        Vector2 from = Muzzle();
        Vector2 aim = ((Vector2)player.position + Vector2.up * 0.15f - from).normalized;
        int n = Mathf.Max(1, p.shotsPerVolley);
        const float spread = 16f;
        for (int i = 0; i < n; i++)
        {
            float angle = (i - (n - 1) * 0.5f) * spread;
            Vector2 dir = Quaternion.Euler(0f, 0f, angle) * aim;
            BossShot s = Instantiate(shotPrefab, from, Quaternion.identity);
            s.Launch(dir * p.shotSpeed);
        }
    }

    IEnumerator BombRun(Phase p)
    {
        bool fromLeft = transform.position.x > (leftX + rightX) * 0.5f;
        float startXRun = fromLeft ? leftX : rightX;
        float endXRun = fromLeft ? rightX : leftX;

        yield return MoveTo(startXRun, hoverY, p.moveSpeed * 1.5f);
        yield return Telegraph(0.35f);

        float timer = 0f;
        while (Mathf.Abs(transform.position.x - endXRun) > 0.05f)
        {
            transform.position = Vector3.MoveTowards(transform.position, new Vector3(endXRun, hoverY, 0f), p.moveSpeed * 1.8f * Time.deltaTime);
            timer += Time.deltaTime;
            if (timer >= p.bombEvery)
            {
                timer = 0f;
                if (bombPrefab != null)
                {
                    BossShot b = Instantiate(bombPrefab, Muzzle(), Quaternion.identity);

                    b.Launch(new Vector2(Mathf.Sign(endXRun - startXRun) * 0.6f, 0f));
                }
            }
            yield return null;
        }
    }

    void Stun()
    {
        StopAllCoroutines();
        BossShot.ClearAll();
        state = State.Stunned;
        heat = 1f;
        if (body != null) body.color = Color.white;
        if (bar != null)
        {
            bar.SetHeat(1f);
            bar.SetStatus("¡Se recalentó y abrió el núcleo! Hackealo desde la consola (E).");
        }
        StartCoroutine(StunRoutine());
    }

    IEnumerator StunRoutine()
    {
        yield return MoveTo(stunnedX, stunnedY, 3f);
        yield return PlayFrames(0, stunnedFrame, 0.12f);
        ConsoleOn(true);

        while (state == State.Stunned)
        {
            if (body != null) body.color = Random.value < 0.5f ? Color.white : new Color(1f, 0.85f, 0.7f);
            yield return new WaitForSeconds(0.08f);
        }
    }

    void OnConsoleSolved()
    {
        if (state != State.Stunned) return;

        StopAllCoroutines();
        ConsoleOn(false);
        if (body != null) body.color = Color.white;

        phase++;
        if (phase >= phases.Length)
        {
            StartCoroutine(Defeat());
            return;
        }

        if (bar != null) bar.SetSegments(phases.Length - phase);
        StartCoroutine(Recover());
    }

    IEnumerator Recover()
    {
        state = State.Hurt;
        int left = phases.Length - phase;
        if (bar != null)
            bar.SetStatus(left == 1 ? "¡Le rompiste otro tramo! Le queda uno." : "¡Le rompiste un tramo! Le quedan " + left + ".");

        if (playerHealth != null) playerHealth.Heal(playerHealth.maxHealth);

        yield return Shake(0.6f, 0.08f);
        yield return PlayFrames(stunnedFrame, 0, 0.1f);
        yield return MoveTo(transform.position.x, hoverY, 2f);
        yield return new WaitForSeconds(0.8f);

        BeginPhase();
    }

    IEnumerator Defeat()
    {
        state = State.Dead;
        if (PlayerHealth.RespawnOverride == (System.Action<PlayerHealth>)OnPlayerDied)
            PlayerHealth.RespawnOverride = null;

        TerminalChallenge last = phases[phases.Length - 1].challenge;
        if (last != null && !string.IsNullOrEmpty(last.expectedOutput))
        {
            foreach (string line in last.expectedOutput.Split('\n'))
            {
                if (bar != null) bar.SetStatus(line);
                yield return Shake(0.35f, 0.14f);
                yield return new WaitForSeconds(0.3f);
            }
        }

        if (bar != null)
        {
            bar.SetSegments(0);
            bar.SetStatus("¡El Custodio se rompió! KIRA es libre.");
        }

        yield return Shake(1.2f, 0.12f);

        if (bodyCollider != null) bodyCollider.enabled = false;
        StartCoroutine(PlayFrames(stunnedFrame, openFrames.Length - 1, 0.12f));

        yield return MoveTo(transform.position.x, groundY + 1.05f, 2.5f);

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / 1.2f;
            if (body != null) body.color = new Color(1f, 1f, 1f, 1f - t);
            yield return null;
        }
        if (body != null) body.enabled = false;

        if (arenaDoor != null) arenaDoor.Open();
        if (cameraFollow != null)
        {
            cameraFollow.ClearSecondaryTarget();
            cameraFollow.ResetZoom();
        }
        if (bar != null) bar.SetStatus("Seguí hacia la derecha: la salida está abierta.");

        if (onDefeated != null) onDefeated.Invoke();

        yield return new WaitForSeconds(4f);
        if (bar != null) bar.Hide();
    }

    void OnPlayerDied(PlayerHealth health)
    {
        StopAllCoroutines();
        BossShot.ClearAll();
        ConsoleOn(false);
        customFrames = false;
        if (body != null) body.color = Color.white;

        if (respawnPoint != null) health.Revive(respawnPoint.position);
        else health.Revive(health.transform.position);

        transform.position = new Vector3((leftX + rightX) * 0.5f, hoverY, 0f);
        StartCoroutine(Retry());
    }

    IEnumerator Retry()
    {
        state = State.Hurt;
        if (bar != null)
        {
            bar.SetHeat(0f);
            bar.SetStatus("Te volteó. Esta parte empieza de nuevo: lo que ya resolviste, queda.");
        }
        yield return new WaitForSeconds(2f);
        BeginPhase();
    }

    void ConsoleOn(bool on)
    {
        if (console != null)
        {
            if (on && phase < phases.Length) console.challenge = phases[phase].challenge;
            console.enabled = on;
            if (!on && console.promptIndicator != null) console.promptIndicator.SetActive(false);
        }

        if (consoleScreen != null) consoleScreen.color = on ? Color.white : new Color(0.3f, 0.3f, 0.38f, 1f);
        if (consoleFlipbook != null) consoleFlipbook.enabled = on;
    }

    Vector2 Muzzle()
    {
        return (Vector2)transform.position + Vector2.down * 0.62f;
    }

    IEnumerator MoveTo(float x, float y, float speed)
    {
        Vector3 target = new Vector3(x, y, 0f);
        while ((transform.position - target).sqrMagnitude > 0.0025f)
        {
            transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
            yield return null;
        }
        transform.position = target;
    }

    IEnumerator Telegraph(float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            if (body != null) body.color = Mathf.Repeat(t, 0.12f) < 0.06f ? new Color(1f, 0.35f, 0.35f) : Color.white;
            t += Time.deltaTime;
            yield return null;
        }
        if (body != null) body.color = Color.white;
    }

    IEnumerator Flash(Color c, float duration)
    {
        if (body == null) yield break;
        body.color = c;
        yield return new WaitForSeconds(duration);
        if (state == State.Attacking) body.color = Color.white;
    }

    IEnumerator Shake(float duration, float amount)
    {
        if (body == null) yield break;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            body.transform.localPosition = Random.insideUnitCircle * amount;
            body.color = Mathf.Repeat(t, 0.16f) < 0.08f ? Color.white : new Color(1f, 0.5f, 0.45f);
            yield return null;
        }
        body.transform.localPosition = Vector3.zero;
        body.color = Color.white;
    }

    IEnumerator PlayFrames(int from, int to, float frameTime)
    {
        if (body == null || openFrames == null || openFrames.Length == 0) yield break;
        customFrames = true;
        int step = to >= from ? 1 : -1;
        for (int i = from; ; i += step)
        {
            body.sprite = openFrames[Mathf.Clamp(i, 0, openFrames.Length - 1)];
            if (i == to) break;
            yield return new WaitForSeconds(frameTime);
        }

        if (to == 0) customFrames = false;
    }

    void Animate()
    {
        if (body == null) return;

        if (!customFrames && idleFrames != null && idleFrames.Length > 0)
        {
            animTimer += Time.deltaTime;
            if (animTimer >= 0.1f)
            {
                animTimer = 0f;
                idleIndex = (idleIndex + 1) % idleFrames.Length;
                body.sprite = idleFrames[idleIndex];
            }
        }

        if (state == State.Attacking || state == State.Intro)
            body.transform.localPosition = new Vector3(0f, Mathf.Sin(Time.time * 2.2f) * 0.06f, 0f);
    }
}
