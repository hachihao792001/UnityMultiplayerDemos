using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PopupCreateLobbyController : MonoBehaviour
{
    [SerializeField] TMP_InputField _inputLobbyName;
    [SerializeField] TMP_Dropdown _dropDownMaxPlayers;
    [SerializeField] Toggle _toggleIsPrivate;

    public async void CreateOnClick()
    {
        await ServiceController.Instance.CreateLobby(
              _inputLobbyName.text,
              int.Parse(_dropDownMaxPlayers.options[_dropDownMaxPlayers.value].text),
              _toggleIsPrivate.isOn);
        gameObject.SetActive(false);
    }
}
