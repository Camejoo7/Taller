using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// La UI del combate: arriba queda fija la pregunta de Kira mientras dura la
/// pelea, y abajo aparece su respuesta cuando el jugador acierta o se equivoca.
/// Son dos cosas separadas a proposito: la pregunta tiene que poder leerse todo
/// el tiempo, y el comentario es pasajero.
/// </summary>
public class CodeBlasterUI : MonoBehaviour
{
    [Header("Partes")]
    public GameObject root;
    public TextMeshProUGUI questionText;
    public GameObject feedbackPanel;
    public TextMeshProUGUI feedbackText;

    [Header("Configuración")]
    public float typingSpeed = 0.02f;

    private Coroutine typing;

    /// <summary>Si Kira todavía está escribiendo su línea.</summary>
    public bool IsTyping { get { return typing != null; } }

    void Awake()
    {
        Hide();
    }

    /// <summary>Muestra la pregunta y deja el cartel de Kira escondido.</summary>
    public void ShowQuestion(string question)
    {
        if (root != null) root.SetActive(true);
        if (questionText != null) questionText.text = question;
        HideFeedback();
    }

    /// <summary>Kira dice algo: sale con el mismo tipeo que el diálogo normal.</summary>
    public void ShowFeedback(string text)
    {
        if (feedbackPanel == null || feedbackText == null) return;

        feedbackPanel.SetActive(true);
        StopTyping();
        typing = StartCoroutine(TypeFeedback(text));
    }

    public void HideFeedback()
    {
        StopTyping();
        if (feedbackPanel != null) feedbackPanel.SetActive(false);
    }

    public void Hide()
    {
        StopTyping();
        if (feedbackPanel != null) feedbackPanel.SetActive(false);
        if (root != null) root.SetActive(false);
    }

    void StopTyping()
    {
        if (typing != null)
        {
            StopCoroutine(typing);
            typing = null;
        }
    }

    IEnumerator TypeFeedback(string text)
    {
        feedbackText.text = "";
        foreach (char c in text)
        {
            feedbackText.text += c;
            yield return new WaitForSeconds(typingSpeed);
        }
        typing = null;
    }
}
