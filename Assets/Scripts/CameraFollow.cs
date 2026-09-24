using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public float smoothSpeed = 5f;

    [Tooltip("Qué tan rápido se abre o cierra el zoom.")]
    public float zoomSpeed = 2.5f;

    private Camera cam;
    private float baseSize;
    private float targetSize;
    private Transform secondaryTarget;
    private float secondaryWeight;

    void Awake()
    {
        cam = GetComponent<Camera>();
        if (cam != null)
        {
            baseSize = cam.orthographicSize;
            targetSize = baseSize;
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 focus = target.position;

        // Durante el combate la cámara mira al punto medio entre el jugador y el
        // dron. Si siguiera solo al jugador, el dron y los fragmentos de arriba
        // quedarían tapados por el cartel de la pregunta.
        if (secondaryTarget != null)
            focus = Vector3.Lerp(target.position, secondaryTarget.position, secondaryWeight);

        Vector3 targetPos = new Vector3(focus.x, focus.y, transform.position.z);
        transform.position = Vector3.Lerp(transform.position, targetPos, smoothSpeed * Time.deltaTime);

        if (cam != null && Mathf.Abs(cam.orthographicSize - targetSize) > 0.001f)
            cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetSize, zoomSpeed * Time.deltaTime);
    }

    /// <summary>
    /// Aleja la cámara durante el combate. A la escala de este juego el dron con
    /// los fragmentos girando no entra en la vista normal, así que la pelea abre
    /// el encuadre y al terminar lo devuelve.
    /// </summary>
    public void SetZoom(float size)
    {
        targetSize = size > 0f ? size : baseSize;
    }

    public void ResetZoom()
    {
        targetSize = baseSize;
    }

    /// <summary>
    /// Hace que la cámara encuadre al jugador y a otro punto a la vez.
    /// <paramref name="weight"/> 0 = solo el jugador, 1 = solo el otro punto.
    /// </summary>
    public void SetSecondaryTarget(Transform other, float weight)
    {
        secondaryTarget = other;
        secondaryWeight = Mathf.Clamp01(weight);
    }

    public void ClearSecondaryTarget()
    {
        secondaryTarget = null;
        secondaryWeight = 0f;
    }
}
