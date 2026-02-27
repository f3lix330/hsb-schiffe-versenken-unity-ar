using DefaultNamespace;
using GameLogic.model;
using UnityEngine;

public class CanvasManager : MonoBehaviour
{
    private GameObject _ownGrid;
    private GameObject _opponentGrid;
    private NetworkCommunicationHandler _networkCommunication;
    private GameObject _shipGenerator;
    public CloudAnchorInterface _cloudAnchorInterface;
    void Start()
    {
        _networkCommunication =  GameObject.Find("NetworkCommunicationHandler").GetComponent<NetworkCommunicationHandler>();
        Transform gridOwn = transform.Find("GridOwn");
        Transform gridEnemy = transform.Find("GridEnemy");
        Transform shipGenerator = transform.Find("ShipSelector");
        shipGenerator.GetComponent<UIShipGenerator>().NetworkCommunication = _networkCommunication;
        gridOwn.GetComponent<UIGridGenerator>().NetworkCommunication = _networkCommunication;
        gridEnemy.GetComponent<UIGridGenerator>().NetworkCommunication = _networkCommunication;
        
        if (gridOwn != null) {
            _ownGrid = gridOwn.gameObject;
        }

        if (gridEnemy != null)
        {
            _opponentGrid = gridEnemy.gameObject;
        }
        
        if (shipGenerator != null)
        {
            _shipGenerator = shipGenerator.gameObject;
        }
    }

    public void HandleEvent(int x, int y, ActionResult result, int playerId)
    {
        if (result == ActionResult.AbilityUsed)
        {
            _opponentGrid.GetComponent<GridManager>().HandleEvent(x, y, result);
            _ownGrid.GetComponent<GridManager>().HandleEvent(x, y, result);
            return;
        }
        if (playerId == Config.PlayerId)
        {
            _opponentGrid.GetComponent<GridManager>().HandleEvent(x, y, result);
        }
        else
        {
            _ownGrid.GetComponent<GridManager>().HandleEvent(x, y, result);
        }
    }

    public static (int, int) GetTileFromShipTransform(Transform transform, Orientation orientation)
    {
        var x = transform.localPosition.x;
        var y = transform.localPosition.z;
        
        Debug.Log("Transform coordinates: X: " + x + ", Y: " + y);
        
        const float cellSize = 0.15f;
        const float halfCell = cellSize / 2.0f;

        if (orientation == Orientation.Vertical)
        {
            x -= halfCell;
        }
        else if (orientation == Orientation.Horizontal)
        {
            y -= halfCell;
        }
        return GetTileXAndY(x, y, cellSize);
    }

    public static (int, int) GetTileXAndY(float x, float y, float cellSize)
    {
        const int gridSize = 10;
        float halfGrid = gridSize * cellSize / 2.0f;

        int tileX = Mathf.FloorToInt((x + halfGrid) / cellSize);
        int tileY = Mathf.FloorToInt((y + halfGrid) / cellSize);

        tileX = Mathf.Clamp(tileX, 0, gridSize - 1);
        tileY = Mathf.Clamp(tileY, 0, gridSize - 1);
        
        Debug.Log($"Got tile {tileX} {tileY}");
        return (tileX, tileY);
    }

    public void ToggleActivePlayer(int playerId)
    {
        if (playerId == Config.PlayerId)
        {
            _opponentGrid.SetActive(true);
            _ownGrid.SetActive(false);
            _shipGenerator.SetActive(false);
        }
        else
        {
            _opponentGrid.SetActive(false);
            _ownGrid.SetActive(true);
            _shipGenerator.SetActive(true);
        }
    }
    
    void Update()
    {
        if (CurrentState.State != States.Playing)
        {
            return;
        }
        
        bool isTouch = Input.touchCount > 0;
        bool isMouse = Input.GetMouseButtonUp(0);

        if (isTouch || isMouse)
        {
            Vector3 interactionPosition;
            bool isEnded = false;

            if (isTouch)
            {
                Touch touch = Input.GetTouch(0);
                interactionPosition = touch.position;
                isEnded = (touch.phase == TouchPhase.Ended);
            }
            else
            {
                interactionPosition = Input.mousePosition;
                isEnded = true;
            }
            
            if (isEnded)
            {
                Ray ray = Camera.main.ScreenPointToRay(interactionPosition);
                RaycastHit hit;

                if (Physics.Raycast(ray, out hit))
                {
                    if (hit.collider.CompareTag("EnemyTile"))
                    {
                        hit.collider.gameObject.GetComponent<UICellEnemy>().OnTouch();
                    }
                }
            }
        }
    }
}
