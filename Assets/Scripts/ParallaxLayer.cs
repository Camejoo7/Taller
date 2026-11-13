using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
[DefaultExecutionOrder(100)]
public class ParallaxLayer : MonoBehaviour
{
    public Vector2 factor = new Vector2(0.9f, 0.95f);

    public Vector2 origin;

    public bool fitToView = false;

    public float fitMargin = 1.15f;

    static readonly List<ParallaxLayer> all = new List<ParallaxLayer>();

    void OnEnable() { if (!all.Contains(this)) all.Add(this); }
    void OnDisable() { all.Remove(this); }

    void LateUpdate()
    {
        Camera cam = Camera.main;
        if (cam == null) return;
        Apply(cam.transform.position, cam.orthographicSize, cam.aspect);
    }

    public void Apply(Vector3 camPos, float orthoSize, float aspect)
    {
        Vector3 p = new Vector3(origin.x + camPos.x * factor.x,
                                origin.y + camPos.y * factor.y,
                                transform.position.z);
        transform.position = p;

        if (fitToView)
        {
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr != null && sr.sprite != null)
            {
                Vector2 size = sr.sprite.bounds.size;
                float h = orthoSize * 2f * fitMargin;
                float w = h * aspect;
                transform.localScale = new Vector3(w / size.x, h / size.y, 1f);
            }
        }
    }

    public static void ApplyAll(Vector3 camPos, float orthoSize, float aspect)
    {
        foreach (ParallaxLayer l in all)
            if (l != null) l.Apply(camPos, orthoSize, aspect);
        ZoneBackdrop.ApplyAll(camPos.x);
    }
}
