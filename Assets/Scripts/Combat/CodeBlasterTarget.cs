using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class CodeBlasterTarget : MonoBehaviour
{
    [Header("Partes")]
    public TextMeshPro label;
    public SpriteRenderer background;

    [Header("Colores")]
    public Color normalColor = new Color(0.078f, 0.110f, 0.180f, 0.95f);
    public Color correctColor = new Color(0.20f, 0.85f, 0.45f, 0.95f);
    public Color wrongColor = new Color(0.85f, 0.22f, 0.25f, 0.95f);

    public int OptionIndex { get; private set; }

    public bool IsCorrect { get; private set; }

    public event Action<CodeBlasterTarget> OnHit;

    private Coroutine flashRoutine;

    public void Setup(int optionIndex, string text, bool isCorrect)
    {
        OptionIndex = optionIndex;
        IsCorrect = isCorrect;

        if (label != null)
            label.text = text;

        if (background != null)
            background.color = normalColor;
    }

    public void Hit()
    {
        if (OnHit != null)
            OnHit(this);
    }

    public void FlashWrong(float duration)
    {
        StartFlash(wrongColor, duration);
    }

    public void MarkCorrect()
    {
        StopFlash();
        if (background != null)
            background.color = correctColor;
    }

    void StartFlash(Color color, float duration)
    {
        StopFlash();
        flashRoutine = StartCoroutine(FlashRoutine(color, duration));
    }

    void StopFlash()
    {
        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
            flashRoutine = null;
        }
    }

    IEnumerator FlashRoutine(Color color, float duration)
    {
        if (background == null) yield break;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            background.color = Mathf.Repeat(elapsed, 0.2f) < 0.1f ? color : normalColor;
            elapsed += Time.deltaTime;
            yield return null;
        }

        background.color = normalColor;
        flashRoutine = null;
    }
}
