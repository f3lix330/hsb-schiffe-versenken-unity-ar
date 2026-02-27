using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UIStartGame : MonoBehaviour
{
    private enum GameType
    {
        Host, Client
    }
    [Header("Input Fields")]
    [SerializeField] private TMP_InputField ipInputField;
    [SerializeField] private TMP_InputField portInputField;
    private GameType _gameType;
    
    public static string IP { get; private set; }
    public static ushort Port { get; private set; }
    
    public void SetHost()
    {
        _gameType = GameType.Host;
    }
    
    public void SetClient()
    {
        _gameType = GameType.Client;
    }

    public void Play()
    {
        IP = ipInputField.text;
        Port = ushort.Parse(portInputField.text);
        switch (_gameType)
        {
            case GameType.Host:
                Config.Port = Port;
                Config.IsHost = true;
                break;
            case GameType.Client:
                Config.IP = IP;
                Config.Port = Port;
                Config.IsHost = false;
                break;
            default:
                Debug.Log("Game Type Not Set");
                break;
        }
        LoadNextScene();
    }

    private static void LoadNextScene()
    {
        var nextSceneIndex = SceneManager.GetActiveScene().buildIndex + 1;
        SceneManager.LoadScene(nextSceneIndex);
    }

}
