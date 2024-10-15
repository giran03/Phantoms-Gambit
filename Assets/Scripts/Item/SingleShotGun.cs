using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SingleShotGun : Gun
{
	[SerializeField] Camera cam;

	[SerializeField] int gunShotIndex;
	[SerializeField] int gunReloadIndex;

	[Header("Recoil")]
	[Range(0f, 10f)] public float recoilForce = 5f;
	[Range(0f, 10f)] public float recoilRecoverySpeed = 5f;
	[Range(0f, 10f)] public float maxRecoilAngle = 10f;

	float currentRecoilAngle = 0;
	float _lastShot;

	PhotonView PV;
	private bool isReloading;
	PlayerNetworkSoundManager _playerNetworkSoundManager;

	void Awake()
	{
		PV = GetComponent<PhotonView>();
		_playerNetworkSoundManager = PlayerController.playerNetworkSoundManager;
	}

	void Update()
	{
		if (currentRecoilAngle > 0)
		{
			currentRecoilAngle = Mathf.Lerp(currentRecoilAngle, 0, recoilRecoverySpeed * Time.deltaTime);
			transform.localRotation = Quaternion.Lerp(transform.localRotation, Quaternion.identity, recoilRecoverySpeed * Time.deltaTime);
		}
	}

	private void OnDisable()
	{
		((GunInfo)itemInfo).currentAmmo = 0;
	}

	public override void Use()
	{
		Shoot();
	}

	void Shoot()
	{
		if (!PV.IsMine) return;
		if (((GunInfo)itemInfo).itemName == "Hand") return;



		if (((GunInfo)itemInfo).fireRate + _lastShot > Time.time) return;

		if (isReloading) return;
		if (((GunInfo)itemInfo).currentAmmo <= 0)
		{
			ReloadGun();
			return;
		}

		_lastShot = Time.time;
		ShootGun();
	}

	void ShootGun()
	{
		StopCoroutine(HealAfterTime());

		Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f));
		ray.origin = cam.transform.position;

		Physics.Raycast(ray, out RaycastHit hit);
		if (hit.collider != null)
		{
			var damageable = hit.collider.gameObject.GetComponent<IDamageable>();
			if (damageable != null)
			{
				damageable.TakeDamage(((GunInfo)itemInfo).damage);
			}
			else
			{
				if (GetComponentInParent<PlayerController>().IsHunter)
				{
					GetComponentInParent<PlayerController>().TakeDamage(4f);
					if (gameObject.activeSelf)
						StartCoroutine(HealAfterTime());
				}
			}
		}

		IEnumerator HealAfterTime()
		{
			Debug.Log($"Healing the player after 10 seconds~");
			yield return new WaitForSeconds(10);

			float timer = 0;
			bool isHealing = true;

			while (isHealing)
			{
				if (GetComponentInParent<PlayerController>().currentHealth >= 100)
					isHealing = false;
				else
				{
					GetComponentInParent<PlayerController>().HealPlayer(1f / 60f);
					timer += Time.deltaTime;
				}
				yield return null;
			}
		}

		Vector3 randomHit = hit.point + Random.insideUnitSphere * .25f;
		PV.RPC("RPC_Shoot", RpcTarget.All, randomHit, hit.normal);

		currentRecoilAngle += recoilForce;
		transform.localRotation = Quaternion.Euler(0, currentRecoilAngle, 0);

		_playerNetworkSoundManager.PlayGunShotSFX(gunShotIndex);

		((GunInfo)itemInfo).currentAmmo--;
	}

	public void ReloadGun()
	{
		if (!PV.IsMine) return;

		PV.RPC(nameof(RPC_Reload), RpcTarget.All);
	}

	[PunRPC]
	void RPC_Reload() => StartCoroutine(Reload());

	IEnumerator Reload()
	{
		Debug.Log("Reloading~");
		isReloading = true;

		_playerNetworkSoundManager.PlayGunShotSFX(gunReloadIndex);

		Vector3 originalRotation = transform.localRotation.eulerAngles;
		transform.localRotation = Quaternion.Euler(45, 0, 0);

		yield return new WaitForSeconds(((GunInfo)itemInfo).reloadTime);

		isReloading = false;
		transform.localRotation = Quaternion.Euler(originalRotation);
		Physics.SyncTransforms();

		int ammoToTake = ((GunInfo)itemInfo).magSize - ((GunInfo)itemInfo).currentAmmo;
		int ammoTaken = Mathf.Min(ammoToTake, ((GunInfo)itemInfo).currentMagSize);

		((GunInfo)itemInfo).currentAmmo += ammoTaken;
		((GunInfo)itemInfo).currentMagSize -= ammoTaken;
	}

	[PunRPC]
	void RPC_Shoot(Vector3 hitPosition, Vector3 hitNormal)
	{
		var hitColliders = new Collider[10];
		var numHits = Physics.OverlapSphereNonAlloc(hitPosition, 0.3f, hitColliders);

		for (int i = 0; i < numHits; i++)
		{
			if (hitColliders[0] != null)
			{
				GameObject bulletImpactObj = Instantiate(bulletImpactPrefab, hitPosition + hitNormal * 0.001f, Quaternion.LookRotation(hitNormal, Vector3.up) * bulletImpactPrefab.transform.rotation);
				Destroy(bulletImpactObj, 10f);
				bulletImpactObj.transform.SetParent(hitColliders[0].transform);
			}
		}
	}
}
