using System;
using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Un fragmento de codigo flotante: una de las opciones de la pregunta.
/// A proposito no sabe nada de la pelea ni de si el jugador acerto — solo
/// muestra su texto y avisa cuando le pegan. Quien decide que significa ese
/// impacto es CodeBlasterFight. Asi el mismo fragmento sirve para cualquier
/// pregunta de cualquier stage.
/// </summary>
public class CodeBlasterTarget : MonoBehaviour
{
    [Header("Partes")]
    public TextMeshPro label;
    public SpriteRenderer background;

    [Header("Colores")]
    public Color normalColor = new Color(0.078f, 0.110f, 0.180f, 0.95f);
    public Color correctColor = new Color(0.20f, 0.85f, 0.45f, 0.95f);
    public Color wrongColor = new Color(0.85f, 0.22f, 0.25f, 0.95f);

    /// <summary>Indice de esta opcion dentro de QuizQuestion.options.</summary>
    public int OptionIndex { get; private set; }

    /// <summary>Si este fragmento es la respuesta correcta.</summary>
    public bool IsCorrect { get; private set; }

    /// <summary>Le avisa a CodeBlasterFight que una bala le pego.</summary>
    public event Action<CodeBlasterTarget> OnHit;

    private Coroutine flashRoutine;

    /// <summary>Carga los datos de la opcion. Lo llama CodeBlasterFight al crearlo.</summary>
    public void Setup(int optionIndex, string text, bool isCorrect)
    {
        OptionIndex = optionIndex;
        IsCorrect = isCorrect;

        if (label != null)
            label.text = text;

        if (background != null)
            background.color = normalColor;
    }

    /// <summary>La llama Projectile al impactar.</summary>
    public void Hit()
    {
        if (OnHit != null)
            OnHit(this);
    }

    /// <summary>Parpadeo rojo: le pegaste a este y no era.</summary>
    public void FlashWrong(float duration)
    {
        StartFlash(wrongColor, duration);
    }

    /// <summary>Se pinta de verde y queda asi: era la respuesta correcta.</summary>
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

        // Parpadea entre el color de aviso y el normal, en vez de quedarse
        // pintado: llama mas la atencion sin tapar el texto del fragmento.
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
