using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class MenuButtonFX : MonoBehaviour,
    IPointerEnterHandler, ISelectHandler, IDeselectHandler,
    ISubmitHandler, IPointerClickHandler
{
    public TMP_Text label;

    [Header("Seleccionado")]
    public float selectedScale = 1.08f;
    public Color labelNormal = new Color(0.755f, 0.755f, 0.755f, 1f);

    public Color labelSelected = new Color(0.55f, 1f, 0.8f, 1f);

    [Header("Cursor")]
    public string caretText = ">";

    public float caretSize = 1.5f;

    public float caretGap = 14f;

    public float caretSlide = 18f;
    public float blinkPeriod = 0.55f;

    [Header("Apretar")]
    public float pressSquash = 0.1f;

    private RectTransform rt;
    private Vector3 baseScale;
    private TMP_Text caret;
    private RectTransform caretRt;
    private bool selected;
    private float amount;
    private float press;

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

        caretRt.anchorMin = caretRt.anchorMax = new Vector2(0f, 0.5f);
        caretRt.pivot = new Vector2(1f, 0.5f);
        caretRt.sizeDelta = new Vector2(40f, 40f);

        caret = go.AddComponent<TextMeshProUGUI>();
        caret.text = caretText;
        caret.font = label.font;

        caret.fontSize = label.fontSize * caretSize;
        caret.alignment = TextAlignmentOptions.Right;
        caret.textWrappingMode = TextWrappingModes.NoWrap;
        caret.raycastTarget = false;
        caret.color = Clear(labelSelected);

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
