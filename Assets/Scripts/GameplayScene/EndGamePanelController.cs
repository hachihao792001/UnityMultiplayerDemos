using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class EndGamePanelController : MonoBehaviour
{
    [SerializeField] TMP_Text _txtWinner;
    [SerializeField] TMP_Text _txtBackToLobby;

    public void Show(string winnerName)
    {
        _txtWinner.text = winnerName + " won!";
        gameObject.SetActive(true);

        StartCoroutine(BackToLobbyCountDown());
    }

    IEnumerator BackToLobbyCountDown()
    {
        int second = 5;
        _txtBackToLobby.text = $"Return to lobby in {second}s";
        while (second > 0)
        {
            yield return new WaitForSeconds(1f);
            second--;
            _txtBackToLobby.text = $"Return to lobby in {second}s";
        }

        GameplayController.Instance.GoBackToLobby();
    }
}
