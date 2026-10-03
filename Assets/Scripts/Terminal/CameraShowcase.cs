using System.Collections;
using UnityEngine;

/// <summary>
/// Lleva la cámara a mirar otra parte del mapa por unos segundos y la
/// devuelve. Se engancha al onSolved de una terminal cuando la consecuencia
/// pasa LEJOS de la consola: si los nodos se prenden fuera de cámara, para el
/// jugador no pasó nada.
///
/// Usa lo que CameraFollow ya tenía para el combate (SetZoom y
/// SetSecondaryTarget), así que no pelea con el seguimiento normal.
/// </summary>
public class CameraShowcase : MonoBehaviour
{
    [Tooltip("Qué mirar. Si queda vacío, este mismo objeto.")]
    public Transform focus;

    [Tooltip("Tamaño ortográfico mientras mira (el normal es 1,8). Más grande = más lejos.")]
    public float zoom = 2.6f;

    [Tooltip("0 = sigue mirando al jugador, 1 = centra del todo en el foco.")]
    [Range(0f, 1f)] public float weight = 1f;

    [Tooltip("Cuánto se queda mirando.")]
    public float duration = 3.5f;

    private bool shown;

    public void Show()
    {
        if (shown) return;
        shown = true;
        StartCoroutine(Run());
    }

    IEnumerator Run()
    {
        Camera cam = Camera.main;
        CameraFollow follow = cam != null ? cam.GetComponent<CameraFollow>() : null;
        if (follow == null) yield break;

        follow.SetSecondaryTarget(focus != null ? focus : transform, weight);
        follow.SetZoom(zoom);

        yield return new WaitForSeconds(duration);

        follow.ClearSecondaryTarget();
        follow.ResetZoom();
    }
}
