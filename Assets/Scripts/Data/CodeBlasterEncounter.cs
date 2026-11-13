using UnityEngine;

[CreateAssetMenu(fileName = "CodeBlasterEncounter", menuName = "CodeBreak/Code Blaster Encounter")]
public class CodeBlasterEncounter : ScriptableObject
{
    public string encounterId;

    public int stageNumber;

    public string concept;

    public QuizQuestion question;

    [Header("Combate")]
    public GameObject enemyPrefab;

    public int damageOnWrong = 1;

    public float fragmentOrbitRadius = 1.6f;

    [Range(0.2f, 1f)]
    public float fragmentOrbitVerticalScale = 0.45f;

    public float fragmentOrbitSpeed = 45f;

    public float timeLimit = 0f;

    public int FragmentCount => question != null && question.options != null ? question.options.Length : 0;
}
