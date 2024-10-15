using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "FPS/New Gun")]
public class GunInfo : ItemInfo
{
	public string SFXName;
	public string ReloadSFXName;
	public float damage;
	public float fireRate;
	public bool isAutomatic;
	public float recoil;
	public int magSize;
	public int currentAmmo;
	public float reloadTime;
	public int currentMagSize;
}