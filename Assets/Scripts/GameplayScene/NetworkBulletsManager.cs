using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class NetworkBulletsManager : NetworkBehaviour
{
    public static int NextAvailableBulletId = 1;
    public static Dictionary<int, BulletController> BulletDic = new Dictionary<int, BulletController>();

    [SerializeField] BulletController _bulletPrefab;
    [SerializeField] float _shootBulletForce;

    [ServerRpc(RequireOwnership = false)]
    public void SpawnBulletServerRpc(ulong shooterOwnerClientId, Vector3 pos, Vector3 dir)
    {
        BulletController newBullet = Instantiate(_bulletPrefab, pos, Quaternion.identity);
        newBullet.bulletId.Value = NextAvailableBulletId++;
        newBullet.shooterOwnerClientId.Value = shooterOwnerClientId;
        BulletDic.Add(newBullet.bulletId.Value, newBullet);

        newBullet.GetComponent<NetworkObject>().Spawn(true);

        newBullet.RB.AddForce(dir * _shootBulletForce);

        Debug.Log("Spawned bullet shoot by " + NetworkPlayersManager.PlayerDic[shooterOwnerClientId].playerName);
    }

    [ServerRpc(RequireOwnership = false)]
    public void DestroyBulletServerRpc(int bulletId)
    {
        if (BulletDic.ContainsKey(bulletId))
        {
            BulletController cachedBullet = BulletDic[bulletId];
            BulletDic.Remove(bulletId);
            cachedBullet.NetworkObject.Despawn();
        }
    }
}
