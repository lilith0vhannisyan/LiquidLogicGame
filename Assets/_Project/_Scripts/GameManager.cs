using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [SerializeField] private LevelData[] levels;
    [SerializeField] private GameObject boxPrefab;
    [SerializeField] private Transform leftSpawnPoint;
    [SerializeField] private Transform rightSpawnPoint;
    [SerializeField] private float boxSpacing = 1.5f; // for fitting on screen

    private List<BoxController> leftBoxes = new List<BoxController>();
    private List<BoxController> rightBoxes = new List<BoxController>();
    private int currentLevel = 0;
    private int completedBoxes = 0;
    private int totalBoxes = 0;
    private int openBoxesPerSide = 1;
    private LevelData currentLevelData;

    public int CurrentLevel => currentLevel;
    public int TotalLevels => levels.Length;

    private void Awake()
    {
        Instance = this;
    }

    private void Start() => Debug.Log("GameManager Start called!");

    private FlaskColor[] allColors = new FlaskColor[]
    {
        FlaskColor.Blue, FlaskColor.Pink,
        FlaskColor.Yellow, FlaskColor.Red, FlaskColor.Purple
    };

    public void LoadLevel(int index)
    {
        //PositionContainers();

        Debug.Log($"Loading level {index}");
        if (index >= levels.Length) { Debug.Log("You win!"); return; }

        currentLevelData = levels[index];
        completedBoxes = 0;
        openBoxesPerSide = currentLevelData.openBoxesPerSide;

        // Initialize conveyor with correct capacity ← ADD THIS
        ConveyorManager.Instance.InitConveyor(currentLevelData.conveyorCapacity);
        //PositionContainers();
        int totalBoxCount = currentLevelData.boxCountPerSide * 2;
        FlaskColor[] picked = PickRandomColors(
            Mathf.Min(totalBoxCount, currentLevelData.colorsCount));

        foreach (var b in leftBoxes) if (b != null) Destroy(b.gameObject);
        foreach (var b in rightBoxes) if (b != null) Destroy(b.gameObject);
        leftBoxes.Clear();
        rightBoxes.Clear();

        List<BoxConfig> allConfigs = GenerateAllBoxConfigs(currentLevelData, picked);

        List<BoxConfig> leftConfigs = new List<BoxConfig>();
        List<BoxConfig> rightConfigs = new List<BoxConfig>();

        for (int i = 0; i < allConfigs.Count; i++)
        {
            if (i % 2 == 0) leftConfigs.Add(allConfigs[i]);
            else rightConfigs.Add(allConfigs[i]);
        }

        totalBoxes = allConfigs.Count;
        int totalFlasks = totalBoxes * currentLevelData.flasksPerBox;

        SpawnBoxes(leftConfigs, leftSpawnPoint, leftBoxes);
        SpawnBoxes(rightConfigs, rightSpawnPoint, rightBoxes);

        OpenNextBoxes(leftBoxes);
        OpenNextBoxes(rightBoxes);

        if (UIManager.Instance != null)
            UIManager.Instance.SetupLevel(index, totalFlasks);
    }

    ////Responsivness : spawn relative to camera width
    //private void PositionContainers()
    //{
    //    float camWidth = Camera.main.orthographicSize * Camera.main.aspect;

    //    // Keep boxes at fixed percentage of screen width
    //    float xPos = Mathf.Min(camWidth * 0.7f, 5f); // max 5 units from center

    //    leftSpawnPoint.position = new Vector3(
    //        -xPos,
    //        leftSpawnPoint.position.y,
    //        leftSpawnPoint.position.z
    //    );
    //    rightSpawnPoint.position = new Vector3(
    //        xPos,
    //        rightSpawnPoint.position.y,
    //        rightSpawnPoint.position.z
    //    );
    //}

    private List<BoxConfig> GenerateAllBoxConfigs(LevelData level, FlaskColor[] colors)
    {
        int totalBoxCount = level.boxCountPerSide * 2;
        int flasksPerBox = level.flasksPerBox;

        // Assign one color per box
        List<FlaskColor> boxColors = new List<FlaskColor>();
        for (int i = 0; i < totalBoxCount; i++)
            boxColors.Add(colors[i % colors.Length]);

        // Create flask pool — exactly right amount per color
        List<FlaskColor> flaskPool = new List<FlaskColor>();
        foreach (FlaskColor color in colors)
        {
            int boxCount = 0;
            foreach (FlaskColor bc in boxColors)
                if (bc == color) boxCount++;

            int totalFlasksForColor = boxCount * flasksPerBox;
            for (int i = 0; i < totalFlasksForColor; i++)
                flaskPool.Add(color);
        }

        // Keep reshuffling until no box starts pre-completed
        List<BoxConfig> allConfigs = new List<BoxConfig>();
        int maxAttempts = 100;

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            // Shuffle pool
            for (int i = flaskPool.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                FlaskColor temp = flaskPool[i];
                flaskPool[i] = flaskPool[j];
                flaskPool[j] = temp;
            }

            allConfigs.Clear();
            bool anyBoxInvalid = false;

            for (int i = 0; i < totalBoxCount; i++)
            {
                List<FlaskColor> boxFlasks = flaskPool.GetRange(
                    i * flasksPerBox, flasksPerBox);

                FlaskColor thisBoxColor = boxColors[i];

                // Check — box must have at least 1 wrong flask
                bool hasWrongFlask = false;
                foreach (var f in boxFlasks)
                    if (f != thisBoxColor) { hasWrongFlask = true; break; }

                if (!hasWrongFlask)
                {
                    anyBoxInvalid = true;
                    break;
                }

                allConfigs.Add(new BoxConfig
                {
                    boxColor = thisBoxColor,
                    flaskCount = flasksPerBox,
                    isHidden = level.levelNumber >= 5,
                    flasksInside = boxFlasks
                });
            }

            // Good distribution — no box pre-complete
            if (!anyBoxInvalid) break;
        }

        return allConfigs;
    }

    private void SpawnBoxes(List<BoxConfig> configs, Transform spawnPoint,
    List<BoxController> list)
    {
        list.Clear();
        for (int i = 0; i < configs.Count; i++)
        {
            Vector3 pos = spawnPoint.position + new Vector3(0, 0.6f, i * boxSpacing);
            GameObject obj = Instantiate(boxPrefab, pos, Quaternion.identity);
            BoxController box = obj.GetComponent<BoxController>();
            if (box == null) { Debug.LogError("BoxController missing!"); return; }
            box.Init(configs[i]); // SetOpen(false) called inside Init now
            list.Add(box);
        }
    }


    private void OpenNextBoxes(List<BoxController> boxes)
    {
        int alreadyOpen = 0;
        foreach (var box in boxes)
            if (box != null && box.isOpen && box.gameObject.activeSelf)
                alreadyOpen++;

        int toOpen = openBoxesPerSide - alreadyOpen;

        Debug.Log($"OpenNextBoxes: alreadyOpen={alreadyOpen} toOpen={toOpen} limit={openBoxesPerSide}");

        if (toOpen <= 0) return;

        int opened = 0;
        foreach (var box in boxes)
        {
            if (opened >= toOpen) break;
            if (box != null && !box.isOpen && !box.IsFull() && box.gameObject.activeSelf)
            {
                box.SetOpen(true);
                opened++;
                Debug.Log($"Opened: {box.boxColor}");
            }
        }
    }

    // Called by ConveyorManager when flasks can't find their box
    public void OnFlasksStuck()
    {
        StartCoroutine(HandleStuckFlasks());
    }

    private IEnumerator HandleStuckFlasks()
    {
        yield return new WaitForSeconds(0.5f);

        // Step 1 — find most needed color from conveyor
        FlaskColor neededColor = ConveyorManager.Instance.GetMostStuckColor();
        Debug.Log($"Most needed color: {neededColor}");

        // Step 2 — try open ONE box of that color
        bool opened = TryOpenClosedBoxOfColor(neededColor);

        // Step 3 — if no matching box, open any ONE closed box
        if (!opened)
        {
            opened = TryOpenRandomClosedBox(leftBoxes);
            if (!opened)
                opened = TryOpenRandomClosedBox(rightBoxes);
        }

        if (opened)
        {
            // Wait then try deliver — if still stuck, GameManager
            // will be called again from RotateAndDeliver
            yield return new WaitForSeconds(0.5f);
            ConveyorManager.Instance.TryDeliverWaiting();
        }
        else
        {
            // No closed boxes anywhere = check lose
            StartCoroutine(CheckLoseCondition());
        }
    }

    // Private — takes a list parameter
    private bool TryOpenRandomClosedBox(List<BoxController> boxes)
    {
        List<BoxController> closed = new List<BoxController>();
        foreach (var box in boxes)
            if (box != null && box.gameObject.activeSelf
                && !box.isOpen && !box.IsFull())
                closed.Add(box);

        if (closed.Count == 0) return false;

        BoxController random = closed[Random.Range(0, closed.Count)];
        random.SetOpen(true);
        Debug.Log($"Auto-opened box: {random.boxColor}");
        return true;
    }
    private bool TryOpenClosedBoxOfColor(FlaskColor color)
    {
        // Try left first
        foreach (var box in leftBoxes)
            if (box != null && !box.isOpen && !box.IsFull()
                && box.gameObject.activeSelf && box.boxColor == color)
            {
                box.SetOpen(true);
                Debug.Log($"Opened matching box: {color}");
                return true;
            }

        // Try right
        foreach (var box in rightBoxes)
            if (box != null && !box.isOpen && !box.IsFull()
                && box.gameObject.activeSelf && box.boxColor == color)
            {
                box.SetOpen(true);
                Debug.Log($"Opened matching box: {color}");
                return true;
            }

        return false;
    }

    public void OpenOneClosedBox()
    {
        // Find most needed color from stuck flasks
        FlaskColor neededColor = ConveyorManager.Instance.GetMostStuckColor();

        // Try open matching color first
        bool opened = TryOpenClosedBoxOfColor(neededColor);

        // Fallback — open any closed box
        if (!opened)
            opened = TryOpenRandomClosedBox(leftBoxes);
        if (!opened)
            opened = TryOpenRandomClosedBox(rightBoxes);

        if (opened)
        {
            // Try deliver waiting flasks to newly opened box
            ConveyorManager.Instance.TryDeliverWaiting();
        }
        else
        {
            // No closed boxes = check lose
            StartCoroutine(CheckLoseCondition());
        }
    }

    public bool HasAnyClosedBox()
    {
        foreach (var box in leftBoxes)
            if (box != null && !box.isOpen
                && !box.IsFull()
                && box.gameObject.activeSelf)
                return true;

        foreach (var box in rightBoxes)
            if (box != null && !box.isOpen
                && !box.IsFull()
                && box.gameObject.activeSelf)
                return true;

        return false;
    }

    public void OnBoxCompleted(BoxController box)
    {
        completedBoxes++;
        Debug.Log($"Box completed: {completedBoxes}/{totalBoxes}");

        // Open next box on that side
        if (leftBoxes.Contains(box))
            OpenNextBoxes(leftBoxes);
        else
            OpenNextBoxes(rightBoxes);

        // Try deliver waiting flasks
        ConveyorManager.Instance.TryDeliverWaiting();

        // Check win
        if (completedBoxes >= totalBoxes)
            StartCoroutine(WinSequence());
    }

    private IEnumerator WinSequence()
    {
        // Wait for all animations to finish
        yield return new WaitForSeconds(1f);
        Debug.Log("YOU WIN!");
        UIManager.Instance.ShowWinScreen(currentLevel);
    }

    private IEnumerator CheckLoseCondition()
    {
        // Check if any progress can still be made
        bool canProgress = false;

        // If any open box still has wrong flasks → can still click
        foreach (var b in leftBoxes)
            if (b != null && b.isOpen && b.gameObject.activeSelf && !b.IsEmpty())
                canProgress = true;

        foreach (var b in rightBoxes)
            if (b != null && b.isOpen && b.gameObject.activeSelf && !b.IsEmpty())
                canProgress = true;

        if (!canProgress && ConveyorManager.Instance.GetFreeSlotCount() == 0)
        {
            // Conveyor full + no moves possible = lose
            yield return new WaitForSeconds(1f);
            Debug.Log("GAME OVER!");
            UIManager.Instance.ShowGameOver();
        }
    }

    public void LoadNextLevel()
    {
        foreach (var b in leftBoxes) if (b != null) Destroy(b.gameObject);
        foreach (var b in rightBoxes) if (b != null) Destroy(b.gameObject);
        leftBoxes.Clear();
        rightBoxes.Clear();

        currentLevel++;
        LoadLevel(currentLevel);
    }

    private FlaskColor[] PickRandomColors(int count)
    {
        List<FlaskColor> shuffled = new List<FlaskColor>(allColors);
        for (int i = shuffled.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            FlaskColor temp = shuffled[i];
            shuffled[i] = shuffled[j];
            shuffled[j] = temp;
        }
        return shuffled.GetRange(0, count).ToArray();
    }

    public bool HasOpenBoxForColor(FlaskColor color)
    {
        foreach (var b in leftBoxes)
            if (b != null && b.isOpen && b.gameObject.activeSelf
                && b.boxColor == color && !b.IsFull())
                return true;

        foreach (var b in rightBoxes)
            if (b != null && b.isOpen && b.gameObject.activeSelf
                && b.boxColor == color && !b.IsFull())
                return true;

        return false;
    }

    public int GetConveyorCapacity()
    {
        if (currentLevelData != null)
            return currentLevelData.conveyorCapacity;
        return 10;
    }


    public List<BoxController> GetAllOpenBoxes()
    {
        var all = new List<BoxController>();
        foreach (var b in leftBoxes)
            if (b != null && b.isOpen && b.gameObject.activeSelf) all.Add(b);
        foreach (var b in rightBoxes)
            if (b != null && b.isOpen && b.gameObject.activeSelf) all.Add(b);
        return all;
    }
}