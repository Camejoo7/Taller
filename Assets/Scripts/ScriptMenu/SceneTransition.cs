using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneTransition : MonoBehaviour
{
    public Image panelFade;
    public float duracion = 1f;

    void Start()
    {
        StartCoroutine(FadeIn());
    }

    public void CargarEscena(string nombreEscena)
    {
        StartCoroutine(FadeOut(nombreEscena));
    }

    IEnumerator FadeIn()
    {
        float t = 1f;
        while (t > 0f)
        {
            t -= Time.deltaTime / duracion;
            panelFade.color = new Color(0, 0, 0, t);
            yield return null;
        }
        panelFade.color = new Color(0, 0, 0, 0);
    }

    IEnumerator FadeOut(string nombreEscena)
    {
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / duracion;
            panelFade.color = new Color(0, 0, 0, t);
            yield return null;
        }
        SceneManager.LoadScene(nombreEscena);
    }
}