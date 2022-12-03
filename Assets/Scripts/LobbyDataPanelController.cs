using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyDataPanelController : MonoBehaviour
{
    [SerializeField] TMP_Text _txtLobbyCode;
    [SerializeField] TMP_Text _txtMaxPlayers;
    [SerializeField] Toggle _togglePrivate;

    public void RefreshPanel()
    {
        if (ServiceController.joinedLobby != null)
        {
            _txtLobbyCode.text = ServiceController.joinedLobby.LobbyCode;
            _txtMaxPlayers.text = ServiceController.joinedLobby.MaxPlayers.ToString();
            _togglePrivate.isOn = ServiceController.joinedLobby.IsPrivate;
        }
    }
}
