using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConveyorManager : MonoBehaviour
{
    public static ConveyorManager Instance;

    [SerializeField] private Transform[] slots;
    [SerializeField] private GameObject flaskPrefab;
    //[SerializeField] private float moveSpeed = 0.3f;

    private FlaskController[] slotContents;
    private bool isDelivering = false;
    private bool isHandlingStuck = false;

    public bool HasFreeSlots => GetFreeSlotCount() > 0;
    public bool IsDelivering => isDelivering;
    public int GetTotalSlotCount() => slotContents.Length;
    public bool IsMoving { get; private set; }

    private void Awake()
    {
        Instance = this;
        // Start with all slots — will be resized by InitConveyor()
        slotContents = new FlaskController[slots.Length];
    }

    public void RequestFlasksFromBox(BoxController box)
    {
        // Wait if currently delivering
        if (isDelivering)
        {
            Debug.Log("Conveyor busy delivering — wait!");
            box.transform.DOShakePosition(0.3f, 0.1f, 10, 90);
            return;
        }

        int freeSlots = GetFreeSlotCount();
        if (freeSlots == 0)
        {
            Debug.Log("Conveyor full!");
            box.transform.DOShakePosition(0.3f, 0.1f, 10, 90);
            return;
        }

        List<FlaskColor> colors = box.TakeFlasks(freeSlots);
        if (colors.Count == 0)
        {
            Debug.Log("Nothing to send!");
            return;
        }

        StartCoroutine(SpawnFlasksOntoConveyor(colors, box.transform));
    }

    public bool HasStuckFlasks()
    {
        // Flasks are stuck if conveyor has flasks 
        // but none can be delivered to open boxes
        for (int i = 0; i < slotContents.Length; i++)
        {
            if (slotContents[i] == null) continue;
            BoxController receiver = FindReceiver(slotContents[i].flaskColor);
            if (receiver == null) return true; // this flask is stuck
        }
        return false;
    }

    public void InitConveyor(int capacity)
    {
        // Clear any existing flasks
        if (slotContents != null)
            foreach (var s in slotContents)
                if (s != null) Destroy(s.gameObject);

        // Use only first N slots based on capacity
        int useSlots = Mathf.Min(capacity, slots.Length);
        slotContents = new FlaskController[useSlots];

        Debug.Log($"Conveyor initialized with {useSlots} slots");
    }

    public void TryDeliverWaiting()
    {
        if (GetOccupiedSlotCount() > 0 && !isDelivering)
        {
            isHandlingStuck = false; // reset so stuck detection works again
            StartCoroutine(RotateAndDeliver());
        }
    }

    private IEnumerator SpawnFlasksOntoConveyor(List<FlaskColor> colors, Transform fromBox)
    {
        isDelivering = true;

        for (int i = 0; i < colors.Count; i++)
        {
            int slot = GetNextFreeSlot();
            if (slot == -1) break;

            GameObject flaskObj = Instantiate(
                flaskPrefab, fromBox.position, Quaternion.identity);
            FlaskController flask = flaskObj.GetComponent<FlaskController>();
            flask.Init(colors[i]);
            slotContents[slot] = flask;

            // Declare variables FIRST
            float delay = i * 0.08f; // Change to sit on slot faster
            int capturedSlot = slot;
            FlaskController capturedFlask = flask;

            // ONE DelayedCall only
            DOVirtual.DelayedCall(delay, () =>
            {
                capturedFlask.MoveToSlot(slots[capturedSlot], 0.3f); // Faster flight to slot
                UIManager.Instance.UpdateConveyorDisplay();
            });
        }

        yield return new WaitForSeconds(colors.Count * 0.08f + 0.4f);
        yield return StartCoroutine(RotateAndDeliver());
        isDelivering = false;
    }

    private IEnumerator RotateAndDeliver()
    {
        isDelivering = true;
        yield return new WaitForSeconds(0.3f);

        int maxRetries = 30;
        int retries = 0;

        while (GetOccupiedSlotCount() > 0 && retries < maxRetries)
        {
            retries++;
            int stuckBefore = GetOccupiedSlotCount();

            for (int i = 0; i < slotContents.Length; i++)
            {
                if (slotContents[i] == null) continue;

                BoxController receiver = FindReceiver(slotContents[i].flaskColor);

                if (receiver != null)
                {
                    FlaskController flask = slotContents[i];
                    slotContents[i] = null;

                    BoxController captured = receiver;
                    flask.MoveToBox(captured.transform, 0.3f, () =>
                    {
                        if (captured != null
                            && captured.gameObject.activeSelf
                            && captured.CanReceiveFlask(flask.flaskColor))
                        {
                            captured.ReceiveFlask(flask);
                        }
                        else
                        {
                            // Don't destroy — put back on conveyor or find new receiver
                            Debug.Log($"Flask {flask.flaskColor} lost receiver — destroying");
                            Destroy(flask.gameObject);
                        }
                    });

                    yield return new WaitForSeconds(0.3f);
                    UIManager.Instance.UpdateConveyorDisplay();
                    isHandlingStuck = false;
                }
            }

            int stuckAfter = GetOccupiedSlotCount();

            if (stuckAfter > 0 && stuckAfter == stuckBefore)
            {
                Debug.Log($"{stuckAfter} flasks stuck — waiting for player");

                if (GetFreeSlotCount() == 0)
                {
                    // Only lose if NO closed boxes left to open
                    bool hasClosedBoxes = GameManager.Instance.HasAnyClosedBox();

                    if (!hasClosedBoxes)
                    {
                        isDelivering = false;
                        isHandlingStuck = false;
                        yield return new WaitForSeconds(0.8f);
                        Debug.Log("LOSE — conveyor full, no closed boxes!");
                        UIManager.Instance.ShowGameOver();
                        yield break;
                    }
                    else
                    {
                        // Conveyor full but closed boxes exist — auto open one
                        Debug.Log("Conveyor full — opening closed box");
                        GameManager.Instance.OpenOneClosedBox();
                        break;
                    }
                }

                // Check if player has any moves
                if (!PlayerHasAnyMoves())
                {
                    Debug.Log("No moves available — auto opening box");
                    GameManager.Instance.OpenOneClosedBox();
                }

                break;
            }

            yield return new WaitForSeconds(0.1f);
        }

        if (!PlayerHasAnyMoves())
        {
            Debug.Log("Player has no moves — auto opening one closed box");
            GameManager.Instance.OpenOneClosedBox();
        }

        isDelivering = false;
        isHandlingStuck = false;
    }

    private bool PlayerHasAnyMoves()
    {
        var openBoxes = GameManager.Instance.GetAllOpenBoxes();
        foreach (var box in openBoxes)
            if (!box.IsEmpty()) return true;
        return false;
    }

    private BoxController FindReceiver(FlaskColor color)
    {
        var allBoxes = GameManager.Instance.GetAllOpenBoxes();

        BoxController best = null;
        int mostReceived = -1;

        foreach (var box in allBoxes)
        {
            if (!box.CanReceiveFlask(color)) continue;

            // Prefer box that already has most correct flasks
            if (box.ReceivedFlasks > mostReceived)
            {
                mostReceived = box.ReceivedFlasks;
                best = box;
            }
        }

        return best;
    }

    public FlaskColor GetMostStuckColor()
    {
        Dictionary<FlaskColor, int> colorCount = new Dictionary<FlaskColor, int>();

        foreach (var s in slotContents)
        {
            if (s == null) continue;
            if (!colorCount.ContainsKey(s.flaskColor))
                colorCount[s.flaskColor] = 0;
            colorCount[s.flaskColor]++;
        }

        FlaskColor mostColor = FlaskColor.Blue;
        int mostCount = 0;
        foreach (var kvp in colorCount)
            if (kvp.Value > mostCount)
            {
                mostCount = kvp.Value;
                mostColor = kvp.Key;
            }

        return mostColor;
    }

    private int GetOccupiedSlotCount()
    {
        int count = 0;
        foreach (var s in slotContents)
            if (s != null) count++;
        return count;
    }

    public int GetFreeSlotCount()
    {
        int count = 0;
        foreach (var s in slotContents)
            if (s == null) count++;
        return count;
    }

    private int GetNextFreeSlot()
    {
        for (int i = 0; i < slotContents.Length; i++)
            if (slotContents[i] == null) return i;
        return -1;
    }
}