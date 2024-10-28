using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class DestroyableProps : MonoBehaviourPunCallbacks, IDestroyable
{
    [SerializeField] int _health = 100;
    [SerializeField] bool isRespawnable = true;
    [SerializeField] List<GameObject> propsModel;
    public bool CanDamagePlayer = true;

    DestroyableStats destroyableStats;

    private void Start() => destroyableStats = new(gameObject, _health, DestroyableStats.DamageMultiplier.Fragile);
    public void Damage(int damageAmount) => destroyableStats.Hit(damageAmount, propsModel, isRespawnable);
    public override void OnDisable() => StopAllCoroutines();
}