using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SingleShotGun : Gun
{
	[SerializeField] Camera cam;

	[SerializeField] int gunShotIndex;
	[SerializeField] int gunReloadIndex;
	[SerializeField] LayerMask shootableLayers;

	[Header("Recoil")]
	[Range(0f, 10f)] public float recoilForce = 5f;
	[Range(0f, 10f)] public float recoilRecoverySpeed = 5f;
	[Range(0f, 10f)] public float maxRecoilAngle = 10f;

	float currentRecoilAngle = 0;
	float _lastShot;

	PhotonView PV;
	bool isReloading;
	PlayerNetworkSoundManager _playerNetworkSoundManager;
	bool canShoot = true;

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

	public override void Use() => Shoot();

	void Shoot()
	{
		if (!PV.IsMine) return;

		if (!canShoot) return;

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
		Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f));
		ray.origin = cam.transform.position;

		Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, shootableLayers);
		if (hit.collider != null)
		{
			if (hit.collider.gameObject.TryGetComponent<IDamageable>(out var damageable))
				damageable.TakeDamage((int)((GunInfo)itemInfo).damage);

			else
			{
				if (GetComponentInParent<PlayerController>().IsHunter)
					if (hit.collider.gameObject.TryGetComponent<DestroyableProps>(out var destroyableProps))
						if (destroyableProps.CanDamagePlayer)
							GetComponentInParent<PlayerController>().TakeDamage(10f);
			}

			if (GetComponentInParent<PlayerController>().IsHunter)
			{
				if (hit.collider.gameObject.TryGetComponent<IDestroyable>(out var destroyable))
					destroyable.Damage((int)((GunInfo)itemInfo).damage);
			}

			Debug.Log($"HITTING {hit.collider.gameObject}");
		}

		Vector3 randomHit = hit.point + Random.insideUnitSphere * .25f;
		PV.RPC("RPC_Shoot", RpcTarget.All, randomHit, hit.normal);

		currentRecoilAngle += recoilForce;
		transform.localRotation = Quaternion.Euler(0, currentRecoilAngle, 0);

		_playerNetworkSoundManager.PlayGunShotSFX(gunShotIndex);

		((GunInfo)itemInfo).currentAmmo--;

		if (GetComponentInParent<PlayerController>().IsHunter)
		{
			StartCoroutine(HealAfterTimeIfNoShooting());
		}

		IEnumerator HealAfterTimeIfNoShooting()
		{
			float timer = 0;
			bool isHealing = true;

			while (isHealing)
			{
				if (_lastShot + 5 < Time.time)
				{
					isHealing = false;
					StartCoroutine(HealAfterTime());
				}
				else
				{
					timer += Time.deltaTime;
				}
				yield return null;
			}
		}

		IEnumerator HealAfterTime()
		{
			float timer = 0;
			bool isHealing = true;

			while (isHealing)
			{
				if (GetComponentInParent<PlayerController>().currentHealth >= 100)
				{
					canShoot = true;
					isHealing = false;
				}
				else
				{
					canShoot = false;
					GetComponentInParent<PlayerController>().HealPlayer(1f / 60f);
					timer += Time.deltaTime;
				}
				yield return null;
			}
		}
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

		if (PhotonNetwork.LocalPlayer.CustomProperties["assignment"].ToString() == "Hunter")
		{
			int ammoToTake = ((GunInfo)itemInfo).magSize - ((GunInfo)itemInfo).currentAmmo;
			int ammoTaken = Mathf.Min(ammoToTake, ((GunInfo)itemInfo).currentMagSize);

			((GunInfo)itemInfo).currentAmmo += ammoTaken;
		}
		else
		{
			int ammoToTake = ((GunInfo)itemInfo).magSize - ((GunInfo)itemInfo).currentAmmo;
			int ammoTaken = Mathf.Min(ammoToTake, ((GunInfo)itemInfo).currentMagSize);

			((GunInfo)itemInfo).currentAmmo += ammoTaken;
			((GunInfo)itemInfo).currentMagSize -= ammoTaken;
		}
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
