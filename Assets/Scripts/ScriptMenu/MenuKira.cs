using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class MenuKira : MonoBehaviour
{
    [Header("Animación")]
    public Sprite[] idleFrames;

    public Sprite[] moveFrames;
    public float frameTime = 0.09f;

    public float moveThreshold = 60f;

    [Header("Paseo (unidades del canvas)")]
    public Vector2 margin = new Vector2(140f, 110f);

    public float centerHalfWidth = 430f;
    [Range(0f, 1f)] public float centerChance = 0.15f;
    public Vector2 pauseRange = new Vector2(1.5f, 4f);

    [Header("Movimiento")]
    public float smoothTime = 0.85f;
    public float maxSpeed = 700f;
    public float bankAngle = 14f;
    public float bankAtSpeed = 450f;
    public float driftAmount = 9f;

    [Header("Mirar los botones")]
    public float visitTime = 2.2f;

    public float visitGap = 110f;

    [Header("Entrada")]
    public float enterDelay = 1.1f;

    private enum State { Waiting, Moving, Scanning, Leaving }

    private RectTransform rt;
    private RectTransform area;
    private Image image;

    private State state = State.Waiting;
    private Vector2 core;
    private Vector2 velocity;
    private Vector2 target;
    private float timer;
    private float driftTimer;
    private float frameTimer;
    private int frame;
    private float facing = 1f;
    private float bank;
    private GameObject lastSelected;
    private bool visiting;

    void Awake()
    {
        rt = (RectTransform)transform;
        area = (RectTransform)rt.parent;
        image = GetComponent<Image>();
        image.raycastTarget = false;
    }

    void Start()
    {
        Vector2 half = area.rect.size * 0.5f;
        core = new Vector2(-half.x - 200f, half.y * 0.35f);
        rt.anchoredPosition = core;
        timer = enterDelay;
        driftTimer = Random.value * 10f;

        if (EventSystem.current != null)
            lastSelected = EventSystem.current.currentSelectedGameObject;
    }

    void Update()
    {
        float dt = Time.unscaledDeltaTime;
        if (dt <= 0f) return;

        WatchSelection();
        Think(dt);
        Move(dt);
        Animate(dt);
    }

    void Think(float dt)
    {
        switch (state)
        {
            case State.Waiting:
                timer -= dt;
                if (timer <= 0f) GoTo(new Vector2(-centerHalfWidth - 150f, area.rect.height * 0.12f));
                break;

            case State.Moving:
                if (Vector2.Distance(core, target) < 12f && velocity.magnitude < 40f)
                {
                    state = State.Scanning;
                    timer = visiting ? visitTime : Random.Range(pauseRange.x, pauseRange.y);
                }
                break;

            case State.Scanning:
                timer -= dt;
                if (timer <= 0f)
                {
                    visiting = false;
                    GoTo(RandomSpot());
                }
                break;
        }
    }

    void WatchSelection()
    {
        if (state == State.Leaving || EventSystem.current == null) return;

        GameObject sel = EventSystem.current.currentSelectedGameObject;

        if (state == State.Waiting) { lastSelected = sel; return; }
        if (sel == lastSelected) return;
        lastSelected = sel;
        if (sel == null) return;

        RectTransform button = sel.transform as RectTransform;
        if (button == null) return;

        Vector3[] corners = new Vector3[4];
        button.GetWorldCorners(corners);
        Vector2 right = area.InverseTransformPoint((corners[2] + corners[3]) * 0.5f);
        visiting = true;
        GoTo(right + new Vector2(visitGap, 6f));
    }

    void GoTo(Vector2 p)
    {
        target = p;
        state = State.Moving;
    }

    Vector2 RandomSpot()
    {
        Vector2 half = area.rect.size * 0.5f - margin;
        float y = Random.Range(-half.y * 0.2f, half.y);

        float x;
        if (Random.value < centerChance)
            x = Random.Range(-centerHalfWidth, centerHalfWidth);
        else
        {
            float side = Random.value < 0.7f ? -Mathf.Sign(core.x) : Mathf.Sign(core.x);
            if (side == 0f) side = 1f;
            x = side * Random.Range(centerHalfWidth + 40f, Mathf.Max(centerHalfWidth + 41f, half.x));
        }
        return new Vector2(x, y);
    }

    public void FlyAway()
    {
        Vector2 half = area.rect.size * 0.5f;
        target = new Vector2(half.x + 300f, core.y + 220f);
        state = State.Leaving;
    }

    void Move(float dt)
    {
        driftTimer += dt;

        float st = state == State.Leaving ? 0.45f : smoothTime;
        float max = state == State.Leaving ? maxSpeed * 2.5f : maxSpeed;
        if (state != State.Waiting)
            core = Vector2.SmoothDamp(core, target, ref velocity, st, max, dt);

        float still = 1f - Mathf.Clamp01(velocity.magnitude / maxSpeed);
        Vector2 drift = new Vector2(
            Mathf.Sin(driftTimer * 0.83f) * driftAmount * 0.7f,
            Mathf.Sin(driftTimer * 1.17f + 1.3f) * driftAmount) * (0.4f + 0.6f * still);

        rt.anchoredPosition = core + drift;

        if (Mathf.Abs(velocity.x) > 25f) facing = Mathf.Sign(velocity.x);

        if (state == State.Scanning && visiting) facing = -1f;

        float wanted = -Mathf.Clamp(velocity.x / bankAtSpeed, -1f, 1f) * bankAngle;
        bank = Mathf.Lerp(bank, wanted, 1f - Mathf.Exp(-6f * dt));

        Vector3 s = rt.localScale;
        s.x = Mathf.Abs(s.x) * facing;
        rt.localScale = s;
        rt.localRotation = Quaternion.Euler(0f, 0f, facing < 0f ? -bank : bank);
    }

    void Animate(float dt)
    {
        Sprite[] frames = velocity.magnitude > moveThreshold && moveFrames != null && moveFrames.Length > 0
            ? moveFrames : idleFrames;
        if (frames == null || frames.Length == 0) return;

        frameTimer += dt;
        if (frameTimer >= frameTime)
        {
            frameTimer -= frameTime;
            frame++;
        }
        image.sprite = frames[frame % frames.Length];
    }
}
