using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Menú CodeBreak → Probar el jefe: abre el Stage 2, le da Play y deja al jugador
/// en la puerta de la arena con todo el libro escrito.
///
/// Mueve las cosas recién al entrar en Play, así que la escena no cambia: al
/// salir de Play, el jugador sigue en el principio del mapa.
/// </summary>
[InitializeOnLoad]
public static class BossTestStart
{
    const string ScenePath = "Assets/Scenes/Stage2.unity";
    const string BookPath = "Assets/Data/Libro/LibroDeKira.asset";

    // Entrar en Play recarga los scripts y borraría un campo común.
    const string PendingKey = "CodeBreak.BossTestStart.Pending";

    // Cuánto antes de donde arranca la pelea aparece el jugador.
    const float BeforeStart = 1.5f;

    static BossTestStart()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    [MenuItem("CodeBreak/Probar el jefe (libro completo)")]
    static void Run()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        if (EditorSceneManager.GetActiveScene().path != ScenePath)
            EditorSceneManager.OpenScene(ScenePath);

        SessionState.SetBool(PendingKey, true);
        EditorApplication.EnterPlaymode();
    }

    [MenuItem("CodeBreak/Probar el jefe (libro completo)", true)]
    static bool CanRun()
    {
        return !EditorApplication.isPlayingOrWillChangePlaymode;
    }

    static void OnPlayModeChanged(PlayModeStateChange change)
    {
        if (change != PlayModeStateChange.EnteredPlayMode) return;
        if (!SessionState.GetBool(PendingKey, false)) return;
        SessionState.EraseBool(PendingKey);

        KiraBookContent book = AssetDatabase.LoadAssetAtPath<KiraBookContent>(BookPath);
        if (book != null) KiraBookProgress.UnlockQuietly(book.chapters.Select(c => c.id));

        BossController boss = Object.FindFirstObjectByType<BossController>();
        PlayerMovement player = Object.FindFirstObjectByType<PlayerMovement>();
        if (boss == null || player == null || boss.respawnPoint == null)
        {
            Debug.LogWarning("Probar el jefe: falta el jefe, el jugador o su punto de reaparición en la escena.");
            return;
        }

        Vector3 spot = new Vector3(boss.startX - BeforeStart, boss.respawnPoint.position.y, player.transform.position.z);
        player.transform.position = spot;

        // El arma se agarra en el garage, que acá queda salteado.
        player.EquipWeapon();

        // La cámara y Kira van siguiendo con suavidad: sin esto, tardarían
        // varios segundos en cruzar el mapa entero.
        Camera cam = Camera.main;
        if (cam != null) cam.transform.position = new Vector3(spot.x, spot.y, cam.transform.position.z);

        KiraFollower kira = Object.FindFirstObjectByType<KiraFollower>();
        if (kira != null) kira.transform.position = spot + new Vector3(-0.8f, 0.8f, 0f);
    }
}
