using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Moving : MonoBehaviourPun, IPunObservable
{
    [SerializeField] float _speed;

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        //info: thông tin về player của photonView này
        //stream: ghi/đọc dữ liệu tùy mình là owner/player khác
        if (stream.IsWriting)
        {
            stream.SendNext(transform.position);
            stream.SendNext(transform.rotation);
        }
        else if (stream.IsReading)
        {
            transform.position = (Vector3)stream.ReceiveNext();
            transform.rotation = (Quaternion)stream.ReceiveNext();
        }
    }

    void FixedUpdate()
    {
        if (!photonView.IsMine) return;
        Vector2 input = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        if (input != Vector2.zero)
        {
            transform.Translate(input * _speed);
        }
    }
}
