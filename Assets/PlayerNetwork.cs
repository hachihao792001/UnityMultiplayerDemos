using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class PlayerNetwork : NetworkBehaviour
{
    [SerializeField] GameObject spawnObjectPrefab;
    GameObject spawnedObject;

    private NetworkVariable<int> randomNumber = new NetworkVariable<int>(1, readPerm: NetworkVariableReadPermission.Everyone, writePerm: NetworkVariableWritePermission.Owner);
    private NetworkVariable<FixedString128Bytes> myStr = new NetworkVariable<FixedString128Bytes>();

    public override void OnNetworkSpawn()
    {
        randomNumber.OnValueChanged += (prev, next) =>
        {
            Debug.Log(OwnerClientId + ", randomNumber: " + randomNumber.Value);
        };
    }

    public struct MyCustomData : INetworkSerializable
    {
        public int _int;
        public bool _bool;
        public FixedString128Bytes _message;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref _int);
            serializer.SerializeValue(ref _bool);
            serializer.SerializeValue(ref _message);
        }
    }

    void Update()
    {
        if (!IsOwner) return;

        if (Input.GetKeyDown(KeyCode.T))
        {
            SpawnObjectServerRpc();

            //TestClientRpc(new ClientRpcParams
            //{
            //    Send = new ClientRpcSendParams { TargetClientIds = new List<ulong> { 1 } }
            //});
            //TestServerRpc(new ServerRpcParams());
            //randomNumber.Value = Random.Range(0, 100);
        }

        if (Input.GetKeyDown(KeyCode.Y))
        {
            DespawnSpawnedObjectServerRpc();
        }

        Vector3 input = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
        transform.position += input * 4 * Time.deltaTime;
    }

    [ServerRpc]
    private void TestServerRpc(ServerRpcParams serverRpcParams)
    {
        Debug.Log("TestServerRpc " + OwnerClientId + "; " + serverRpcParams.Receive.SenderClientId);
    }

    [ClientRpc]
    private void TestClientRpc(ClientRpcParams clientRpcParams)
    {
        Debug.Log("TestClientRpc ");
    }

    [ServerRpc]
    private void DespawnSpawnedObjectServerRpc()
    {
        if (spawnedObject != null)
            Destroy(spawnedObject);
    }

    [ServerRpc]
    private void SpawnObjectServerRpc()
    {
        spawnedObject = Instantiate(spawnObjectPrefab);
        spawnedObject.GetComponent<NetworkObject>().Spawn(true);
    }
}
