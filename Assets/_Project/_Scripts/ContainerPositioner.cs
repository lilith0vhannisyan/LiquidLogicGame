using UnityEngine;

public class ContainerPositioner : MonoBehaviour
{
    [SerializeField] private bool isBottom = true; // true=bottom, false=top
    [SerializeField] private float edgePadding = 1f; // distance from screen edge

    private void Start()
    {
        PositionToScreenEdge();
    }

    private void PositionToScreenEdge()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        float camHeight = cam.orthographicSize;     // half screen height in units
        float camWidth = camHeight * cam.aspect;    // half screen width in units

        float zPos;
        if (isBottom)
            zPos = -camHeight + edgePadding;  // bottom edge + padding
        else
            zPos = camHeight - edgePadding;   // top edge - padding

        transform.position = new Vector3(
            0,                          // center X
            transform.position.y,       // keep Y (height off ground)
            zPos                        // auto Z based on screen
        );

        Debug.Log($"{gameObject.name} positioned at Z={zPos:F1}");
    }
}