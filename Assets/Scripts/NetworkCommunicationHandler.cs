using System.Threading.Tasks;
using DefaultNamespace;
using GameLogic.model;
using PurrNet;
using PurrNet.Packing;
using PurrNet.Transports;
using UnityEngine;

public class NetworkCommunicationHandler : NetworkBehaviour
{
    public GameManager gameManager;
    public GameObject customNetManager;

    private void Start()
    {
        var netManager = customNetManager.GetComponent<NetworkManager>();
        var udpTransport = netManager.GetComponent<UDPTransport>();
        if (Config.IsHost)
        {
            udpTransport.serverPort = Config.Port;
            netManager.StartHost();
        }
        else
        {
            udpTransport.address = Config.IP;
            udpTransport.serverPort = Config.Port;
            netManager.StartClient();
        }
    }
    
    protected async override void OnSpawned(bool asServer)
    {
        if (asServer)
        {
            gameManager.InitGameOnServer();
            return;
        }
        var bitPacker = BitPackerPool.Get();
        Packer<string>.Write(bitPacker, asServer ? "Server" : "Client");
        Config.PlayerId = await HandleRegisterPlayer(bitPacker);
    }

    [ServerRpc]
    public async Task<int> HandleRegisterPlayer(BitPacker data)
    {
        string playerName = default;
        using (data)
        {
            Packer<string>.Read(data, ref playerName);
        }

        try
        {
            var id = gameManager.RegisterPlayer(playerName);
            return id;
        }
        catch (System.Exception e)
        {
            return -1;
        }
    }

    [ServerRpc]
    public async Task<bool> HandlePlaceShip(BitPacker data)
    {
        int playerId = default;
        int length = default;
        int x = default;
        int y = default;
        int orientation = default;
        using (data)
        {
            Packer<int>.Read(data, ref playerId);
            Packer<int>.Read(data, ref length);
            Packer<int>.Read(data, ref x);
            Packer<int>.Read(data, ref y);
            Packer<int>.Read(data, ref orientation);
        }

        try
        {
            var result = gameManager.PlaceShip(playerId, length, x, y, (Orientation)orientation);
            if (gameManager.IsGameStarted())
            {
                StartGame();
                ToggleActivePlayer(gameManager.GetCurrentPlayer());
            }
            return result;
        }
        catch (System.Exception e)
        {
            Debug.Log(e.Message);
            return false;
        }
    }

    [ServerRpc]
    public async Task<string> HandleShoot(BitPacker data)
    {
        int playerId = default;
        int x = default;
        int y = default;
        using (data)
        {
            Packer<int>.Read(data, ref playerId);
            Packer<int>.Read(data, ref x);
            Packer<int>.Read(data, ref y);
        }

        try
        {
            var actionResult = gameManager.Shoot(playerId, x, y);
            if (actionResult == (int)ActionResult.HitSunkAndWon || actionResult == (int)ActionResult.AbilityFinishedGame)
            {
                FinishGame();
            }
            UpdateClient(actionResult, playerId, x, y);
            ToggleActivePlayer(gameManager.GetCurrentPlayer());
            return "success";
        }
        catch (System.Exception e)
        {
            return e.Message;
        }
    }

    [ObserversRpc]
    public void UpdateClient(int actionResult, int playerId, int x, int y)
    {
        GameObject.Find("GridCanvas").GetComponent<CanvasManager>().HandleEvent(x, y, (ActionResult) actionResult, playerId);
    }
    
    [ServerRpc]
    public void UseAbility(string cardName, int playerId, int x, int y)
    {
        int result = gameManager.UseAbility(cardName, playerId, x, y);
        UpdateClient(result, playerId, x, y);
    }

    [ServerRpc]
    public async Task<string> GetCurrentAnchorId()
    {
        return Config.currentAnchorId;
    }

    [ServerRpc]
    public Task<int> HandleGetCurrentPlayer()
    {
        return Task.FromResult(gameManager.GetCurrentPlayer());
    }

    [ObserversRpc]
    public async void ToggleActivePlayer(int player)
    {
        await Task.Delay(2000);
        GameObject.Find("GridCanvas").GetComponent<CanvasManager>().ToggleActivePlayer(player);
    }

    [ObserversRpc]
    public void StartGame()
    {
        CurrentState.State = States.Playing;
    }
    [ObserversRpc]
    public void FinishGame()
    {
        CurrentState.State = States.Finished;
    }
}