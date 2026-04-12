using UnityEngine;
using DG.Tweening;

public class FlaskController : MonoBehaviour
{
    public FlaskColor flaskColor;
    [SerializeField] private Material[] colorMaterials;

    public void Init(FlaskColor color)
    {
        // Freeze physics on spawn
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;

        flaskColor = color;
        int index = (int)color;
        Renderer[] allRenderers = GetComponentsInChildren<Renderer>(true);
        foreach (Renderer r in allRenderers)
        {
            if (index < colorMaterials.Length && colorMaterials[index] != null)
            {
                Material[] mats = new Material[r.materials.Length];
                for (int i = 0; i < mats.Length; i++)
                    mats[i] = colorMaterials[index];
                r.materials = mats;
            }
        }
    }

    public void MoveToSlot(Transform slot, float duration, System.Action onComplete = null)
    {
        transform.DOKill();

        // Add Y offset to sit ON conveyor surface
        Vector3 targetPos = slot.position + new Vector3(0, 0.3f, 0);

        transform.DOJump(targetPos, 1.5f, 1, duration)
            .SetEase(Ease.OutQuad)
            .OnComplete(() => onComplete?.Invoke());

        transform.DORotate(
            new Vector3(360, 0, 0), duration, RotateMode.FastBeyond360
        ).SetEase(Ease.Linear);
    }

    public void MoveToBox(Transform box, float duration, System.Action onComplete = null)
    {
        transform.DOKill();

        //Rigidbody rb = GetComponent<Rigidbody>();
        //if (rb != null) rb.isKinematic = true; // stay kinematic always

        transform.DOJump(
            box.position + Vector3.up * 0.2f, 1f, 1, duration)
            .SetEase(Ease.InQuad)
            .OnComplete(() => onComplete?.Invoke());

        transform.DORotate(
            new Vector3(-360, 0, 0), duration, RotateMode.FastBeyond360
        ).SetEase(Ease.Linear);
    }
}