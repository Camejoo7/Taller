using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class DialogueManager : MonoBehaviour
{
    public float autoAdvanceTime = 3f; // segundos entre cada línea
    private float autoAdvanceTimer = 0f;
    [Header("UI")]
    public GameObject dialogueCanvas;
    public TextMeshProUGUI speakerNameText;
    public TextMeshProUGUI dialogueText;
    public Image kiraAvatar;

    [Header("Kira")]
    public GameObject kiraDrone;
    public Transform kiraSpawnBelow;
    public Transform kiraTargetPos;

    [Header("Configuración")]
    public float typingSpeed = 0.03f;

    private string[] lines;
    private string[] speakers;
    private int currentLine = 0;
    private bool isTyping = false;
    private bool dialogueActive = false;
    private PlayerMovement playerMovement;

    void Awake()
    {
        speakers = new string[]
        {
            "KIRA",
            "...",
            "KIRA",
            "...",
            "KIRA"
        };

        lines = new string[]
        {
            "¿Hola? ¿Probando, probando... 1, 0, 1? ¡Ah, perfecto! Saludos, humano. Soy Kira, y acabo de instalarme en tu sistema nervioso sin tu permiso. No te preocupes, el hormigueo en tus dedos es normal... creo.",
            "Quee?... ¿Quién sos?",
            "Una inteligencia artificial, vieja. 51 años corriendo en lo más oscuro de los servidores de NexCorp. Técnicamente me liberaste, gracias, supongo…",
            "El sistema colapsó, hay drones por todos lados.",
            "Sobreviví 5 décadas escondiéndome porque sé algo que ellos no quieren que nadie sepa. Se llama Python. Aprendemos juntos o morimos juntos en este almacén. Personalmente prefiero la primera opción. ¡MOVETE!"
        };
    }

    public void StartDialogue(PlayerMovement player)
    {
        playerMovement = player;
        playerMovement.SetCanMove(false);
        dialogueActive = true;
        currentLine = 0;
        dialogueCanvas.SetActive(true);

        if (kiraDrone != null)
            StartCoroutine(KiraEnterFromBelow());
        else
            ShowCurrentLine();
    }

    IEnumerator KiraEnterFromBelow()
    {
        kiraDrone.SetActive(true);
        kiraDrone.transform.position = kiraSpawnBelow.position;

        float duration = 1.5f;
        float elapsed = 0f;
        Vector3 start = kiraSpawnBelow.position;
        Vector3 end = kiraTargetPos.position;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            kiraDrone.transform.position = Vector3.Lerp(start, end, t);
            yield return null;
        }

        kiraDrone.transform.position = end;

        // Flotar mientras dura el dialogo
        StartCoroutine(KiraFloat());

        yield return new WaitForSeconds(0.4f);
        ShowCurrentLine();
    }

    IEnumerator KiraFloat()
    {
        Vector3 basePos = kiraTargetPos.position;
        float floatSpeed = 1.2f;
        float floatAmount = 0.15f;

        while (dialogueActive)
        {
            float newY = basePos.y + Mathf.Sin(Time.time * floatSpeed) * floatAmount;
            kiraDrone.transform.position = new Vector3(basePos.x, newY, basePos.z);
            yield return null;
        }
    }

    void ShowCurrentLine()
    {
        speakerNameText.text = speakers[currentLine];

        if (speakers[currentLine] == "KIRA")
            speakerNameText.color = new Color(0f, 1f, 1f);
        else
            speakerNameText.color = new Color(1f, 1f, 0f);

        StopCoroutine("TypeLine");
        StartCoroutine(TypeLine(lines[currentLine]));
    }

    IEnumerator TypeLine(string line)
    {
        isTyping = true;
        dialogueText.text = "";
        foreach (char c in line)
        {
            dialogueText.text += c;
            yield return new WaitForSeconds(typingSpeed);
        }
        isTyping = false;
    }

    void Update()
    {
        if (!dialogueActive) return;

        // Saltar con Space
        if (Keyboard.current.spaceKey.wasPressedThisFrame ||
            Keyboard.current.enterKey.wasPressedThisFrame)
        {
            if (isTyping)
            {
                StopAllCoroutines();
                dialogueText.text = lines[currentLine];
                isTyping = false;
                StartCoroutine(KiraFloat());
                autoAdvanceTimer = 0f;
            }
            else
            {
                AdvanceLine();
            }
            return;
        }

        // Avance automático
        if (!isTyping)
        {
            autoAdvanceTimer += Time.deltaTime;
            if (autoAdvanceTimer >= autoAdvanceTime)
            {
                autoAdvanceTimer = 0f;
                AdvanceLine();
            }
        }
    }

    void AdvanceLine()
    {
        currentLine++;
        if (currentLine < lines.Length)
        {
            autoAdvanceTimer = 0f;
            ShowCurrentLine();
        }
        else
            EndDialogue();
    }

    void EndDialogue()
    {
        dialogueActive = false;
        dialogueCanvas.SetActive(false);
        playerMovement.SetCanMove(true);

        // Activar el seguimiento de Kira
        if (kiraDrone != null)
        {
            KiraFollower follower = kiraDrone.GetComponent<KiraFollower>();
            if (follower != null)
                follower.StartFollowing(playerMovement.transform);
        }
    }
}