using PurrNet.Packing;
using UnityEngine;
using UnityEngine.UI;

public class UIShipGenerator : MonoBehaviour
{
    public GameObject shipPrefab20;
    public GameObject shipPrefab30;
    public GameObject shipPrefab40;
    public GameObject shipPrefab50;
    private Button _confirmShips;

    private GameObject[] _placedShips = new GameObject[5];
    public NetworkCommunicationHandler NetworkCommunication;
    
    void Start()
    {
        GenerateAvailableShips();
        _confirmShips = GameObject.Find("ConfirmShipsButton").GetComponent<Button>();
        _confirmShips.onClick.AddListener(ConfirmShipPlacement);
    }

    private void GenerateAvailableShips()
    {
        var shipObject20 = Instantiate(shipPrefab20, transform);
        shipObject20.transform.localPosition = new Vector3(0, 0, 0);
        _placedShips[0] = shipObject20;
        
        var shipObject30 = Instantiate(shipPrefab30, transform);
        shipObject30.transform.localPosition = new Vector3(0.1f, 0, 0);
        _placedShips[1] = shipObject30;
        
        var shipObject302 = Instantiate(shipPrefab30, transform);
        shipObject302.transform.localPosition = new Vector3(0.2f, 0, 0);
        _placedShips[2] = shipObject302;
        
        var shipObject40 = Instantiate(shipPrefab40, transform);
        shipObject40.transform.localPosition = new Vector3(0.3f, 0, 0);
        _placedShips[3] = shipObject40;
        
        var shipObject50 = Instantiate(shipPrefab50, transform);
        shipObject50.transform.localPosition = new Vector3(0.4f, 0, 0);
        _placedShips[4] = shipObject50;
    }
    
    public async void ConfirmShipPlacement()
    {
        int shipsPlaced = 0;
        for (var i = 0; i < _placedShips.Length; i++)
        {
            GameObject ship = _placedShips[i];
            if (ship != null)
            {
                (int x, int y) = CanvasManager.GetTileFromShipTransform(ship.transform, ship.GetComponent<UIShip>()._orientation);
                using var writer = BitPackerPool.Get();
                Packer<int>.Write(writer, Config.PlayerId);
                Packer<int>.Write(writer, ship.GetComponent<UIShip>()._length);
                Packer<int>.Write(writer, x);
                Packer<int>.Write(writer, y);
                Packer<int>.Write(writer, (int) ship.GetComponent<UIShip>()._orientation);
                if (await NetworkCommunication.HandlePlaceShip(writer))
                {
                    shipsPlaced++;
                    _placedShips[i] = null;
                    ship.GetComponent<UIShip>().isMovable = false;
                }
            }
        }
    }
}
