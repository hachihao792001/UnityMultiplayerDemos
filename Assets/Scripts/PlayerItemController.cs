using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

public class PlayerItemController : MonoBehaviour
{
    [SerializeField] Image _bg;
    [SerializeField] TMP_Text _txtPlayerName;
    [SerializeField] GameObject _playerColor;
    [SerializeField] Image _imgPlayerColor;
    [SerializeField] GameObject _host;

    public void SetData(string hostId, Player player)
    {
        _bg.color = player.Id == ServiceController.PlayerId ? Color.yellow : Color.white;
        _txtPlayerName.text = player.Data[ServiceController.PlayerNameDataKey].Value;

        _playerColor.SetActive(true);
        Color playerColor;
        ColorUtility.TryParseHtmlString("#" + player.Data[ServiceController.PlayerColorDataKey].Value, out playerColor);
        _imgPlayerColor.color = playerColor;

        _host.SetActive(player.Id == hostId);
    }

    public void SetEmpty()
    {
        _bg.color = Color.white;
        _txtPlayerName.text = "";
        _playerColor.SetActive(false);
        _host.SetActive(false);
    }

    public void SetInactive()
    {
        _bg.color = Color.gray;
        _txtPlayerName.text = "";
        _playerColor.SetActive(false);
        _host.SetActive(false);
    }
}
