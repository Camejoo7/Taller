using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// El jefe final: EL CUSTODIO, el programa de NEXCORP que tuvo encadenada a
/// Kira 51 años. Está al final de la zona verde.
///
/// Cómo se pelea (lo que se acordó el 16/10, ver la Fase 2B del ROADMAP):
/// 1. Ataca y el jugador esquiva. Mientras ataca se va recalentando; los
///    tiros del jugador lo recalientan más rápido, pero NO le sacan vida.
/// 2. Recalentado, baja, abre el núcleo y se queda quieto, sin atacar.
/// 3. La consola de la arena se prende con el código de esa fase. Con la
///    terminal abierta el juego se congela: pensar no tiene apuro.
/// 4. Acertar le rompe un tramo de la barra. Equivocarse no saca vida: la
///    dificultad la ponen los ataques, no el castigo al error.
/// 5. Son tres fases, cada una con ataques más difíciles. Si el jugador
///    muere, reaparece en la arena y el jefe vuelve al principio de ESA
///    fase: lo que ya resolvió no se vuelve a escribir.
///
/// La pelea arranca sola cuando el jugador pasa <see cref="startX"/>.
/// </summary>
public class BossController : MonoBehaviour
{
    [System.Serializable]
    public class Phase
    {
        [Tooltip("Segundos atacando hasta recalentarse y quedar aturdido.")]
        public float heatTime = 12f;

        [Tooltip("Pausa entre un ataque y el siguiente.")]
        public float pause = 1.4f;

        [Tooltip("Cuántas bolas por disparo: 1 va derecho al jugador, 3 es un abanico.")]
        public int shotsPerVolley = 1;

        public float shotSpeed = 3.2f;

        [Tooltip("Si además hace pasadas soltando bombas.")]
        public bool bombs = false;

        [Tooltip("Cada cuántos segundos suelta una bomba durante la pasada.")]
        public float bombEvery = 0.6f;

        [Tooltip("Qué tan rápido se mueve de un lado al otro.")]
        public float moveSpeed = 1.4f;

        [Tooltip("El ejercicio que aparece en la consola cuando queda aturdido.")]
        public TerminalChallenge challenge;
    }

    [Header("Nombre")]
    public string bossName = "EL CUSTODIO";

    [Header("Dibujo (pack DRONES, dron 5)")]
    public SpriteRenderer body;

    [Tooltip("Idle_0..3: los propulsores andando.")]
    public Sprite[] idleFrames;

    [Tooltip("Death_0..5: la caja se abre. Los primeros cuadros son el núcleo " +
             "expuesto (aturdido); los últimos, la caja rota (derrota).")]
    public Sprite[] openFrames;

    [Tooltip("Hasta qué cuadro de openFrames se abre cuando queda aturdido.")]
    public int stunnedFrame = 3;

    [Header("La arena")]
    [Tooltip("La pelea arranca cuando el jugador pasa esta x.")]
    public float startX = 125.6f;

    [Tooltip("Entre qué x se mueve volando.")]
    public float leftX = 126.5f;
    public float rightX = 133.5f;

    [Tooltip("Altura a la que vuela atacando.")]
    public float hoverY = -0.9f;

    [Tooltip("Adónde va a quedarse cuando se recalienta (lejos de la consola).")]
    public float stunnedX = 132.6f;
    public float stunnedY = -1.75f;

    [Tooltip("Altura del piso de la arena: ahí se estrella cuando pierde.")]
    public float groundY = -3.16f;

    [Tooltip("Adónde mira la cámara durante la pelea.")]
    public Transform cameraFocus;

    [Tooltip("Zoom de la cámara en la pelea. 2,7 entra la arena entera y el " +
             "pixel art sigue nítido (cada pixel del dibujo = 4 de pantalla).")]
    public float cameraZoom = 2.7f;

    [Tooltip("Dónde reaparece el jugador si muere en la pelea.")]
    public Transform respawnPoint;

    [Tooltip("La puerta que se cierra detrás del jugador al empezar.")]
    public PoweredDoor arenaDoor;

    [Header("La consola")]
    public CodeTerminal console;

    [Tooltip("El dibujo de la pantalla de la consola: apagado hasta que el " +
             "jefe queda aturdido.")]
    public SpriteRenderer consoleScreen;

    [Header("Ataques")]
    public BossShot shotPrefab;
    public BossShot bombPrefab;

    [Tooltip("Cuánto se recalienta por cada tiro del jugador, en segundos de " +
             "ataque. Disparar no le saca vida, pero acorta la espera.")]
    public float heatPerShot = 0.35f;

    public Phase[] phases = new Phase[3];

    [Header("Barra")]
    public BossHealthBar bar;

    [Header("Al ganarle")]
    [Tooltip("Lo que pasa cuando cae: se habilita la salida del mapa, etc.")]
    public UnityEvent onDefeated = new UnityEvent();

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

    // ------------------------------------------------------------ arranque

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
            // Resolver la consola es lo único que lo lastima.
            console.onSolved.AddListener(OnConsoleSolved);
            console.reusable = true;
            console.freezeTime = true;
        }
        if (consoleScreen != null) consoleFlipbook = consoleScreen.GetComponent<SpriteFlipbook>();
        ConsoleOn(false);

        // Espera fuera de cámara, arriba de la arena.
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

    /// <summary>Lo llama la bala del jugador al pegarle.</summary>
    public void TakeShot()
    {
        if (state != State.Attacking) return;
        heat += heatPerShot / Current.heatTime;
        StartCoroutine(Flash(new Color(1f, 0.8f, 0.6f), 0.05f));
    }

    // ------------------------------------------------------------ las partes de la pelea

    IEnumerator Intro()
    {
        state = State.Intro;

        // La puerta se cierra detrás: de acá no se sale hasta ganarle.
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

        // Si te mata, volvés a la arena, no al garage.
        PlayerHealth.RespawnOverride = OnPlayerDied;

        if (bar != null)
        {
            bar.Show(bossName, phases.Length);
            bar.SetStatus("El programa que tuvo encadenada a Kira 51 años.");
        }

        // Baja desde arriba de la pantalla.
        yield return MoveTo((leftX + rightX) * 0.5f, hoverY, 2.2f);
        yield return new WaitForSeconds(1.2f);

        BeginPhase();
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

    /// <summary>Se pone rojo un momento (el aviso) y tira bolas hacia el jugador.</summary>
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

    /// <summary>Cruza la arena de punta a punta soltando bombas.</summary>
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
                    // Sale con un poquito de la velocidad del jefe: cae en
                    // diagonal suave, no a plomo.
                    b.Launch(new Vector2(Mathf.Sign(endXRun - startXRun) * 0.6f, 0f));
                }
            }
            yield return null;
        }
    }

    /// <summary>Se recalentó: baja, abre el núcleo y prende la consola.</summary>
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

        // Mientras espera, el núcleo chisporrotea.
        while (state == State.Stunned)
        {
            if (body != null) body.color = Random.value < 0.5f ? Color.white : new Color(1f, 0.85f, 0.7f);
            yield return new WaitForSeconds(0.08f);
        }
    }

    /// <summary>Lo llama la consola al resolverse.</summary>
    void OnConsoleSolved()
    {
        if (state != State.Stunned) return;

        StopAllCoroutines();
        ConsoleOn(false);
        if (body != null) body.color = Color.white;

        phase++;
        if (bar != null) bar.SetSegments(phases.Length - phase);

        if (phase >= phases.Length) StartCoroutine(Defeat());
        else StartCoroutine(Recover());
    }

    /// <summary>Le pegaste: se sacude, cierra el núcleo y vuelve a subir.</summary>
    IEnumerator Recover()
    {
        state = State.Hurt;
        int left = phases.Length - phase;
        if (bar != null)
            bar.SetStatus(left == 1 ? "¡Le rompiste otro tramo! Le queda uno." : "¡Le rompiste un tramo! Le quedan " + left + ".");

        // De premio, los corazones llenos: la fase que viene es más difícil.
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
        if (bar != null) bar.SetStatus("¡El Custodio se rompió! Kira es libre.");

        yield return Shake(1.2f, 0.12f);

        // Se abre del todo y se viene abajo.
        if (bodyCollider != null) bodyCollider.enabled = false;
        StartCoroutine(PlayFrames(stunnedFrame, openFrames.Length - 1, 0.12f));
        // (abierta, la caja cuelga ~1 unidad por debajo de su centro)
        yield return MoveTo(transform.position.x, groundY + 1.05f, 2.5f);

        // Se apaga.
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

    /// <summary>
    /// El jugador se quedó sin corazones en la pelea: reaparece en la arena y
    /// esta fase empieza de nuevo. Las fases ya ganadas siguen ganadas.
    /// </summary>
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

    // ------------------------------------------------------------ ayudas

    void ConsoleOn(bool on)
    {
        if (console != null)
        {
            if (on && phase < phases.Length) console.challenge = phases[phase].challenge;
            console.enabled = on;
            if (!on && console.promptIndicator != null) console.promptIndicator.SetActive(false);
        }
        // Apagada se ve apagada: si la pantalla brillara todo el tiempo, el
        // jugador iría a la consola cuando no sirve.
        if (consoleScreen != null) consoleScreen.color = on ? Color.white : new Color(0.3f, 0.3f, 0.38f, 1f);
        if (consoleFlipbook != null) consoleFlipbook.enabled = on;
    }

    /// <summary>De dónde salen los disparos: la panza de la caja.</summary>
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

    /// <summary>El aviso antes de atacar: titila en rojo. Es lo que lo hace esquivable.</summary>
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

    /// <summary>Pasa por los cuadros de openFrames de 'from' a 'to' (para adelante o para atrás).</summary>
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
        // Cerrado del todo, vuelve a la animación de vuelo.
        if (to == 0) customFrames = false;
    }

    /// <summary>Los propulsores (idle) y un vaivén suave mientras vuela.</summary>
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
