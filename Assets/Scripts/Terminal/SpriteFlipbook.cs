using UnityEngine;

/// <summary>
/// Pasa un SpriteRenderer por una lista de frames en loop. Es para los objetos
/// animados de los packs, que vienen como tiras de sprites ya cortadas: montar
/// un Animator + AnimatorController para una pantalla que parpadea es más
/// archivos de los que el objeto se merece.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class SpriteFlipbook : MonoBehaviour
{
    [Tooltip("Los frames, en orden. Se repiten en loop.")]
    public Sprite[] frames;

    [Tooltip("Segundos por frame.")]
    public float frameTime = 0.18f;

    [Tooltip("Arranca en un frame al azar. Si hay varios objetos iguales en " +
             "pantalla, sin esto parpadean todos al mismo tiempo y se nota.")]
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
