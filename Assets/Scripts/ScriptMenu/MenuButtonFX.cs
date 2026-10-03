using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Hace que un botón del menú reaccione: se agranda un poco, el texto se
/// prende y aparece un ">" titilando a la izquierda, como el cursor de una
/// terminal.
///
/// El hover del mouse y la selección con teclado son LA MISMA cosa: pasar el
/// mouse por encima selecciona el botón. Así nunca hay dos botones marcados a
/// la vez (uno por el mouse y otro por las flechas), que es lo que confunde en
/// los menús de Unity por defecto.
///
/// Usa tiempo sin escalar: si se vuelve al menú desde la pausa con el
/// timeScale en 0, el menú igual tiene que moverse.
/// </summary>
[RequireComponent(typeof(Button))]
public class MenuButtonFX : MonoBehaviour,
    IPointerEnterHandler, ISelectHandler, IDeselectHandler,
    ISubmitHandler, IPointerClickHandler
{
    [Tooltip("El texto del botón. Tiene que ser HIJO del botón, si no queda " +
             "quieto cuando el botón se agranda.")]
    public TMP_Text label;

    [Header("Seleccionado")]
    public float selectedScale = 1.08f;
    public Color labelNormal = new Color(0.755f, 0.755f, 0.755f, 1f);
    [Tooltip("Verde terminal: el mismo idioma visual que la consola del juego.")]
    public Color labelSelected = new Color(0.55f, 1f, 0.8f, 1f);

    [Header("Cursor")]
    public string caretText = ">";
    [Tooltip("Tamaño del cursor relativo al texto del botón.")]
    public float caretSize = 1.5f;
    [Tooltip("Separación entre el cursor y el borde izquierdo del botón.")]
    public float caretGap = 14f;
    [Tooltip("Cuánto se desliza el cursor al aparecer.")]
    public float caretSlide = 18f;
    public float blinkPeriod = 0.55f;

    [Header("Apretar")]
    [Tooltip("Cuánto se achica al apretarlo. Sin esto el clic no se siente.")]
    public float pressSquash = 0.1f;

    private RectTransform rt;
    private Vector3 baseScale;
    private TMP_Text caret;
    private RectTransform caretRt;
    private bool selected;
    private float amount;   // 0 = normal, 1 = seleccionado (suavizado)
    private float press;    // 1 justo al apretar, decae a 0

    void Awake()
    {
        rt = (RectTransform)transform;
        baseScale = rt.localScale;
        if (label == null) label = GetComponentInChildren<TMP_Text>();
        BuildCaret();
    }

    void BuildCaret()
    {
        if (label == null) return;

        GameObject go = new GameObject("Cursor", typeof(RectTransform));
        caretRt = (RectTransform)go.transform;
        caretRt.SetParent(transform, false);
        // Anclado al borde izquierdo, a media altura, con el pivote a la
        // derecha: posicionarlo es decir cuánto queda separado del botón.
        caretRt.anchorMin = caretRt.anchorMax = new Vector2(0f, 0.5f);
        caretRt.pivot = new Vector2(1f, 0.5f);
        caretRt.sizeDelta = new Vector2(40f, 40f);

        caret = go.AddComponent<TextMeshProUGUI>();
        caret.text = caretText;
        caret.font = label.font;
        // En esta fuente pixel el ">" es más chico que las mayúsculas: a igual
        // tamaño casi no se ve.
        caret.fontSize = label.fontSize * caretSize;
        caret.alignment = TextAlignmentOptions.Right;
        caret.textWrappingMode = TextWrappingModes.NoWrap;
        caret.raycastTarget = false;
        caret.color = Clear(labelSelected);

        // El botón está escalado (los sprites del pack son chicos), así que el
        // cursor hereda esa escala. Lo compensamos para que mida lo mismo que
        // el texto del botón.
        float k = label.transform.lossyScale.x / Mathf.Max(0.0001f, transform.lossyScale.x);
        caretRt.localScale = Vector3.one * k;
    }

    void Update()
    {
        float dt = Time.unscaledDeltaTime;

        amount = Mathf.MoveTowards(amount, selected ? 1f : 0f, dt * 7f);
        press = Mathf.MoveTowards(press, 0f, dt * 5f);

        float ease = Mathf.SmoothStep(0f, 1f, amount);
        float s = Mathf.Lerp(1f, selectedScale, ease) - pressSquash * press;
        rt.localScale = baseScale * s;

        if (label != null) label.color = Color.Lerp(labelNormal, labelSelected, ease);

        if (caret != null)
        {
            // Titila como un cursor de verdad: encendido/apagado, no un fundido.
            bool on = Mathf.Repeat(Time.unscaledTime, blinkPeriod * 2f) < blinkPeriod;
            Color c = labelSelected;
            c.a = ease * (on ? 1f : 0.25f);
            caret.color = c;
            caretRt.anchoredPosition = new Vector2(-caretGap - caretSlide * (1f - ease), 0f);
        }
    }

    public void OnPointerEnter(PointerEventData e)
    {
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(gameObject);
    }

    public void OnSelect(BaseEventData e) { selected = true; }
    public void OnDeselect(BaseEventData e) { selected = false; }
    public void OnSubmit(BaseEventData e) { press = 1f; }
    public void OnPointerClick(PointerEventData e) { press = 1f; }

    static Color Clear(Color c) { c.a = 0f; return c; }
}
