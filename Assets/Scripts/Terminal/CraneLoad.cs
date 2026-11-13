using System.Collections;
using UnityEngine;

[ExecuteAlways]
public class CraneLoad : MonoBehaviour
{
    public Vector2 lowerOffset = new Vector2(0f, -2.16f);

    public float duration = 1.8f;

    public float shakeDuration = 0.4f;
    public float shakeAmount = 0.025f;

    [Header("Grúa")]
    public Transform cart;

    public SpriteRenderer cable;

    public float hookOffset = -0.35f;

    [Header("Pared invisible")]
    public Collider2D blockUntilLowered;

    private Collider2D solid;
    private SpriteRenderer body;
    private Vector3 hangPos;
    private bool lowered;

    void Awake()
    {
        solid = GetComponent<Collider2D>();
        body = GetComponent<SpriteRenderer>();
        hangPos = transform.position;
        if (Application.isPlaying && solid != null) solid.enabled = false;
    }

    void LateUpdate()
    {
        UpdateCable();
    }

    public void UpdateCable()
    {
        if (cable == null || cart == null || body == null || cable.sprite == null) return;
        float top = cart.position.y + hookOffset;
        float bottom = body.bounds.max.y;
        float len = Mathf.Max(0.01f, top - bottom);
        cable.transform.position = new Vector3(transform.position.x, (top + bottom) * 0.5f, cable.transform.position.z);
        Vector3 s = cable.transform.localScale;
        s.y = len / cable.sprite.bounds.size.y;
        cable.transform.localScale = s;
    }

    public void Lower()
    {
        if (lowered) return;
        lowered = true;
        StartCoroutine(Run());
    }

    IEnumerator Run()
    {
        float t = 0f;
        while (t < shakeDuration)
        {
            t += Time.deltaTime;
            transform.position = hangPos + (Vector3)(Random.insideUnitCircle * shakeAmount);
            yield return null;
        }

        Vector3 target = hangPos + (Vector3)lowerOffset;
        t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;

            transform.position = Vector3.Lerp(hangPos, target, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / duration)));
            yield return null;
        }
        transform.position = target;

        if (solid != null) solid.enabled = true;
        if (blockUntilLowered != null) blockUntilLowered.enabled = false;

        t = 0f;
        while (t < 0.25f)
        {
            t += Time.deltaTime;
            transform.position = target + Vector3.down * Mathf.Sin(t / 0.25f * Mathf.PI) * 0.05f;
            yield return null;
        }
        transform.position = target;
    }
}
