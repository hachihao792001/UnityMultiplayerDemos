using Photon.Pun;
using Photon.Realtime;
using QFSW.QC;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestConnect : MonoBehaviourPunCallbacks
{
    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("Connecting to Photon...");
        PhotonNetwork.AutomaticallySyncScene = true;
        PhotonNetwork.GameVersion = "0.0.1";
        PhotonNetwork.NickName = "Player" + Random.Range(1, 100);
        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("Connected to Photon");
        Debug.Log(PhotonNetwork.LocalPlayer.NickName);

        if (!PhotonNetwork.InLobby)
            PhotonNetwork.JoinLobby();
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.Log("Disconnected from photon " + cause);
    }

    [Command]
    public void CreateRoom(string name)
    {
        if (!PhotonNetwork.IsConnected)
            return;
        RoomOptions options = new RoomOptions();
        options.MaxPlayers = 4;
        PhotonNetwork.JoinOrCreateRoom(name, options, TypedLobby.Default);
    }

    public override void OnCreatedRoom()
    {
        Debug.Log("Created room successfully");
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.Log("Created room failed");
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        Debug.Log("Room list updated: ");
        foreach (RoomInfo roomInfo in roomList)
        {
            if (!roomInfo.RemovedFromList)
                Debug.Log(roomInfo);
        }
    }

    [Command]
    public void JoinRoom(string name)
    {
        PhotonNetwork.JoinRoom(name);
    }

    public override void OnJoinedRoom()
    {
        Debug.Log($"You joined room {PhotonNetwork.CurrentRoom.Name}, player list:");
        ListPlayersInRoom();
    }

    public string GetPlayerString(Player player)
    {
        return $"( {player.ActorNumber} | {player.NickName} )";
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log($"Player {GetPlayerString(newPlayer)} joined room, player list: ");
        ListPlayersInRoom();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        Debug.Log("Player " + GetPlayerString(otherPlayer) + " left room");
    }

    [Command]
    public void ListPlayersInRoom()
    {
        foreach (var kv in PhotonNetwork.CurrentRoom.Players)
        {
            Debug.Log(GetPlayerString(kv.Value));
        }
    }

    [Command]
    public void LeaveCurrentRoom()
    {
        PhotonNetwork.LeaveRoom(true);
    }

    public override void OnLeftRoom()
    {
        Debug.Log("You left room");
    }

    [Command]
    public void Me()
    {
        Debug.Log(GetPlayerString(PhotonNetwork.LocalPlayer));
    }

    [Command]
    public void LoadScene(string name)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.CurrentRoom.IsOpen = false;
            PhotonNetwork.CurrentRoom.IsVisible = false;
            PhotonNetwork.LoadLevel(name);
        }
    }

    private ExitGames.Client.Photon.Hashtable _myCustomProperties = new ExitGames.Client.Photon.Hashtable();
    [Command]
    public void SetMyNumber(int number)
    {
        _myCustomProperties["MyNumber"] = number;
        PhotonNetwork.SetPlayerCustomProperties(_myCustomProperties);
        //PhotonNetwork.LocalPlayer.SetCustomProperties(_myCustomProperties);
    }
    [Command]
    public void GetMyNumberOfPlayer(int actorNumber)
    {
        if (PhotonNetwork.CurrentRoom.Players[actorNumber].CustomProperties.ContainsKey("MyNumber"))
        {
            int result = (int)PhotonNetwork.CurrentRoom.Players[actorNumber].CustomProperties["MyNumber"];
            Debug.Log(result);
        }
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
    {
        Debug.Log($"Set MyNumber of {GetPlayerString(targetPlayer)} to {changedProps["MyNumber"]}");
    }

    [Command]
    public void CallRPC(int arg)
    {
        base.photonView.RPC(nameof(RPC_Test), RpcTarget.MasterClient, arg);
        //base.photonView.RpcSecure(nameof(RPC_Test), RpcTarget.MasterClient, true, arg);
    }

    [PunRPC]
    private void RPC_Test(int arg)
    {
        Debug.Log("RPC_Test called with arg=" + arg);
    }
}
