using System.Collections.Generic;
using DefaultNamespace;
using GameLogic.model;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class ShipDragHandler : MonoBehaviour
{
    [Header("Einstellungen")]
    public float gridSize = 0.15f;
    public string shipTag = "Ship";
    private Vector2 startInputPosition;
    private bool isDragging;
    public float dragThreshold = 10f;
 
    [Header("AR Komponenten (optional)")]
    public ARRaycastManager raycastManager;
 
    private GameObject selectedObject;
    static List<ARRaycastHit> hits = new List<ARRaycastHit>();
    private float fixedY;
 
    void Update()
    {
        if (CurrentState.State != States.Preparing)
        {
            return;
        }
        bool isMoving = false;
        bool isStarting = false;
        bool isEnding = false;
        Vector2 inputPosition = Vector2.zero;
 
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            inputPosition = touch.position;
            isStarting = (touch.phase == TouchPhase.Began);
            isMoving = (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary);
            isEnding = (touch.phase == TouchPhase.Ended);
        }
        else
        {
            inputPosition = Input.mousePosition;
            isStarting = Input.GetMouseButtonDown(0);
            isMoving = Input.GetMouseButton(0);
            isEnding = Input.GetMouseButtonUp(0);
        }
 
        if (isStarting)
        {
            Ray ray = Camera.main.ScreenPointToRay(inputPosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (hit.collider.gameObject.CompareTag(shipTag))
                {
                    selectedObject = hit.collider.gameObject;
                    fixedY = selectedObject.transform.position.y;
            
                    startInputPosition = inputPosition;
                    isDragging = false; 
                }
            }
        }

        if (selectedObject != null && !selectedObject.GetComponent<UIShip>().isMovable)
        {
            return;
        }
        
        if (isMoving && selectedObject != null)
        {
            
            if (Vector2.Distance(startInputPosition, inputPosition) > dragThreshold)
            {
                isDragging = true;
                Vector3 targetWorldPos;
                
                if (Application.isMobilePlatform && raycastManager != null)
                {
                    if (raycastManager.Raycast(inputPosition, hits, TrackableType.PlaneWithinPolygon))
                    {
                        Pose hitPose = hits[0].pose;
                        selectedObject.transform.position = new Vector3(hitPose.position.x, fixedY, hitPose.position.z);

                    }
                }
                else
                {
                    Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                    
                    Plane groundPlane = new Plane(Vector3.up, new Vector3(0, fixedY, 0));

                    if (groundPlane.Raycast(ray, out float distance))
                    {
                        Vector3 worldPos = ray.GetPoint(distance);

                        selectedObject.transform.position =
                            new Vector3(worldPos.x, fixedY, worldPos.z);
                    }
                }
            }
        }

        if (isEnding)
        {
            if (selectedObject != null)
            {
                if (!isDragging)
                {
                    Rotate();
                }
                ApplySnapping(selectedObject.transform.localPosition);
                selectedObject = null;
            }
            isDragging = false;
        }
    }
 
    void ApplySnapping(Vector3 rawPos)
    {
        float count = 10;
        float gridSize = 0.15f;
        float offset = gridSize / 2.0f;

        float range = count / 2 * gridSize;
        float gridMinX = range * -1;
        float rangeMaxX = range;
        float rangeMinZ = range * -1;
        float rangeMaxZ = range;
        
        float snappedX = Mathf.Floor(rawPos.x / gridSize) * gridSize + (selectedObject.GetComponent<UIShip>()._orientation == Orientation.Horizontal ? offset : gridSize);
        float snappedZ = Mathf.Floor(rawPos.z / gridSize) * gridSize + (selectedObject.GetComponent<UIShip>()._orientation == Orientation.Vertical ? offset : gridSize);
        
        if (snappedX > rangeMaxX || snappedX < gridMinX || snappedZ > rangeMaxZ || snappedZ < rangeMinZ)
        {
            return;
        }
 
        selectedObject.transform.localPosition = new Vector3(snappedX, selectedObject.transform.localPosition.y, snappedZ);
    }

    void Rotate()
    {
        switch (selectedObject.GetComponent<UIShip>()._orientation)
        {
            case Orientation.Horizontal:
                selectedObject.GetComponent<UIShip>()._orientation = Orientation.Vertical;
                selectedObject.transform.Rotate(0, 90, 0);
                break;
            case Orientation.Vertical:
                selectedObject.GetComponent<UIShip>()._orientation = Orientation.Horizontal;
                selectedObject.transform.Rotate(0, -90, 0);
                break;
        }
        ApplySnapping(selectedObject.transform.position);
    }
}
