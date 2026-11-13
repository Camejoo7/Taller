using System.Collections;
using UnityEngine;

public class PoweredDoor : MonoBehaviour
{
    [Header("Apertura")]
    public Sprite[] openFrames;

    public Vector2 openOffset = new Vector2(0f, 0.72f);

    public float openDuration = 1.1f;

    public float shakeDuration = 0.45f;

    public float shakeAmount = 0.02f;

    [Header("Cerrarse")]
    public bool startOpen = false;

    public float closeDuration = 0.25f;

    [Header("Luces")]
    public SpriteRenderer[] indicators;

    public Color lockedColor = new Color(1f, 0.25f, 0.25f, 1f);
    public Color unlockedColor = new Color(0.2f, 1f, 0.55f, 1f);

    public bool hideIndicatorsWhenOpen = true;

    private Collider2D blocker;
    private SpriteRenderer sprite;
    private Sprite closedSprite;
    private Vector3 closedPos;
    private bool opening;

    void Awake()
    {
        blocker = GetComponent<Collider2D>();
        sprite = GetComponent<SpriteRenderer>();
        if (sprite != null) closedSprite = sprite.sprite;
        closedPos = transform.position;
        Tint(lockedColor);
        if (startOpen) ShowOpen();
    }

    void ShowOpen()
    {
        transform.position = closedPos + (Vector3)openOffset;
        if (sprite != null && openFrames != null && openFrames.Length > 0 && openFrames[openFrames.Length - 1] != null)
            sprite.sprite = openFrames[openFrames.Length - 1];
        if (blocker != null) blocker.enabled = false;
        Tint(unlockedColor);
        if (hideIndicatorsWhenOpen && indicators != null)
            for (int i = 0; i < indicators.Length; i++)
                if (indicators[i] != null) indicators[i].enabled = false;
    }

    public void Close()
    {
        StopAllCoroutines();
        opening = false;
        StartCoroutine(CloseRoutine());
    }

    IEnumerator CloseRoutine()
    {
        if (blocker != null) blocker.enabled = true;
        if (indicators != null)
            for (int i = 0; i < indicators.Length; i++)
                if (indicators[i] != null) indicators[i].enabled = true;
        Tint(lockedColor);

        Vector3 from = transform.position;
        bool animar = openFrames != null && openFrames.Length > 0;
        float t = 0f;
        while (t < closeDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / closeDuration);
            transform.position = Vector3.Lerp(from, closedPos, p * p);
            if (animar && sprite != null)
            {
                int i = Mathf.Max(0, openFrames.Length - 1 - Mathf.FloorToInt(p * openFrames.Length));
                if (openFrames[i] != null) sprite.sprite = openFrames[i];
            }
            yield return null;
        }

        transform.position = closedPos;
        if (sprite != null && closedSprite != null) sprite.sprite = closedSprite;

        t = 0f;
        while (t < 0.2f)
        {
            t += Time.deltaTime;
            transform.position = closedPos + (Vector3)(Random.insideUnitCircle * shakeAmount * 1.5f);
            yield return null;
        }
        transform.position = closedPos;
    }

    public void Open()
    {
        if (opening) return;
        opening = true;
        StartCoroutine(OpenRoutine());
    }

    public void CloseInstantly()
    {
        StopAllCoroutines();
        opening = false;
        transform.position = closedPos;
        if (sprite != null && closedSprite != null) sprite.sprite = closedSprite;
        if (blocker != null) blocker.enabled = true;
        if (indicators != null)
            for (int i = 0; i < indicators.Length; i++)
                if (indicators[i] != null) indicators[i].enabled = true;
        Tint(lockedColor);
    }

    IEnumerator OpenRoutine()
    {
        Tint(unlockedColor);

        float t = 0f;
        while (t < shakeDuration)
        {
            t += Time.deltaTime;
            transform.position = closedPos + (Vector3)(Random.insideUnitCircle * shakeAmount);
            yield return null;
        }

        Vector3 target = closedPos + (Vector3)openOffset;
        bool animar = openFrames != null && openFrames.Length > 0;
        t = 0f;

        while (t < openDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / openDuration);

            transform.position = Vector3.Lerp(closedPos, target, Mathf.SmoothStep(0f, 1f, p));

            if (animar && sprite != null)
            {
                int i = Mathf.Min(openFrames.Length - 1, Mathf.FloorToInt(p * openFrames.Length));
                if (openFrames[i] != null) sprite.sprite = openFrames[i];
            }

            yield return null;
        }

        transform.position = target;
        if (animar && sprite != null && openFrames[openFrames.Length - 1] != null)
            sprite.sprite = openFrames[openFrames.Length - 1];

        if (blocker != null) blocker.enabled = false;

        if (hideIndicatorsWhenOpen && indicators != null)
            for (int i = 0; i < indicators.Length; i++)
                if (indicators[i] != null) indicators[i].enabled = false;
    }

    void Tint(Color c)
    {
        if (indicators == null) return;
        for (int i = 0; i < indicators.Length; i++)
            if (indicators[i] != null) indicators[i].color = c;
    }
}
