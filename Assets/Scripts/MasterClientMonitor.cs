using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MasterClientMonitor : MonoBehaviourPunCallbacks
{
    public override void OnMasterClientSwitched(Player newMasterClient)
    {

    }

    public override void OnFriendListUpdate(List<FriendInfo> friendList)
    {
        base.OnFriendListUpdate(friendList);

        foreach (FriendInfo friendInfo in friendList)
        {
            Debug.Log("Friend info received " + friendInfo.UserId + " is online?" + friendInfo.IsOnline);
        }
    }
}
