﻿using System.Collections.Generic;
using TMPro;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

public class LobbyListScreenController : MonoBehaviour
{
    [SerializeField] TMP_Text _txtPlayerName;
    [SerializeField] Image _imgPlayerColor;

    [SerializeField] Transform _lobbyItemsParent;
    [SerializeField] LobbyItemController _lobbyItemPrefab;

    [SerializeField] LobbyScreenController _lobbyScreen;

    [SerializeField] TMP_InputField _inputLobbyCode;

    private void Start()
    {
        ServiceController.Instance.onLobbyCreated += OnLobbyCreated;
        ServiceController.Instance.onJoinedLobby += OnJoinedLobby;
        ServiceController.Instance.onPlayerDataUpdated += UpdatePlayerDataUI;

        UpdatePlayerDataUI();
    }
    private void OnDestroy()
    {
        ServiceController.Instance.onLobbyCreated -= OnLobbyCreated;
        ServiceController.Instance.onJoinedLobby -= OnJoinedLobby;
        ServiceController.Instance.onPlayerDataUpdated -= UpdatePlayerDataUI;
    }

    void OnLobbyCreated(Lobby lobby)
    {
        RefreshLobbiesListUI();
    }

    void OnJoinedLobby(Lobby lobby)
    {
        gameObject.SetActive(false);
        _lobbyScreen.Show(lobby);
    }

    private void OnEnable()
    {
        if (ServiceController.IsInitialized)
            RefreshLobbiesListUI();

        CheckBackFromGameplay();
    }
    async void CheckBackFromGameplay()
    {
        if (ServiceController.IsBackFromGameplay)
        {
            OnJoinedLobby(ServiceController.joinedLobby);
            if (ServiceController.Instance.IsPlayerHostOfJoinedLobby)
                await ServiceController.Instance.JoinLobbyStopPlaying();

            ServiceController.IsBackFromGameplay = false;
        }
    }

    void UpdatePlayerDataUI()
    {
        _txtPlayerName.text = ServiceController.PlayerName;

        Color playerColor;
        ColorUtility.TryParseHtmlString("#" + ServiceController.PlayerColor, out playerColor);
        _imgPlayerColor.color = playerColor;
    }

    public async void RefreshLobbiesListUI()
    {
        foreach (Transform child in _lobbyItemsParent)
        {
            Destroy(child.gameObject);
        }

        List<Lobby> updatedLobbiesList = await ServiceController.Instance.GetLobbiesListFromService();
        if (updatedLobbiesList != null)
        {
            foreach (Lobby lobby in updatedLobbiesList)
            {
                LobbyItemController newLobbyItem = Instantiate(_lobbyItemPrefab, _lobbyItemsParent);
                newLobbyItem.SetData(lobby);
            }
        }
    }

    public void JoinByCodeOnClick()
    {
        ServiceController.Instance.JoinLobbyByCode(_inputLobbyCode.text);
    }

    public void QuickJoinOnClick()
    {
        ServiceController.Instance.QuickJoinLobby();
    }
}
