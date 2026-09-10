using UnityEngine;
using UnityEngine.UI;

public class AutoScrollUI : MonoBehaviour
{
    public float velocidad = 0.1f;
    private RawImage imagenUI;
    private Rect rectActual;

    void Start()
    {
        imagenUI = GetComponent<RawImage>();
        rectActual = imagenUI.uvRect;
    }

    void Update()
    {
        rectActual.x += velocidad * Time.deltaTime;
        imagenUI.uvRect = rectActual;
    }
}