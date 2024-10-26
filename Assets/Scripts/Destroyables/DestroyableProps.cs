using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestroyableProps : MonoBehaviour, IDestroyable
{
    [SerializeField] int _health = 100;
    [SerializeField] bool isRespawnable = true;
    [SerializeField] List<GameObject> propsModel;
    public bool CanDamagePlayer = true;
    
    DestroyableStats destroyableStats;

    private void Start() => destroyableStats = new(gameObject, _health, DestroyableStats.DamageMultiplier.Fragile);
    public void Damage(int damageAmount) => destroyableStats.Hit(damageAmount, propsModel, isRespawnable);
}