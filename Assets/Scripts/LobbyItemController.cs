using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

public class LobbyItemController : MonoBehaviour
{
    public Lobby lobby;
    [SerializeField] TMP_Text _txtLobbyName;
    [SerializeField] TMP_Text _txtPlayers;

    public void SetData(Lobby lobby)
    {
        this.lobby = lobby;
        _txtLobbyName.text = lobby.Name;
        _txtPlayers.text = $"Players: {lobby.Players.Count}/{lobby.MaxPlayers}";
    }

    public void JoinButtonOnClick()
    {
        ServiceController.Instance.JoinLobby(lobby);
    }
}
