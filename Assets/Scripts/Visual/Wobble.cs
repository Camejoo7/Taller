using UnityEngine;

public class Wobble : MonoBehaviour
{
    public float amount = 0.04f;

    public float stretch = 0.2f;

    public float speed = 1.5f;

    private Vector3 basePos;
    private Vector3 baseScale;
    private float seed;

    void Awake()
    {
        basePos = transform.localPosition;
        baseScale = transform.localScale;
        seed = Random.value * 100f;
    }

    void Update()
    {
        float t = Time.time * speed;
        float dx = (Mathf.PerlinNoise(t, seed) - 0.5f) * 2f * amount;
        float sx = 1f + (Mathf.PerlinNoise(seed, t * 1.3f) - 0.5f) * 2f * stretch;
        transform.localPosition = basePos + new Vector3(dx, 0f, 0f);
        transform.localScale = new Vector3(baseScale.x * sx, baseScale.y, baseScale.z);
    }
}
