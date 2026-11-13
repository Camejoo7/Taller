using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public float smoothSpeed = 5f;

    public float zoomSpeed = 2.5f;

    private Camera cam;
    private float baseSize;
    private float targetSize;
    private Transform secondaryTarget;
    private float secondaryWeight;

    void Awake()
    {
        cam = GetComponent<Camera>();
        if (cam != null)
        {
            baseSize = cam.orthographicSize;
            targetSize = baseSize;
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 focus = target.position;

        if (secondaryTarget != null)
            focus = Vector3.Lerp(target.position, secondaryTarget.position, secondaryWeight);

        Vector3 targetPos = new Vector3(focus.x, focus.y, transform.position.z);
        transform.position = Vector3.Lerp(transform.position, targetPos, smoothSpeed * Time.deltaTime);

        if (cam != null && Mathf.Abs(cam.orthographicSize - targetSize) > 0.001f)
            cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetSize, zoomSpeed * Time.deltaTime);
    }

    public void SetZoom(float size)
    {
        targetSize = size > 0f ? size : baseSize;
    }

    public void ResetZoom()
    {
        targetSize = baseSize;
    }

    public void SetSecondaryTarget(Transform other, float weight)
    {
        secondaryTarget = other;
        secondaryWeight = Mathf.Clamp01(weight);
    }

    public void ClearSecondaryTarget()
    {
        secondaryTarget = null;
        secondaryWeight = 0f;
    }
}
