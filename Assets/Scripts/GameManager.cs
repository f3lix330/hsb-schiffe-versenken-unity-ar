using UnityEngine;
using GameLogic.control;
using GameLogic.model;
using GameLogic.model.ability;

public class GameManager : MonoBehaviour
{
    
    public static GameManager Instance { get; private set; }
    private GameController _gameController;
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
        _gameController = new GameController();
    }
    
    public void InitGameOnServer()
    {
        _gameController = new GameController();
        _gameController.EnableFullPlacementForShooting();
    }

    public int RegisterPlayer(string playerName)
    {
        int id = _gameController.RegisterPlayer(playerName);
        if (_gameController.GetPlayers().Count == 2) _gameController.InitGame();
        return id;
    }

    public int Shoot(int playerId, int x, int y)
    {
        return _gameController.Shoot(playerId, x, y);
    }

    public int UseAbility(string cardName, int playerId, int x, int y)
    {
        SpecialAbility ability = cardName switch
        {
            "Korallen" => new CoralReef(),
            "Tornado" => new Waterspout(),
            "Radar"   => new Radar(),
            _         => new CoralReef()
        };

       return _gameController.UseSpecialAbility(playerId, x, y, ability);
    }

    public bool PlaceShip(int playerId, int length, int x, int y, Orientation orientation)
    {
        return _gameController.PlaceShip(playerId, length, x, y, orientation);
    }

    public int GetCurrentPlayer()
    {
        return _gameController.GetCurrentPlayer();
    }
    
    public bool IsGameStarted()
    {
        return _gameController.IsGameStarted();
    }
}
