using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class SpriteFlipbook : MonoBehaviour
{
    public Sprite[] frames;

    public float frameTime = 0.18f;

    public bool randomStart = true;

    private SpriteRenderer sr;
    private int index;
    private float timer;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (frames != null && frames.Length > 0)
        {
            if (randomStart) index = Random.Range(0, frames.Length);
            sr.sprite = frames[index];
        }
    }

    void Update()
    {
        if (frames == null || frames.Length < 2) return;

        timer += Time.deltaTime;
        if (timer < frameTime) return;

        timer -= frameTime;
        index = (index + 1) % frames.Length;
        sr.sprite = frames[index];
    }
}
