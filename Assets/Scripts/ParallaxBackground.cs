using UnityEngine;

/// <summary>
/// Parallax simple para fondos 2D. Mueve cada capa una fracción del
/// desplazamiento de la cámara: factor 1 = pegada a la cámara (infinitamente
/// lejos), factor 0 = fija en el mundo (primer plano).
/// </summary>
public class ParallaxBackground : MonoBehaviour
{
    [System.Serializable]
    public class Layer
    {
        public Transform target;
        [Range(0f, 1f)] public float factor = 0.5f;
        public bool lockY = true;
    }

    public Transform cam;
    public Layer[] layers;

    private Vector3 lastCamPos;

    void Start()
    {
        if (cam == null && Camera.main != null)
            cam = Camera.main.transform;
        if (cam != null)
            lastCamPos = cam.position;
    }

    void LateUpdate()
    {
        if (cam == null) return;

        Vector3 delta = cam.position - lastCamPos;
        lastCamPos = cam.position;

        foreach (Layer l in layers)
        {
            if (l.target == null) continue;
            float dy = l.lockY ? 0f : delta.y * l.factor;
            l.target.position += new Vector3(delta.x * l.factor, dy, 0f);
        }
    }
}
