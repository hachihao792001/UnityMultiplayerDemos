using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RaiseEventExample : MonoBehaviourPun
{
    [SerializeField] MeshRenderer _meshRenderer;

    void Update()
    {
        if (photonView.IsMine && Input.GetKeyDown(KeyCode.Space))
            SetRandomColor();
    }

    private void OnEnable()
    {
        PhotonNetwork.NetworkingClient.EventReceived += NetworkingClient_EventReceived;
    }
    private void OnDisable()
    {
        PhotonNetwork.NetworkingClient.EventReceived -= NetworkingClient_EventReceived;
    }

    private void NetworkingClient_EventReceived(EventData obj)
    {
        if (obj.Code == COLOR_CHANGE_EVENT)
        {
            float[] datas = (float[])obj.CustomData;
            float r = datas[0];
            float g = datas[1];
            float b = datas[2];

            _meshRenderer.material.color = new Color(r, b, g);
        }
    }

    const byte COLOR_CHANGE_EVENT = 1;
    private void SetRandomColor()
    {
        float r = Random.Range(0f, 1f);
        float g = Random.Range(0f, 1f);
        float b = Random.Range(0f, 1f);

        _meshRenderer.material.color = new Color(r, b, g);

        float[] datas = new float[] { r, g, b };
        PhotonNetwork.RaiseEvent(COLOR_CHANGE_EVENT, datas, RaiseEventOptions.Default, SendOptions.SendUnreliable);
    }
}
