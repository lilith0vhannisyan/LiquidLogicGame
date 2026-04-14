using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [SerializeField] private LevelData[] levels;
    [SerializeField] private GameObject boxPrefab;
    [SerializeField] private Transform bottomContainer;
    [SerializeField] private SideConveyorPath leftConveyor;
    [SerializeField] private SideConveyorPath rightConveyor;
    [SerializeField] private GridLayout3D bottomGrid;

    private List<BoxController> allBoxes = new List<BoxController>();
    private int currentLevel = 0;
    private int completedBoxes = 0;
    private int totalBoxes = 0;
    private int openBoxesAtOnce = 1;
    private LevelData currentLevelData;

    public int CurrentLevel => currentLevel;
    public int TotalLevels => levels.Length;

    private void Awake() => Instance = this;
    private void Start() => Debug.Log("GameManager Start!");

    private FlaskColor[] allColors = new FlaskColor[]
    {
        FlaskColor.Blue, FlaskColor.Pink,
        FlaskColor.Yellow, FlaskColor.Red, FlaskColor.Purple
    };

    public void LoadLevel(int index)
    {
        if (index >= levels.Length) { Debug.Log("All levels done!"); return; }

        currentLevelData = levels[index];
        completedBoxes = 0;
        openBoxesAtOnce = currentLevelData.openBoxesPerSide;

        ConveyorManager.Instance.InitConveyor(currentLevelData.conveyorCapacity);

        // Destroy old boxes
        foreach (var b in allBoxes) if (b != null) Destroy(b.gameObject);
        allBoxes.Clear();

        int totalBoxCount = currentLevelData.boxCountPerSide * 2;
        totalBoxes = totalBoxCount;

        FlaskColor[] picked = PickRandomColors(
            Mathf.Min(totalBoxCount, currentLevelData.colorsCount));

        List<BoxConfig> allConfigs = GenerateAllBoxConfigs(currentLevelData, picked);
        
        // Setup grids
        bottomGrid.RecalculateMaxPerRow();
        bottomGrid.SetTotalItems(totalBoxCount);
        bottomGrid.Reset();

        CompletedBoxManager.Instance.Setup(totalBoxCount);

        // Spawn all boxes in bottom grid
        for (int i = 0; i < allConfigs.Count; i++)
        {
            Vector3 pos = bottomGrid.GetNextSlotPosition();
            GameObject obj = Instantiate(boxPrefab, pos, Quaternion.identity);
            BoxController box = obj.GetComponent<BoxController>();
            box.Init(allConfigs[i]);
            allBoxes.Add(box);
            bottomGrid.RegisterBox(box); // register for auto-reposition
        }

        // Open first N boxes
        OpenNextBoxes();

        if (UIManager.Instance != null)
            UIManager.Instance.SetupLevel(index,
                totalBoxCount * currentLevelData.flasksPerBox);
    }

    private void OpenNextBoxes()
    {
        int alreadyOpen = 0;
        foreach (var box in allBoxes)
            if (box != null && box.isOpen && box.gameObject.activeSelf)
                alreadyOpen++;

        int toOpen = openBoxesAtOnce - alreadyOpen;
        if (toOpen <= 0) return;

        int opened = 0;
        foreach (var box in allBoxes)
        {
            if (opened >= toOpen) break;
            if (box != null && !box.isOpen
                && !box.IsFull()
                && box.gameObject.activeSelf)
            {
                box.SetOpen(true);
                opened++;
            }
        }
    }

    public void OnBoxCompleted(BoxController box)
    {
        completedBoxes++;
        Debug.Log($"Completed: {completedBoxes}/{totalBoxes}");

        // Animate box to top zone via nearest side conveyor
        StartCoroutine(SendBoxToTop(box));

        // Open next closed box
        OpenNextBoxes();

        ConveyorManager.Instance.TryDeliverWaiting();

        if (completedBoxes >= totalBoxes)
            StartCoroutine(WinSequence());
    }

    private IEnumerator SendBoxToTop(BoxController box)
    {
        // Disable clicking while animating
        box.isOpen = false;

        // Find nearest side conveyor
        float distLeft = Vector3.Distance(
            box.transform.position, leftConveyor.BottomPosition);
        float distRight = Vector3.Distance(
            box.transform.position, rightConveyor.BottomPosition);

        SideConveyorPath conveyor = distLeft < distRight
            ? leftConveyor : rightConveyor;

        // Get destination in top grid
        Vector3 topSlot = CompletedBoxManager.Instance.GetNextCompletedSlot();

        // Step 1 — slide to bottom of nearest side conveyor
        yield return box.transform
            .DOMove(conveyor.BottomPosition, 0.4f)
            .SetEase(Ease.InOutQuad)
            .WaitForCompletion();

        // Step 2 — animate UP along conveyor path points
        yield return StartCoroutine(
            conveyor.MoveBoxAlongPath(box.transform, 1f));

        // Step 3 — slide into top grid slot
        yield return box.transform
            .DOMove(topSlot, 0.3f)
            .SetEase(Ease.OutBack)
            .WaitForCompletion();

        // Step 4 — now deactivate from bottom grid tracking
        // Box stays visible in top zone
        bottomGrid.UnregisterBox(box);

        Debug.Log($"Box {box.boxColor} arrived at top!");
    }

    public bool HasAnyClosedBox()
    {
        foreach (var box in allBoxes)
            if (box != null && !box.isOpen
                && !box.IsFull()
                && box.gameObject.activeSelf)
                return true;
        return false;
    }

    public void OpenOneClosedBox()
    {
        FlaskColor neededColor = ConveyorManager.Instance.GetMostStuckColor();

        // Try matching color first
        foreach (var box in allBoxes)
        {
            if (box != null && !box.isOpen && !box.IsFull()
                && box.gameObject.activeSelf && box.boxColor == neededColor)
            {
                box.SetOpen(true);
                ConveyorManager.Instance.TryDeliverWaiting();
                return;
            }
        }

        // Fallback — any closed box
        foreach (var box in allBoxes)
        {
            if (box != null && !box.isOpen
                && !box.IsFull()
                && box.gameObject.activeSelf)
            {
                box.SetOpen(true);
                ConveyorManager.Instance.TryDeliverWaiting();
                return;
            }
        }

        StartCoroutine(CheckLoseCondition());
    }

    public List<BoxController> GetAllOpenBoxes()
    {
        var list = new List<BoxController>();
        foreach (var b in allBoxes)
            if (b != null && b.isOpen && b.gameObject.activeSelf)
                list.Add(b);
        return list;
    }

    private IEnumerator WinSequence()
    {
        yield return new WaitForSeconds(1.5f);
        UIManager.Instance.ShowWinScreen(currentLevel);
    }

    private IEnumerator CheckLoseCondition()
    {
        yield return new WaitForSeconds(1f);

        bool canProgress = false;
        foreach (var b in allBoxes)
            if (b != null && b.isOpen && b.gameObject.activeSelf && !b.IsEmpty())
                canProgress = true;

        if (!canProgress && ConveyorManager.Instance.GetFreeSlotCount() == 0)
            UIManager.Instance.ShowGameOver();
    }

    public void LoadNextLevel()
    {
        foreach (var b in allBoxes) if (b != null) Destroy(b.gameObject);
        allBoxes.Clear();
        currentLevel++;
        LoadLevel(currentLevel);
    }

    private IEnumerator HandleStuckFlasks()
    {
        yield return new WaitForSeconds(0.5f);
        OpenOneClosedBox();
    }

    public void OnFlasksStuck() => StartCoroutine(HandleStuckFlasks());

    public int GetConveyorCapacity() =>
        currentLevelData != null ? currentLevelData.conveyorCapacity : 10;

    public bool HasOpenBoxForColor(FlaskColor color)
    {
        foreach (var b in allBoxes)
            if (b != null && b.isOpen && b.gameObject.activeSelf
                && b.boxColor == color && !b.IsFull())
                return true;
        return false;
    }

    private FlaskColor[] PickRandomColors(int count)
    {
        List<FlaskColor> shuffled = new List<FlaskColor>(allColors);
        for (int i = shuffled.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
        }
        return shuffled.GetRange(0, count).ToArray();
    }

    private List<BoxConfig> GenerateAllBoxConfigs(LevelData level, FlaskColor[] colors)
    {
        int totalBoxCount = level.boxCountPerSide * 2;
        int flasksPerBox = level.flasksPerBox;

        List<FlaskColor> boxColors = new List<FlaskColor>();
        for (int i = 0; i < totalBoxCount; i++)
            boxColors.Add(colors[i % colors.Length]);

        List<FlaskColor> flaskPool = new List<FlaskColor>();
        foreach (FlaskColor color in colors)
        {
            int boxCount = 0;
            foreach (FlaskColor bc in boxColors)
                if (bc == color) boxCount++;
            for (int i = 0; i < boxCount * flasksPerBox; i++)
                flaskPool.Add(color);
        }

        List<BoxConfig> allConfigs = new List<BoxConfig>();
        int maxAttempts = 100;

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            for (int i = flaskPool.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (flaskPool[i], flaskPool[j]) = (flaskPool[j], flaskPool[i]);
            }

            allConfigs.Clear();
            bool invalid = false;

            for (int i = 0; i < totalBoxCount; i++)
            {
                List<FlaskColor> boxFlasks = flaskPool.GetRange(
                    i * flasksPerBox, flasksPerBox);

                bool hasWrong = false;
                foreach (var f in boxFlasks)
                    if (f != boxColors[i]) { hasWrong = true; break; }

                if (!hasWrong) { invalid = true; break; }

                allConfigs.Add(new BoxConfig
                {
                    boxColor = boxColors[i],
                    flaskCount = flasksPerBox,
                    isHidden = level.levelNumber >= 5,
                    flasksInside = boxFlasks
                });
            }

            if (!invalid) break;
        }

        return allConfigs;
    }
}