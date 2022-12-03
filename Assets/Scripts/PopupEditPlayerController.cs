using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PopupEditPlayerController : MonoBehaviour
{
    [SerializeField] TMP_InputField _inputPlayerName;
    [SerializeField] TMP_InputField _inputPlayerColor;
    [SerializeField] Image _imgPlayerColor;

    private void Start()
    {
        _inputPlayerColor.onEndEdit.AddListener((val) =>
        {
            Color playerColor;
            ColorUtility.TryParseHtmlString("#" + val, out playerColor);
            _imgPlayerColor.color = playerColor;
        });
    }

    private void OnEnable()
    {
        _inputPlayerName.text = ServiceController.PlayerName;
        _inputPlayerColor.text = ServiceController.PlayerColor;
    }

    public void UpdateOnClick()
    {
        ServiceController.Instance.UpdatePlayerData(_inputPlayerName.text, _inputPlayerColor.text);
        gameObject.SetActive(false);
    }
}
