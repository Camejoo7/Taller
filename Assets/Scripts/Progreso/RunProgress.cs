using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Maneja el progreso de la partida (libro y diálogos) entre escenas:
/// - Al volver al menú principal, todo arranca de cero.
/// - Al entrar a un stage, se saca una foto de cómo estaba todo.
/// - El botón Reiniciar de la pausa vuelve a esa foto.
/// Morir recarga la misma escena y no toca nada: se conserva lo ganado.
/// </summary>
public static class RunProgress
{
    static string lastScene;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Init()
    {
        lastScene = null;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode != LoadSceneMode.Single) return;

        if (scene.name == "MainMenu")
        {
            KiraBookProgress.ResetAll();
            PlayerHealth.ClearCarry();
        }
        else if (scene.name != lastScene)
        {
            // Se entró a un stage nuevo (no es una recarga por morir o reiniciar).
            KiraBookProgress.Snapshot();
            DialogueProgress.Snapshot();
        }

        lastScene = scene.name;
    }

    /// <summary>Lo llama la pausa antes de recargar la escena.</summary>
    public static void RestartStage()
    {
        KiraBookProgress.RestoreSnapshot();
        DialogueProgress.RestoreSnapshot();
    }
}
