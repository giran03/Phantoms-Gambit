using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Player Ability")]
public class Ability : ScriptableObject
{
    public AbilityHandler.AbilityType abilityType;
    public float AbilityCooldown;
    public bool OnCooldown;
}