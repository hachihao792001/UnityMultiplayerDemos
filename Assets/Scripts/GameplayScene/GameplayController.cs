using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

public class GameplayController : MonoBehaviour
{
    void Start()
    {
        if (ServiceController.Instance.IsPlayerHostOfJoinedLobby)
        {
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(
                ServiceController.RelayHostData.IPv4Address,
                ServiceController.RelayHostData.Port,
                ServiceController.RelayHostData.AllocationIDBytes,
                ServiceController.RelayHostData.Key,
                ServiceController.RelayHostData.ConnectionData);

            NetworkManager.Singleton.StartHost();
        }
        else
        {
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetClientRelayData(
                ServiceController.RelayJoinData.IPv4Address,
                ServiceController.RelayJoinData.Port,
                ServiceController.RelayJoinData.AllocationIDBytes,
                ServiceController.RelayJoinData.Key,
                ServiceController.RelayJoinData.ConnectionData,
                ServiceController.RelayJoinData.HostConnectionData);

            NetworkManager.Singleton.StartClient();
        }
    }
}
