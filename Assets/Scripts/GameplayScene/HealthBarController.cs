using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class HealthBarController : MonoBehaviour
{
    [SerializeField] Image _healthBar;

    public void UpdateHealthBar(float currentHealth, float fullHealth)
    {
        _healthBar.fillAmount = currentHealth / fullHealth;
    }

    private void Update()
    {
        transform.LookAt(Camera.main.transform);
    }
}
