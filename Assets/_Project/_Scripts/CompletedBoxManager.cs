using UnityEngine;
using System.Collections.Generic;

public class CompletedBoxManager : MonoBehaviour
{
    public static CompletedBoxManager Instance;

    [SerializeField] private GridLayout3D topGrid;

    private int completedCount = 0;

    private void Awake() => Instance = this;

    public void Setup(int totalBoxes)
    {
        completedCount = 0;
        topGrid.SetTotalItems(totalBoxes);
        topGrid.Reset();
    }

    // Returns world position where completed box should go
    public Vector3 GetNextCompletedSlot()
    {
        return topGrid.GetNextSlotPosition();
    }
}