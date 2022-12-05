using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

public class GameplayController : OneSceneMonoSingleton<GameplayController>
{
    public NetworkBulletsManager BulletsManagerPrefab;
    private NetworkBulletsManager _bulletsManager;
    public NetworkBulletsManager BulletsManager
    {
        get
        {
            if (_bulletsManager == null)
                _bulletsManager = FindObjectOfType<NetworkBulletsManager>();
            return _bulletsManager;
        }
    }

    public NetworkPlayersManager PlayersManagerPrefab;
    private NetworkPlayersManager _playersManager;
    public NetworkPlayersManager PlayersManager
    {
        get
        {
            if (_playersManager == null)
                _playersManager = FindObjectOfType<NetworkPlayersManager>();
            return _playersManager;
        }
    }

    public static bool IsGameEnded = false;
    [SerializeField] EndGamePanelController _endGamePanel;

    void Start()
    {
        NetworkManager.Singleton.OnClientConnectedCallback += (id) =>
        {
            if (NetworkManager.Singleton.IsHost)
            {
                if (NetworkManager.Singleton.LocalClientId == id)
                {
                    _bulletsManager = Instantiate(BulletsManagerPrefab);
                    _bulletsManager.NetworkObject.Spawn(true);

                    _playersManager = Instantiate(PlayersManagerPrefab);
                    _playersManager.NetworkObject.Spawn(true);
                }
            }
            Debug.Log("Player with client id " + id + " joined");
        };

        if (ServiceController.Instance.IsPlayerHostOfJoinedLobby)
        {
            NetworkManager.Singleton.StartHost();
        }
        else
        {
            NetworkManager.Singleton.StartClient();
        }
    }

    public void ShowEndGamePanel(string winnerName)
    {
        IsGameEnded = true;
        _endGamePanel.Show(winnerName);
    }

    public void GoBackToLobby()
    {
        NetworkManager.Singleton.Shutdown();
        PlayerPrefs.SetInt(ServiceController.BackFromGameplayPlayerPrefKey, 1);
        UnityEngine.SceneManagement.SceneManager.LoadScene(ServiceController.LobbySceneName);
    }
}
