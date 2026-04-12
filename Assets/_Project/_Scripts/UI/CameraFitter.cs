using UnityEngine;

public class CameraFitter : MonoBehaviour
{
    private Camera cam;

    private void Start()
    {
        cam = GetComponent<Camera>();
        FitCamera();
    }

    private void FitCamera()
    {
        float currentAspect = (float)Screen.width / Screen.height;

        // Auto calculate min/max from common phone ratios
        float minAspect = 9f / 21f;  // tallest phones (Galaxy Ultra etc)
        float maxAspect = 9f / 16f;  // widest phones (older phones)

        // Auto calculate base size from screen DPI and resolution
        float referenceHeight = 1920f;
        float scaleFactor = Screen.height / referenceHeight;
        float baseSize = 10f / scaleFactor;
        baseSize = Mathf.Clamp(baseSize, 8f, 14f); // safe range

        // Clamp aspect and calculate final size
        float clampedAspect = Mathf.Clamp(currentAspect, minAspect, maxAspect);
        cam.orthographicSize = baseSize * (maxAspect / clampedAspect);

        Debug.Log($"Screen:{Screen.width}x{Screen.height} DPI:{Screen.dpi} Size:{cam.orthographicSize:F2}");
    }
}