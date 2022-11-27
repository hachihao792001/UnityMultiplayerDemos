using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class NetworkManagerUI : MonoBehaviour
{
    public void ServerOnClick()
    {
        NetworkManager.Singleton.StartServer();
    }
    public void HostOnClick()
    {
        NetworkManager.Singleton.StartHost();
    }
    public void ClientOnClick()
    {
        NetworkManager.Singleton.StartClient();
    }
}
