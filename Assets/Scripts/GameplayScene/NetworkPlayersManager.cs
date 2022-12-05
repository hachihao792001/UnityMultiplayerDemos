using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public struct ClientData
{
    public PlayerController playerController;
    public string playerName;
    public string playerColor;

    public ClientData(PlayerController playerController, string playerName, string playerColor)
    {
        this.playerController = playerController;
        this.playerName = playerName;
        this.playerColor = playerColor;
    }
}

public class NetworkPlayersManager : NetworkBehaviour
{
    public static Dictionary<ulong, ClientData> PlayerDic = new Dictionary<ulong, ClientData>();

    public override void OnNetworkSpawn()
    {
        PlayerDic.Clear();
        OnClientConnectedServerRpc(NetworkManager.Singleton.LocalClientId, ServiceController.PlayerName, ServiceController.PlayerColor);
    }

    [ServerRpc(RequireOwnership = false)]
    public void OnClientConnectedServerRpc(ulong clientId, string playerName, string playerColor)
    {
        PlayerController newClientPlayer = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject.gameObject.GetComponent<PlayerController>();

        PlayerDic.Add(clientId, new ClientData(newClientPlayer, playerName, playerColor));

        foreach (var kv in PlayerDic)
        {
            UpdatePlayersDataClientRpc(kv.Value.playerController, kv.Value.playerName, kv.Value.playerColor);
        }
    }

    [ClientRpc]
    public void UpdatePlayersDataClientRpc(NetworkBehaviourReference networkBehaviorRefernce, string playerName, string playerColor)
    {
        NetworkBehaviour playerNetworkBehavior;
        networkBehaviorRefernce.TryGet(out playerNetworkBehavior);

        if (playerNetworkBehavior != null)
        {
            PlayerController newClientPlayer = playerNetworkBehavior as PlayerController;
            newClientPlayer.UpdatePlayerData(playerName, playerColor);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void PlayerDeadServerRpc(ulong playerOwnerClientId)
    {
        PlayerDic[playerOwnerClientId].playerController.IsDead.Value = true;
        Debug.Log(PlayerDic[playerOwnerClientId].playerName + " died");

        PlayerDeadClientRpc(PlayerDic[playerOwnerClientId].playerController);

        List<ulong> alivePlayers = new List<ulong>();
        foreach (var kv in PlayerDic)
        {
            if (!kv.Value.playerController.IsDead.Value)
                alivePlayers.Add(kv.Key);
        }
        Debug.Log("Alive players: " + alivePlayers.Count);
        if (alivePlayers.Count == 1)
        {
            ClientData winClientData = PlayerDic[alivePlayers[0]];
            EndGameClientRpc(winClientData.playerName);
        }
    }

    [ClientRpc]
    public void PlayerDeadClientRpc(NetworkBehaviourReference networkBehaviorRefernce)
    {
        NetworkBehaviour playerNetworkBehavior;
        networkBehaviorRefernce.TryGet(out playerNetworkBehavior);

        if (playerNetworkBehavior != null)
        {
            PlayerController deadPlayer = playerNetworkBehavior as PlayerController;
            deadPlayer.gameObject.SetActive(false);
        }
    }

    [ClientRpc]
    public void EndGameClientRpc(string winPlayerName)
    {
        GameplayController.Instance.ShowEndGamePanel(winPlayerName);
    }
}
