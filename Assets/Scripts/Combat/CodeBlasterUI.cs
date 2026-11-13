using System.Collections;
using TMPro;
using UnityEngine;

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

    public bool IsTyping { get { return typing != null; } }

    void Awake()
    {
        Hide();
    }

    public void ShowQuestion(string question)
    {
        if (root != null) root.SetActive(true);
        if (questionText != null) questionText.text = question;
        HideFeedback();
    }

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
