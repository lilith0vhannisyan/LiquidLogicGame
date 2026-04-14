using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;

public class BoxController : MonoBehaviour
{
    public FlaskColor boxColor;
    public int capacity = 4;
    public bool isHidden = false;
    public bool isOpen = false;

    [SerializeField] private GameObject hiddenCover;
    [SerializeField] private Material[] colorMaterials;
    [SerializeField] private Transform flaskSpawnParent;
    [SerializeField] private GameObject flaskPrefab;

    [Header("Flask Grid Settings")]
    [SerializeField] private float flaskGridSize = 0.15f;
    [SerializeField] private float flaskYOffset = 0.1f;

    private List<GameObject> visualFlasks = new List<GameObject>();
    private List<FlaskColor> pendingFlaskColors = new List<FlaskColor>();
    private int receivedFlasks = 0;
    private bool[] positionOccupied;
    private Material[] instancedMaterials;
    private Color originalColor;

    public int ReceivedFlasks => receivedFlasks;

    //private void Update()
    //{
    //    if (Input.GetMouseButtonDown(0))
    //        HandleClick(Input.mousePosition);

    //    if (Input.touchCount > 0)
    //    {
    //        Touch touch = Input.GetTouch(0);
    //        if (touch.phase == TouchPhase.Began)
    //            HandleClick(touch.position);
    //    }
    //}

    public void Init(BoxConfig config)
    {
        boxColor = config.boxColor;
        capacity = config.flaskCount;
        isHidden = config.isHidden;
        pendingFlaskColors = new List<FlaskColor>(config.flasksInside);
        receivedFlasks = 0;

        positionOccupied = new bool[capacity];
        for (int i = 0; i < capacity; i++)
            positionOccupied[i] = false;

        // Apply color with instanced materials
        int index = (int)boxColor;
        if (index < colorMaterials.Length && colorMaterials[index] != null)
        {
            originalColor = colorMaterials[index].color;

            foreach (Renderer r in GetComponentsInChildren<Renderer>(true))
            {
                // Skip flask spawn parent children
                if (flaskSpawnParent != null &&
                    r.transform.IsChildOf(flaskSpawnParent)) continue;

                Material[] mats = new Material[r.materials.Length];
                for (int j = 0; j < mats.Length; j++)
                    mats[j] = new Material(colorMaterials[index]);
                r.materials = mats;
            }
        }

        SpawnVisualFlasks();
        SetOpen(false);
    }

    private int GetFreePositionIndex()
    {
        for (int i = 0; i < positionOccupied.Length; i++)
            if (!positionOccupied[i]) return i;
        return -1;
    }

    private List<Vector3> GetGridPositions()
    {
        Vector3 boxWorldScale = transform.lossyScale;
        float interiorX = boxWorldScale.x * flaskGridSize;
        float interiorZ = boxWorldScale.z * flaskGridSize;
        float topY = transform.position.y + boxWorldScale.y * flaskYOffset;

        return new List<Vector3>
        {
            new Vector3(transform.position.x - interiorX, topY, transform.position.z - interiorZ),
            new Vector3(transform.position.x + interiorX, topY, transform.position.z - interiorZ),
            new Vector3(transform.position.x - interiorX, topY, transform.position.z + interiorZ),
            new Vector3(transform.position.x + interiorX, topY, transform.position.z + interiorZ),
        };
    }

    private void SpawnVisualFlasks()
    {
        if (flaskPrefab == null || flaskSpawnParent == null) return;

        foreach (var f in visualFlasks)
            if (f != null) Destroy(f);
        visualFlasks.Clear();

        for (int i = 0; i < positionOccupied.Length; i++)
            positionOccupied[i] = false;

        List<Vector3> positions = GetGridPositions();
        int posIdx = 0;

        // Show received correct flasks
        for (int i = 0; i < receivedFlasks && posIdx < positions.Count; i++)
        {
            GameObject flaskObj = Instantiate(
                flaskPrefab, positions[posIdx], Quaternion.identity);
            flaskObj.transform.SetParent(flaskSpawnParent);
            FlaskController fc = flaskObj.GetComponent<FlaskController>();
            if (fc != null) fc.Init(boxColor);
            visualFlasks.Add(flaskObj);
            positionOccupied[posIdx] = true;
            posIdx++;
        }

        // Show pending wrong flasks
        for (int i = 0; i < pendingFlaskColors.Count && posIdx < positions.Count; i++)
        {
            GameObject flaskObj = Instantiate(
                flaskPrefab, positions[posIdx], Quaternion.identity);
            flaskObj.transform.SetParent(flaskSpawnParent);
            FlaskController fc = flaskObj.GetComponent<FlaskController>();
            if (fc != null) fc.Init(pendingFlaskColors[i]);
            visualFlasks.Add(flaskObj);
            positionOccupied[posIdx] = true;
            posIdx++;
        }
    }

    private void ClearVisualFlasks()
    {
        foreach (var f in visualFlasks)
            if (f != null) Destroy(f);
        visualFlasks.Clear();
    }

    //public void SetOpen(bool open)
    //{
    //    isOpen = open;
    //    if (hiddenCover != null)
    //        hiddenCover.SetActive(!open);
    //    Debug.Log($"Box {boxColor} isOpen={open} hiddenCover={hiddenCover != null}");
    //}
    // Just temperoy, when will be the closed box
    public void SetOpen(bool open)
    {
        isOpen = open;

        // Only get THIS box's own renderers — not flask children
        // Use the root GameObject's renderer only
        Renderer[] boxRenderers = GetComponents<Renderer>(); // root only

        // Also get BoxOpenVisuals child renderers but NOT flask spawn parent children
        List<Renderer> renderers = new List<Renderer>();
        foreach (Renderer r in GetComponentsInChildren<Renderer>(true))
        {
            // Skip flask spawn parent and its children
            if (flaskSpawnParent != null &&
                r.transform.IsChildOf(flaskSpawnParent)) continue;
            renderers.Add(r);
        }

        if (open)
        {
            // Restore original color on box only
            foreach (Renderer r in renderers)
                foreach (Material m in r.materials)
                    m.color = originalColor;

            // Show flasks
            foreach (var f in visualFlasks)
                if (f != null) f.SetActive(true);

            // Bounce animation
            transform.DOKill();
            transform.DOPunchScale(Vector3.one * 0.2f, 0.4f, 5, 0.5f);
        }
        else
        {
            // Dark tint on box only — not flasks
            foreach (Renderer r in renderers)
                foreach (Material m in r.materials)
                    m.color = new Color(0.25f, 0.25f, 0.25f, 1f);

            // Hide flasks
            foreach (var f in visualFlasks)
                if (f != null) f.SetActive(false);
        }
    }

    //private void HandleClick(Vector2 screenPosition)
    //{
    //    Ray ray = Camera.main.ScreenPointToRay(screenPosition);
    //    RaycastHit hit;

    //    if (Physics.Raycast(ray, out hit))
    //    {
    //        if (hit.collider.gameObject == gameObject
    //            || hit.collider.transform.IsChildOf(transform))
    //        {
    //            OnBoxClicked();
    //        }
    //    }
    //}

    public void OnBoxClicked()
    {
        if (!isOpen) return;

        if (ConveyorManager.Instance.IsDelivering)
        {
            transform.DOKill();
            transform.DOShakePosition(0.3f, 0.1f, 10, 90);
            return;
        }

        int freeSlots = ConveyorManager.Instance.GetFreeSlotCount();

        if (ConveyorManager.Instance.HasStuckFlasks()
            && pendingFlaskColors.Count == 0)
        {
            GameManager.Instance.OpenOneClosedBox();
            return;
        }

        if (pendingFlaskColors.Count == 0) return;

        if (freeSlots == 0)
        {
            transform.DOKill();
            transform.DOShakePosition(0.3f, 0.1f, 10, 90);
            return;
        }

        ConveyorManager.Instance.RequestFlasksFromBox(this);
    }

    public List<FlaskColor> TakeFlasks(int maxCount)
    {
        List<FlaskColor> toKeep = new List<FlaskColor>();
        List<FlaskColor> toSend = new List<FlaskColor>();

        foreach (FlaskColor flask in pendingFlaskColors)
        {
            if (flask == boxColor)
                toKeep.Add(flask);
            else
                toSend.Add(flask);
        }

        // Count correct flasks
        receivedFlasks += toKeep.Count;
        for (int i = 0; i < toKeep.Count; i++)
            UIManager.Instance.OnFlaskDelivered();

        // Check instant completion
        if (receivedFlasks >= capacity)
        {
            pendingFlaskColors.Clear();
            ClearVisualFlasks();
            Debug.Log($"Box {boxColor} complete instantly!");
            GameManager.Instance.OnBoxCompleted(this);
            gameObject.SetActive(false);
            return new List<FlaskColor>();
        }

        // Send wrong flasks up to maxCount
        List<FlaskColor> canSend = new List<FlaskColor>();
        List<FlaskColor> cantSend = new List<FlaskColor>();
        int slotsUsed = 0;

        foreach (FlaskColor flask in toSend)
        {
            if (slotsUsed < maxCount)
            {
                canSend.Add(flask);
                slotsUsed++;
            }
            else
            {
                cantSend.Add(flask);
            }
        }

        pendingFlaskColors = cantSend;
        SpawnVisualFlasks();

        return canSend;
    }

    public bool CanReceiveFlask(FlaskColor color)
    {
        if (!isOpen) return false;
        if (color != boxColor) return false;
        if (IsFull()) return false;
        return GetFreePositionIndex() != -1;
    }

    public void UndoTakeFlasks(List<FlaskColor> colors)
    {
        pendingFlaskColors.AddRange(colors);
        SpawnVisualFlasks();
    }

    public void ReceiveFlask(FlaskController flask)
    {
        if (flask.flaskColor != boxColor)
        {
            Debug.Log($"Wrong color! Box={boxColor} Flask={flask.flaskColor}");
            return;
        }

        int freePos = GetFreePositionIndex();
        if (freePos == -1)
        {
            Debug.Log($"Box {boxColor} has no free position!");
            return;
        }

        positionOccupied[freePos] = true;
        receivedFlasks++;

        // Freeze physics
        Rigidbody rb = flask.GetComponent<Rigidbody>();
        if (rb != null)
            rb.isKinematic = true;

        // Place at free position
        List<Vector3> positions = GetGridPositions();
        flask.transform.SetParent(flaskSpawnParent);
        flask.transform.position = positions[freePos];
        flask.transform.rotation = Quaternion.identity;
        visualFlasks.Add(flask.gameObject);

        UIManager.Instance.OnFlaskDelivered();

        Debug.Log($"Box {boxColor}: {receivedFlasks}/{capacity}");

        if (receivedFlasks >= capacity)
        {
            Debug.Log($"Box {boxColor} complete!");
            GameManager.Instance.OnBoxCompleted(this);
            //gameObject.SetActive(false);
        }
    }

    public bool IsEmpty() => pendingFlaskColors.Count == 0;
    public bool IsFull() => receivedFlasks >= capacity;
    public int FlasksNeeded() => capacity - receivedFlasks;
}