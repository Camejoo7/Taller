using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// La vida del jugador. Implementa <see cref="IPlayerDamageable"/>, que era el
/// enganche que el combate Code Blaster ya venía llamando sin que nadie
/// contestara: desde ahora, errarle a un fragmento también duele.
///
/// Al recibir un golpe el jugador queda un rato invulnerable y parpadeando. Sin
/// eso, un dron pegado encima vaciaría las tres vidas en medio segundo y no
/// habría forma de reaccionar.
///
/// Cuando se queda sin corazones se reinicia la stage entera. Antes reaparecía
/// en el último piso firme con la vida llena, que en los hechos hacía que perder
/// no costara nada.
/// </summary>
public class PlayerHealth : MonoBehaviour, IPlayerDamageable
{
    [Header("Vida")]
    public int maxHealth = 3;

    [Tooltip("Segundos de gracia después de un golpe, parpadeando.")]
    public float invulnerableTime = 1.5f;

    [Header("Golpe")]
    [Tooltip("Cuánto empuja el golpe al jugador, para despegarlo del enemigo.")]
    public float knockbackForce = 4.5f;

    [Header("Muerte")]
    [Tooltip("Cuánto se espera antes de reiniciar la stage, para que se alcance " +
             "a entender que te quedaste sin vidas y no sea un corte seco.")]
    public float restartDelay = 1.2f;

    public int Current { get; private set; }
    public bool IsDead { get; private set; }
    public bool IsInvulnerable { get { return Time.time < invulnerableUntil; } }

    /// <summary>Avisa a la UI de corazones que algo cambió.</summary>
    public event Action OnHealthChanged;

    private Rigidbody2D rb;
    private PlayerMovement movement;
    private SpriteRenderer visual;
    private float invulnerableUntil;
    private Coroutine blinking;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        movement = GetComponent<PlayerMovement>();
        visual = GetComponentInChildren<SpriteRenderer>();
        Current = maxHealth;
    }

    void Start()
    {
        Raise();
    }

    /// <summary>Golpe sin origen conocido (por ejemplo, errar una respuesta).</summary>
    public void TakeDamage(int amount)
    {
        ApplyDamage(amount, null, false);
    }

    /// <summary>Golpe con origen, para que el empujón salga en la dirección correcta.</summary>
    public void TakeDamageFrom(int amount, Vector2 source)
    {
        ApplyDamage(amount, source, false);
    }

    /// <summary>
    /// Caerse al vacío. Saltea los segundos de gracia a propósito: si no,
    /// caerse justo después de un golpe saldría gratis. Tampoco empuja, porque
    /// al jugador lo van a teletransportar igual.
    /// </summary>
    public void TakeFallDamage(int amount)
    {
        ApplyDamage(amount, null, true);
    }

    void ApplyDamage(int amount, Vector2? source, bool ignoreInvulnerable)
    {
        if (IsDead || amount <= 0) return;
        if (IsInvulnerable && !ignoreInvulnerable) return;

        Current = Mathf.Max(0, Current - amount);
        Raise();

        if (Current <= 0)
        {
            StartCoroutine(Die());
            return;
        }

        invulnerableUntil = Time.time + invulnerableTime;

        if (source.HasValue && rb != null)
        {
            Vector2 away = ((Vector2)transform.position - source.Value).normalized;
            if (away.sqrMagnitude < 0.01f) away = Vector2.up;

            // Un poco hacia arriba siempre, si no el empujón se come el piso
            // y no se nota.
            away = (away + Vector2.up * 0.6f).normalized;
            rb.linearVelocity = away * knockbackForce;
        }

        if (blinking != null) StopCoroutine(blinking);
        blinking = StartCoroutine(Blink(invulnerableTime));
    }

    public void Heal(int amount)
    {
        if (IsDead || amount <= 0) return;

        Current = Mathf.Min(maxHealth, Current + amount);
        Raise();
    }

    IEnumerator Blink(float duration)
    {
        if (visual == null) yield break;

        float end = Time.time + duration;
        while (Time.time < end)
        {
            visual.enabled = !visual.enabled;
            yield return new WaitForSeconds(0.08f);
        }

        visual.enabled = true;
        blinking = null;
    }

    IEnumerator Die()
    {
        IsDead = true;

        if (movement != null) movement.SetCanMove(false);
        if (blinking != null) StopCoroutine(blinking);
        if (visual != null) visual.enabled = false;

        // Lo sacamos de la simulación: si murió cayéndose al vacío, sin esto
        // sigue cayendo sin parar durante la espera.
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.simulated = false;
        }

        yield return new WaitForSeconds(restartDelay);

        // De vuelta al principio de la stage. Recargar la escena deja todo como
        // estaba: enemigos, diálogos y posición de arranque.
        Scene current = SceneManager.GetActiveScene();
        Time.timeScale = 1f;
        SceneManager.LoadScene(current.name);
    }

    void Raise()
    {
        if (OnHealthChanged != null)
            OnHealthChanged();
    }
}
