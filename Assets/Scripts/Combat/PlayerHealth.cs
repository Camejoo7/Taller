using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerHealth : MonoBehaviour, IPlayerDamageable
{
    [Header("Vida")]
    public int maxHealth = 3;

    public float invulnerableTime = 1.5f;

    [Header("Golpe")]
    public float knockbackForce = 4.5f;

    [Header("Muerte")]
    public float restartDelay = 1.2f;

    public int Current { get; private set; }
    public bool IsDead { get; private set; }
    public bool IsInvulnerable { get { return Time.time < invulnerableUntil; } }

    public static Action<PlayerHealth> RespawnOverride;

    static int carried = -1;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetOnPlay() { carried = -1; }

    public static void CarryToNextScene(int hearts) { carried = hearts; }

    public static void ClearCarry() { carried = -1; }

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

        if (carried > 0)
            Current = Mathf.Min(carried, maxHealth);
        carried = -1;
    }

    void Start()
    {
        Raise();
    }

    public void TakeDamage(int amount)
    {
        ApplyDamage(amount, null, false);
    }

    public void TakeDamageFrom(int amount, Vector2 source)
    {
        ApplyDamage(amount, source, false);
    }

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

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.simulated = false;
        }

        yield return new WaitForSeconds(restartDelay);

        if (RespawnOverride != null)
        {
            RespawnOverride(this);
            yield break;
        }

        Scene current = SceneManager.GetActiveScene();
        Time.timeScale = 1f;
        SceneManager.LoadScene(current.name);
    }

    public void Revive(Vector3 position)
    {
        IsDead = false;
        Current = maxHealth;
        Raise();

        transform.position = position;
        if (rb != null)
        {
            rb.simulated = true;
            rb.linearVelocity = Vector2.zero;
        }
        if (visual != null) visual.enabled = true;
        if (movement != null) movement.SetCanMove(true);

        invulnerableUntil = Time.time + invulnerableTime;
        if (blinking != null) StopCoroutine(blinking);
        blinking = StartCoroutine(Blink(invulnerableTime));
    }

    void OnDestroy()
    {
        RespawnOverride = null;
    }

    void Raise()
    {
        if (OnHealthChanged != null)
            OnHealthChanged();
    }
}
