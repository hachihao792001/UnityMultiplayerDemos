using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PopupEditLobbyController : MonoBehaviour
{
    [SerializeField] TMP_InputField _inputLobbyName;
    [SerializeField] TMP_Dropdown _dropDownMaxPlayers;
    [SerializeField] Toggle _toggleIsPrivate;

    private void OnEnable()
    {
        _inputLobbyName.text = ServiceController.joinedLobby.Name;
        _dropDownMaxPlayers.value = _dropDownMaxPlayers.options.FindIndex(x => x.text == ServiceController.joinedLobby.MaxPlayers.ToString());
        _toggleIsPrivate.isOn = ServiceController.joinedLobby.IsPrivate;
    }

    public async void UpdateOnClick()
    {
        await ServiceController.Instance.UpdateJoinedLobbyData(
              _inputLobbyName.text,
              int.Parse(_dropDownMaxPlayers.options[_dropDownMaxPlayers.value].text),
              _toggleIsPrivate.isOn);
        gameObject.SetActive(false);
    }
}
