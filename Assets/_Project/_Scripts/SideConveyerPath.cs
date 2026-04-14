using DG.Tweening;
using System.Collections;
using UnityEngine;

public class SideConveyorPath : MonoBehaviour
{
    [SerializeField] private Transform[] pathPoints;

    private void Start()
    {
        AutoPositionPoints();
    }

    private void AutoPositionPoints()
    {
        Camera cam = Camera.main;
        if (cam == null || pathPoints == null || pathPoints.Length < 3) return;

        float camHeight = cam.orthographicSize;

        // Point_0 at bottom, Point_1 at middle, Point_2 at top
        pathPoints[0].position = new Vector3(
            pathPoints[0].position.x,
            pathPoints[0].position.y,
            -camHeight + 1.5f);

        pathPoints[1].position = new Vector3(
            pathPoints[1].position.x,
            pathPoints[1].position.y,
            0);

        pathPoints[2].position = new Vector3(
            pathPoints[2].position.x,
            pathPoints[2].position.y,
            camHeight - 1.5f);
    }

    // Animate box smoothly along path points upward
    public IEnumerator MoveBoxAlongPath(Transform box, float duration,
    System.Action onComplete = null)
    {
        if (pathPoints == null || pathPoints.Length == 0)
        {
            onComplete?.Invoke();
            yield break;
        }

        float timePerSegment = duration / pathPoints.Length;

        for (int i = 0; i < pathPoints.Length; i++)
        {
            yield return box.DOMove(pathPoints[i].position, timePerSegment)
                .SetEase(Ease.InOutQuad)
                .WaitForCompletion();
        }

        onComplete?.Invoke();
    }

    // Get top end position
    public Vector3 TopPosition =>
        pathPoints != null && pathPoints.Length > 0
        ? pathPoints[pathPoints.Length - 1].position
        : transform.position;

    // Get bottom start position  
    public Vector3 BottomPosition =>
        pathPoints != null && pathPoints.Length > 0
        ? pathPoints[0].position
        : transform.position;
}