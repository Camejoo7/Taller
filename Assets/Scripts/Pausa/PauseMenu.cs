using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class PauseMenu : MonoBehaviour
{
    public GameObject pausaCanvas;
    private bool isPaused = false;

    void Start()
    {
        pausaCanvas.SetActive(false);
    }

    void Update()
    {
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (isPaused)
                Reanudar();
            else
                Pausar();
        }
    }

    public void Pausar()
    {
        pausaCanvas.SetActive(true);
        Time.timeScale = 0f;
        isPaused = true;
    }

    public void Reanudar()
    {
        pausaCanvas.SetActive(false);
        Time.timeScale = 1f;
        isPaused = false;
    }

    public void Reiniciar()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void IrAlMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }
}
