using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class BulletController : NetworkBehaviour
{
    public NetworkVariable<ulong> shooterOwnerClientId = new NetworkVariable<ulong>();
    public NetworkVariable<int> bulletId = new NetworkVariable<int>();
    [SerializeField] float _damage;
    public Rigidbody RB;

    private IEnumerator Start()
    {
        yield return new WaitForSeconds(5f);
        GameplayController.Instance.BulletsManager.DestroyBulletServerRpc(bulletId.Value);
    }

    private void OnCollisionEnter(Collision collision)
    {
        GameObject hit = collision.collider.gameObject;
        if (hit.CompareTag("Player"))
        {
            PlayerController hitPlayer = hit.GetComponent<PlayerController>();
            Debug.Log("Bullet of " + NetworkPlayersManager.PlayerDic[shooterOwnerClientId.Value].playerName +
                        " hit " + NetworkPlayersManager.PlayerDic[hitPlayer.NetworkObject.OwnerClientId].playerName);

            if (shooterOwnerClientId.Value != hitPlayer.NetworkObject.OwnerClientId) //đạn không sát thương người bắn
                hitPlayer.TakeHealth(_damage);
        }
        GameplayController.Instance.BulletsManager.DestroyBulletServerRpc(bulletId.Value);
    }
}
