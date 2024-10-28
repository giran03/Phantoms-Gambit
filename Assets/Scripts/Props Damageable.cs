using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PropsDamageable : MonoBehaviour, IDamageable
{
    PlayerController _playerController;
    private void Awake() => _playerController = GetComponentInParent<PlayerController>();
    public void TakeDamage(float damage, bool byPassCanKillHunter = false) => _playerController.TakeDamage(damage);
}