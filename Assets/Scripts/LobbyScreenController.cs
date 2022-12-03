using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Services.Lobbies.Models;
using UnityEngine;

public class LobbyScreenController : MonoBehaviour
{
    [SerializeField] GameObject _lobbyListScreen;

    [SerializeField] TMP_Text _txtLobbyName;
    [SerializeField] PlayerItemController[] _playerItems;
    [SerializeField] GameObject _updateLobbyDataButton;
    [SerializeField] LobbyDataPanelController _lobbyDataPanel;

    private void Start()
    {
        ServiceController.Instance.onLobbyUpdated += RefreshScreen;
    }

    public void Show(Lobby lobby)
    {
        gameObject.SetActive(true);

        RefreshScreen(lobby);
    }

    void RefreshScreen(Lobby lobby)
    {
        _txtLobbyName.text = lobby.Name;
        RefreshPlayers(lobby);
        _updateLobbyDataButton.SetActive(ServiceController.Instance.IsPlayerHostOfJoinedLobby);
        _lobbyDataPanel.RefreshPanel();
    }

    void RefreshPlayers(Lobby lobby)
    {
        for (int i = 0; i < _playerItems.Length; i++)
        {
            if (i >= lobby.MaxPlayers)
            {
                _playerItems[i].SetInactive();
            }
            else if (i >= lobby.Players.Count)
            {
                _playerItems[i].SetEmpty();
            }
            else
            {
                _playerItems[i].SetData(lobby.HostId, lobby.Players[i]);
            }
        }
    }

    public async void LeaveOnClick()
    {
        await ServiceController.Instance.LeaveJoinedLobby();
        gameObject.SetActive(false);
        _lobbyListScreen.SetActive(true);
    }

    public void StartOnClick()
    {

    }
}