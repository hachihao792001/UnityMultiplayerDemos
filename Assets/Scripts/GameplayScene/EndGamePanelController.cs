using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class EndGamePanelController : MonoBehaviour
{
    [SerializeField] TMP_Text _txtWinner;

    public void Show(string winnerName)
    {
        _txtWinner.text = winnerName + " won!";
        gameObject.SetActive(true);
    }

    public void LobbyOnClick()
    {
        GameplayController.Instance.GoBackToLobby();
    }
}
