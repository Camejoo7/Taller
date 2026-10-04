using UnityEngine;

/// <summary>
/// Mueve el objeto de costado a velocidad constante y, cuando sale del tramo
/// [minX, maxX] (coordenadas locales del padre), lo vuelve a entrar por el
/// otro lado. Sirve para los autos voladores del cielo, la niebla que se
/// arrastra o un barco que cruza el puerto a lo lejos.
///
/// Si el padre tiene un ParallaxLayer, todo el tránsito se mueve con la
/// profundidad de esa capa y esto solo suma el viaje propio.
/// </summary>
public class Drifter : MonoBehaviour
{
    [Tooltip("Unidades por segundo. Negativo = hacia la izquierda.")]
    public float speed = 0.5f;

    public float minX = -10f;
    public float maxX = 10f;

    [Tooltip("Sube y baja un poco mientras viaja (0 = recto).")]
    public float bobAmount = 0f;
    public float bobSpeed = 1f;

    [Tooltip("Da vuelta el sprite según para dónde viaja.")]
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
