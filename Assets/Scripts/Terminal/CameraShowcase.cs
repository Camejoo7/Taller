using System.Collections;
using UnityEngine;

public class CameraShowcase : MonoBehaviour
{
    public Transform focus;

    public float zoom = 2.6f;

    [Range(0f, 1f)] public float weight = 1f;

    public float duration = 3.5f;

    private bool shown;

    public void Show()
    {
        if (shown) return;
        shown = true;
        StartCoroutine(Run());
    }

    IEnumerator Run()
    {
        Camera cam = Camera.main;
        CameraFollow follow = cam != null ? cam.GetComponent<CameraFollow>() : null;
        if (follow == null) yield break;

        follow.SetSecondaryTarget(focus != null ? focus : transform, weight);
        follow.SetZoom(zoom);

        yield return new WaitForSeconds(duration);

        follow.ClearSecondaryTarget();
        follow.ResetZoom();
    }
}
