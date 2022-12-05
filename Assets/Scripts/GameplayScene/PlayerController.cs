using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class PlayerController : NetworkBehaviour
{
    [SerializeField] Rigidbody _rb;

    [SerializeField] HealthBarController _healthBarController;
    [SerializeField] float _fullHealth;
    NetworkVariable<float> _currentHealth = new NetworkVariable<float>(1);
    public NetworkVariable<bool> IsDead = new NetworkVariable<bool>(false);

    [SerializeField] MeshRenderer _meshRenderer;
    [SerializeField] TMP_Text _txtPlayerName;

    [SerializeField] float _moveSpeed = 4;
    [SerializeField] float _rotateSpeed;

    [SerializeField] Transform _bulletSpawnPos;

    public override void OnNetworkSpawn()
    {
        _currentHealth.OnValueChanged += OnHealthUpdate;
        _currentHealth.Value = _fullHealth;
    }

    public void UpdatePlayerData(string name, string hexColor)
    {
        _txtPlayerName.text = name;
        Color playerColor;
        ColorUtility.TryParseHtmlString("#" + hexColor, out playerColor);
        _meshRenderer.material.color = playerColor;
    }

    void Update()
    {
        if (GameplayController.IsGameEnded || !IsOwner || IsDead.Value) return;

        Vector3 input = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
        _rb.velocity = input * _moveSpeed;

        if (Input.GetKey(KeyCode.Q))
        {
            transform.Rotate(0, -_rotateSpeed * Time.deltaTime, 0);
        }
        if (Input.GetKey(KeyCode.E))
        {
            transform.Rotate(0, _rotateSpeed * Time.deltaTime, 0);
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            GameplayController.Instance.BulletsManager.SpawnBulletServerRpc(
                NetworkObject.OwnerClientId,
                _bulletSpawnPos.position,
                transform.forward);
        }
    }

    public void TakeHealth(float damage)
    {
        _currentHealth.Value -= damage;
    }

    void OnHealthUpdate(float prevValue, float newValue)
    {
        _healthBarController.UpdateHealthBar(newValue, _fullHealth);
        if (newValue <= 0 && IsOwner)
        {
            GameplayController.Instance.PlayersManager.PlayerDeadServerRpc(OwnerClientId);
        }
    }
}
