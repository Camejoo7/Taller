using UnityEngine;

public class Drifter : MonoBehaviour
{
    public float speed = 0.5f;

    public float minX = -10f;
    public float maxX = 10f;

    public float bobAmount = 0f;
    public float bobSpeed = 1f;

    public bool faceDirection = true;

    private float baseY;
    private float seed;

    void Awake()
    {
        baseY = transform.localPosition.y;
        seed = Random.value * 10f;
        if (faceDirection)
        {
            Vector3 s = transform.localScale;
            s.x = Mathf.Abs(s.x) * (speed < 0f ? -1f : 1f);
            transform.localScale = s;
        }
    }

    void Update()
    {
        Vector3 p = transform.localPosition;
        p.x += speed * Time.deltaTime;
        float w = maxX - minX;
        if (w > 0f)
        {
            if (p.x > maxX) p.x -= w;
            else if (p.x < minX) p.x += w;
        }
        if (bobAmount > 0f) p.y = baseY + Mathf.Sin((Time.time + seed) * bobSpeed) * bobAmount;
        transform.localPosition = p;
    }
}
