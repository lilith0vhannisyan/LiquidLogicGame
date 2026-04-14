using UnityEngine;
using System.Collections.Generic;

public class GridLayout3D : MonoBehaviour
{
    [Header("Grid Settings")]
    [SerializeField] private float boxSize = 1.5f;
    [SerializeField] private float gap = 0.3f;
    [SerializeField] private bool growUpward = false;

    private int maxPerRow = 3;
    private int totalItems = 0;
    private List<BoxController> spawnedBoxes = new List<BoxController>();

    // Track screen size to detect changes
    private int lastScreenWidth = 0;
    private int lastScreenHeight = 0;

    private void Update()
    {
        // Detect screen size change
        if (Screen.width != lastScreenWidth ||
            Screen.height != lastScreenHeight)
        {
            lastScreenHeight = Screen.height;
            lastScreenHeight = lastScreenHeight;
            RecalculateAndReposition();
        }
    }

    public void RecalculateMaxPerRow()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        float camWidth = cam.orthographicSize * cam.aspect * 2f;
        float cellSize = boxSize + gap;
        maxPerRow = Mathf.Max(2, Mathf.FloorToInt(camWidth / cellSize));
        Debug.Log($"Grid maxPerRow={maxPerRow} camWidth={camWidth:F1}");
    }

    public void SetTotalItems(int count)
    {
        totalItems = count;
        spawnedBoxes.Clear();
    }

    // Register box after spawning
    public void RegisterBox(BoxController box)
    {
        spawnedBoxes.Add(box);
    }

    // Recalculate positions and move all boxes
    public void RecalculateAndReposition()
    {
        RecalculateMaxPerRow();

        for (int i = 0; i < spawnedBoxes.Count; i++)
        {
            if (spawnedBoxes[i] == null) continue;
            Vector3 newPos = GetSlotPosition(i);
            spawnedBoxes[i].transform.position = newPos;
        }

        Debug.Log($"Repositioned {spawnedBoxes.Count} boxes");
    }

    public Vector3 GetSlotPosition(int index)
    {
        Camera cam = Camera.main;
        if (cam == null) return transform.position;

        float camWidth = cam.orthographicSize * cam.aspect * 2f;
        float cellSize = boxSize + gap;

        int row = index / maxPerRow;
        int col = index % maxPerRow;

        // Count items in this row for centering
        int startOfRow = row * maxPerRow;
        int itemsInRow = Mathf.Min(maxPerRow, totalItems - startOfRow);

        // Center row within camera width
        float rowWidth = itemsInRow * cellSize - gap;
        float startX = -rowWidth / 2f + cellSize / 2f;

        float x = startX + col * cellSize;
        float z = transform.position.z +
            (growUpward ? row * cellSize : -row * cellSize);

        // Clamp X to stay within screen
        float maxX = cam.orthographicSize * cam.aspect - boxSize / 2f;
        x = Mathf.Clamp(x, -maxX, maxX);

        return new Vector3(
            transform.position.x + x,
            transform.position.y,
            z
        );
    }

    private int nextSlotIndex = 0;

    public void Reset()
    {
        nextSlotIndex = 0;
        spawnedBoxes.Clear();
    }

    public void UnregisterBox(BoxController box)
    {
        spawnedBoxes.Remove(box);
    }

    public Vector3 GetNextSlotPosition()
    {
        Vector3 pos = GetSlotPosition(nextSlotIndex);
        nextSlotIndex++;
        return pos;
    }
}