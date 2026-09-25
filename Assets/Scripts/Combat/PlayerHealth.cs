using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// La vida del jugador. Implementa <see cref="IPlayerDamageable"/>, que era el
/// enganche que el combate Code Blaster ya venía llamando sin que nadie
/// contestara: desde ahora, errarle a un fragmento también duele.
///
/// Al recibir un golpe el jugador queda un rato invulnerable y parpadeando. Sin
/// eso, un dron pegado encima vaciaría las tres vidas en medio segundo y no
/// habría forma de reaccionar.
/// </summary>
public class PlayerHealth : MonoBehaviour, IPlayerDamageable
{
    [Header("Vida")]
    public int maxHealth = 3;

    [Tooltip("Segundos de gracia después de un golpe, parpadeando.")]
    public float invulnerableTime = 1.5f;

    [Tooltip("Gracia al reaparecer. Más larga que la normal porque se reaparece " +
             "en el último piso firme, que puede estar al lado del dron que te mató.")]
    public float respawnInvulnerableTime = 2.5f;

    [Header("Golpe")]
    [Tooltip("Cuánto empuja el golpe al jugador, para despegarlo del enemigo.")]
    public float knockbackForce = 4.5f;

    [Header("Muerte")]
    [Tooltip("Punto fijo donde reaparecer. Si está vacío (lo normal), reaparece " +
             "en el último piso firme que pisó, que es mucho menos frustrante " +
             "que mandarlo al principio del nivel.")]
    public Transform respawnPoint;

    public float respawnDelay = 1.2f;

    public int Current { get; private set; }
    public bool IsDead { get; private set; }
    public bool IsInvulnerable { get { return Time.time < invulnerableUntil; } }

    /// <summary>Avisa a la UI de corazones que algo cambió.</summary>
    public event Action OnHealthChanged;

    private Rigidbody2D rb;
    private PlayerMovement movement;
    private SpriteRenderer visual;
    private float invulnerableUntil;
    private Vector3 startPosition;
    private Vector3 lastSafeGround;
    private float nextSafeSample;
    private Coroutine blinking;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        movement = GetComponent<PlayerMovement>();
        visual = GetComponentInChildren<SpriteRenderer>();
        startPosition = transform.position;
        lastSafeGround = startPosition;
        Current = maxHealth;
    }

    void Start()
    {
        Raise();
    }

    void Update()
    {
        // Va anotando el último piso firme. Reaparecer ahí es mucho menos
        // castigador que mandarlo al principio, y no hace falta ir poniendo
        // puntos de control a mano por todo el nivel.
        if (IsDead || movement == null || Time.time < nextSafeSample) return;

        nextSafeSample = Time.time + 0.4f;

        if (movement.IsGrounded)
            lastSafeGround = transform.position;
    }

    /// <summary>Golpe sin origen conocido (por ejemplo, errar una respuesta).</summary>
    public void TakeDamage(int amount)
    {
        ApplyDamage(amount, null);
    }

    /// <summary>Golpe con origen, para que el empujón salga en la dirección correcta.</summary>
    public void TakeDamageFrom(int amount, Vector2 source)
    {
        ApplyDamage(amount, source);
    }

    void ApplyDamage(int amount, Vector2? source)
    {
        if (IsDead || IsInvulnerable || amount <= 0) return;

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
        if (rb != null) rb.linearVelocity = Vector2.zero;
        if (blinking != null) StopCoroutine(blinking);
        if (visual != null) visual.enabled = false;

        yield return new WaitForSeconds(respawnDelay);

        transform.position = respawnPoint != null ? respawnPoint.position : lastSafeGround;
        if (rb != null) rb.linearVelocity = Vector2.zero;

        Current = maxHealth;
        IsDead = false;
        invulnerableUntil = Time.time + respawnInvulnerableTime;

        if (visual != null) visual.enabled = true;
        if (movement != null) movement.SetCanMove(true);

        Raise();
        blinking = StartCoroutine(Blink(respawnInvulnerableTime));
    }

    void Raise()
    {
        if (OnHealthChanged != null)
            OnHealthChanged();
    }
}
