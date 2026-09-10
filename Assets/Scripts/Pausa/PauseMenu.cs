using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PauseMenu : MonoBehaviour
{
    public GameObject pausaCanvas;

    [Header("Botones del menú")]
    public Button btnReanudar;
    public Button btnReiniciar;
    public Button btnMenu;

    private bool isPaused = false;

    void Start()
    {
        pausaCanvas.SetActive(false);

        // Cableado por código para no depender de referencias de onClick en el Inspector.
        if (btnReanudar != null) btnReanudar.onClick.AddListener(Reanudar);
        if (btnReiniciar != null) btnReiniciar.onClick.AddListener(Reiniciar);
        if (btnMenu != null) btnMenu.onClick.AddListener(IrAlMenu);
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
