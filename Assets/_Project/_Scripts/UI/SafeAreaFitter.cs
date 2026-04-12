using UnityEngine;

public class SafeAreaFitter : MonoBehaviour
{
    private RectTransform rect;
    private Rect lastSafeArea;

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
        Apply();
    }

    private void Update()
    {
        if (Screen.safeArea != lastSafeArea)
            Apply();
    }

    private void Apply()
    {
        lastSafeArea = Screen.safeArea;
        var anchorMin = Screen.safeArea.position / new Vector2(Screen.width, Screen.height);
        var anchorMax = (Screen.safeArea.position + Screen.safeArea.size) / new Vector2(Screen.width, Screen.height);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
    }
}