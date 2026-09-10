using UnityEngine;

/// <summary>
/// Un encuentro Code Blaster: un dron aparece con varios fragmentos de código
/// orbitando, Kira hace una pregunta y el jugador le dispara al fragmento
/// correcto. Asset propio para poder reusarlo y probarlo aislado.
/// </summary>
[CreateAssetMenu(fileName = "CodeBlasterEncounter", menuName = "CodeBreak/Code Blaster Encounter")]
public class CodeBlasterEncounter : ScriptableObject
{
    [Tooltip("Identificador para que el spawner / trigger referencie este encuentro.")]
    public string encounterId;

    [Tooltip("La pregunta. Cada opción se convierte en un fragmento flotante.")]
    public QuizQuestion question;

    [Header("Combate")]
    [Tooltip("El dron que aparece en el encuentro.")]
    public GameObject enemyPrefab;

    [Tooltip("Daño al jugador al dispararle a un fragmento incorrecto.")]
    public int damageOnWrong = 1;

    [Tooltip("Radio al que orbitan los fragmentos alrededor del dron.")]
    public float fragmentOrbitRadius = 2f;

    [Tooltip("Velocidad de órbita de los fragmentos.")]
    public float fragmentOrbitSpeed = 45f;

    [Tooltip("0 = sin límite de tiempo. Mayor a 0 = segundos para responder.")]
    public float timeLimit = 0f;

    /// <summary>
    /// Cantidad de fragmentos a instanciar. Se deriva de las opciones de la
    /// pregunta en runtime para que no exista una segunda fuente de verdad
    /// que se pueda desincronizar.
    /// </summary>
    public int FragmentCount => question != null && question.options != null ? question.options.Length : 0;
}
