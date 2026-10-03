using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Un grupo de capas de fondo que pertenece a una zona del mapa: se ve
/// mientras la cámara está entre fromX y toX y se funde en los bordes. Así
/// cada zona tiene su propio horizonte (la ciudad, la central eléctrica...) y
/// al cruzar de una a otra los fondos se cruzan en vez de cortarse.
///
/// Toca solo el alfa: los colores (el tinte violeta) se guardan la primera
/// vez y se respetan.
/// </summary>
[ExecuteAlways]
[DefaultExecutionOrder(110)] // después de la cámara y del parallax
public class ZoneBackdrop : MonoBehaviour
{
    public float fromX = -1000f;
    public float toX = 1000f;

    [Tooltip("Ancho del fundido en cada borde, en unidades de mundo.")]
    public float fade = 4f;

    [SerializeField, HideInInspector] private SpriteRenderer[] renderers;
    [SerializeField, HideInInspector] private Color[] baseColors;

    static readonly List<ZoneBackdrop> all = new List<ZoneBackdrop>();

    void OnEnable()
    {
        if (!all.Contains(this)) all.Add(this);
        if (renderers == null || renderers.Length == 0) Capture();
    }

    void OnDisable() { all.Remove(this); }

    /// <summary>Guarda los colores actuales como los "de verdad". Llamar después de cambiar tintes.</summary>
    [ContextMenu("Guardar colores")]
    public void Capture()
    {
        renderers = GetComponentsInChildren<SpriteRenderer>(true);
        baseColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++) baseColors[i] = renderers[i].color;
    }

    void LateUpdate()
    {
        Camera cam = Camera.main;
        if (cam != null) Apply(cam.transform.position.x);
    }

    public void Apply(float camX)
    {
        if (renderers == null) return;
        float a = Mathf.Clamp01(Mathf.Min((camX - fromX) / fade + 0.5f, (toX - camX) / fade + 0.5f));
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null) continue;
            Color c = baseColors[i];
            c.a *= a;
            renderers[i].color = c;
        }
    }

    public static void ApplyAll(float camX)
    {
        foreach (ZoneBackdrop z in all) if (z != null) z.Apply(camX);
    }
}
