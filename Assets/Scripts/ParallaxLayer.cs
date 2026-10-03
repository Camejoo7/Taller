using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Una capa de fondo que se mueve más lento que la cámara: lo lejano casi la
/// acompaña, lo cercano se queda más atrás. Es lo que le da profundidad al
/// cielo de las zonas de afuera del Stage 2.
///
/// La posición es una función directa de la cámara (no se acumula frame a
/// frame), así que en el editor se ve igual que en el juego y nunca se
/// "corre" con el tiempo:
///
///     posición = origen + cámara × factor
///
/// factor 1 = pegada a la cámara (infinitamente lejos, ej. el cielo).
/// factor 0 = quieta en el mundo (como el escenario).
///
/// Las capas son sprites en modo Tiled muy anchos, así que no hace falta
/// reciclarlas al avanzar: alcanza con que el ancho cubra todo el recorrido.
///
/// Va en la capa de dibujo "Fondo", detrás de los tilemaps. Donde el nivel
/// tiene relleno de fondo propio (el tramo negro del principio del Stage 2)
/// queda tapada sola, sin tener que apagarla.
/// </summary>
[ExecuteAlways]
[DefaultExecutionOrder(100)] // después de CameraFollow, si no tiembla
public class ParallaxLayer : MonoBehaviour
{
    [Tooltip("Cuánto acompaña a la cámara en cada eje. 1 = pegada, 0 = quieta.")]
    public Vector2 factor = new Vector2(0.9f, 0.95f);

    [Tooltip("Dónde está la capa cuando la cámara está en (0,0).")]
    public Vector2 origin;

    [Tooltip("Para el cielo: se estira para tapar siempre toda la vista, " +
             "mida lo que mida la cámara.")]
    public bool fitToView = false;

    [Tooltip("Margen extra al estirar, para que no se vean los bordes al moverse.")]
    public float fitMargin = 1.15f;

    static readonly List<ParallaxLayer> all = new List<ParallaxLayer>();

    void OnEnable() { if (!all.Contains(this)) all.Add(this); }
    void OnDisable() { all.Remove(this); }

    void LateUpdate()
    {
        Camera cam = Camera.main;
        if (cam == null) return;
        Apply(cam.transform.position, cam.orthographicSize, cam.aspect);
    }

    public void Apply(Vector3 camPos, float orthoSize, float aspect)
    {
        Vector3 p = new Vector3(origin.x + camPos.x * factor.x,
                                origin.y + camPos.y * factor.y,
                                transform.position.z);
        transform.position = p;

        if (fitToView)
        {
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr != null && sr.sprite != null)
            {
                Vector2 size = sr.sprite.bounds.size;
                float h = orthoSize * 2f * fitMargin;
                float w = h * aspect;
                transform.localScale = new Vector3(w / size.x, h / size.y, 1f);
            }
        }
    }

    /// <summary>
    /// Acomoda todas las capas como si la cámara del juego estuviera en
    /// camPos. Lo usa la captura de mapas del editor (MapTools.Capture).
    /// </summary>
    public static void ApplyAll(Vector3 camPos, float orthoSize, float aspect)
    {
        foreach (ParallaxLayer l in all)
            if (l != null) l.Apply(camPos, orthoSize, aspect);
    }
}
