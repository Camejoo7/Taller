using UnityEngine;
using UnityEngine.SceneManagement;

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
            KiraBookProgress.Snapshot();
            DialogueProgress.Snapshot();
        }

        lastScene = scene.name;
    }

    public static void RestartStage()
    {
        KiraBookProgress.RestoreSnapshot();
        DialogueProgress.RestoreSnapshot();
    }
}
