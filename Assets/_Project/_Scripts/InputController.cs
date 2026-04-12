using UnityEngine;

public class InputController : MonoBehaviour
{
    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Debug.Log("Click detected!");
            HandleClick(Input.mousePosition);
        }
        else if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            Debug.Log("Touch detected!");
            HandleClick(Input.GetTouch(0).position);
        }
    }

    private void HandleClick(Vector2 screenPosition)
    {
        Ray ray = Camera.main.ScreenPointToRay(screenPosition);
        RaycastHit hit;

        Debug.Log($"Raycasting from {screenPosition}");

        if (Physics.Raycast(ray, out hit))
        {
            Debug.Log($"Hit: {hit.collider.gameObject.name}");
            BoxController box = hit.collider.GetComponentInParent<BoxController>();
            if (box != null)
            {
                Debug.Log($"Box found: {box.boxColor}");
                box.OnBoxClicked();
            }
            else
            {
                Debug.Log("No BoxController on hit object!");
            }
        }
        else
        {
            Debug.Log("Raycast hit nothing!");
        }
    }
}