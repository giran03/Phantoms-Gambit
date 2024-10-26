using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class PlayerController : MonoBehaviourPunCallbacks, IDamageable, IPunObservable
{
	[SerializeField] Image healthbarImage;
	[SerializeField] GameObject ui;

	[SerializeField] GameObject cameraHolder;

	[SerializeField] float mouseSensitivity, sprintSpeed, walkSpeed, jumpForce, smoothTime;

	[SerializeField] Item[] items;

	[Header("Props Configs")]
	[SerializeField] GameObject[] props;
	[SerializeField] GameObject[] Map2_props;
	GameObject[] _MapProps;
	bool isSwappingModel = false;
	int _currentPropIndex;
	bool IsProp;

	[Header("Configs")]
	public bool IsHunter;
	[SerializeField] float gravityValue = 2f;
	[SerializeField] Transform orientation;
	[SerializeField] LayerMask GroundLayer;
	[SerializeField] TMP_Text stunTimerText;
	float _footstepSFXCooldown = 0f;
	float currentSpeed;
	Vector3 moveDirection;
	RaycastHit slopeHit;
	(Vector3, quaternion) InitialPosition;

	[Header("HUD")]
	[SerializeField] TMP_Text ammoText;
	[SerializeField] GameObject rescueOverlay;
	[SerializeField] Image rescueProgressBar;

	[Header("Abilities")]
	public Ability[] _abilities = new Ability[4];
	private float _propsDefaultSpeed;

	public float MoveSpeedBurst { get; set; }

	int itemIndex;
	int previousItemIndex = -1;

	float verticalLookRotation;
	bool grounded;
	bool isJumpingOnCooldown;

	Rigidbody rb;

	public PhotonView photonViewPlayer;

	public const float maxHealth = 100f;
	public float currentHealth = maxHealth;

	PlayerManager playerManager;

	// hunter vars
	public bool canKillHunter;
	GunInfo gunInfo;

	// puzzle
	[Header("HUD")]
	[SerializeField] GameObject cantMoveIcon;

	// Player resuce vars
	const float rescueTime = 10f;
	GameObject rescuerProps;

	// IPunobservable vars
	Vector3 CheckAnimationMovement;

	public static PlayerNetworkSoundManager playerNetworkSoundManager;
	public bool CanMove { get; set; } = true;
	public float StunDuration { get; private set; }

	// HUNTER VARS
	public bool IsAuraActive { get; set; }

	bool lockMovement = false;

	private bool isStunned;

	void Awake()
	{
		rb = GetComponent<Rigidbody>();
		photonViewPlayer = GetComponent<PhotonView>();
		playerNetworkSoundManager = GetComponentInChildren<PlayerNetworkSoundManager>();

		playerManager = PhotonView.Find((int)photonViewPlayer.InstantiationData[0]).GetComponent<PlayerManager>();
	}

	void Start()
	{
		if (photonViewPlayer.IsMine)
		{
			// Cursor.lockState = CursorLockMode.Locked;
			// Cursor.visible = false;

			if (!IsHunter)
			{
				rescueOverlay.SetActive(false);

				switch (SceneManager.GetActiveScene().name)
				{
					case "Map1":
						SetPropList(1);
						break;

					case "Map2":
						SetPropList(2);
						break;
				}

				rescueProgressBar.fillAmount = 0f;
				photonViewPlayer.RPC(nameof(SetDefaultProp), RpcTarget.All);

				InitialPosition = (transform.position, transform.rotation);
			}

			stunTimerText.gameObject.SetActive(false);

			EquipItem(0);
			UpdatePlayerProperties_HP();

			StunDuration = IsHunter ? 7 : 45;
		}
		else
		{
			Destroy(GetComponentInChildren<Camera>().gameObject);
			Destroy(rb);
			Destroy(ui);
		}
	}

	void Update()
	{
		if (!photonViewPlayer.IsMine) return;

		//TODO: REMOVE THIS!!!
		if (Input.GetKeyDown(KeyCode.B))
		{
			Debug.Log($"FORCE REVIVED PLAYER!");
			SaveDownedPlayer();
		}

		// toggle movement
		if (Input.GetKeyDown(KeyCode.L))
			lockMovement = !lockMovement;

		if (!CanMove)
		{
			cantMoveIcon.SetActive(true);
			ShowPlayerOutline();

			if (!isStunned)
				StartCoroutine(UpdateStunnedTimer(StunDuration));

			return;
		}
		else
			cantMoveIcon.SetActive(false);

		if (lockMovement) return;

		currentSpeed = Input.GetKey(KeyCode.LeftShift) ? sprintSpeed + MoveSpeedBurst : walkSpeed + MoveSpeedBurst;

		if (!IsHunter)
		{
			grounded = Physics.Raycast(_MapProps[_currentPropIndex].transform.position + _MapProps[_currentPropIndex].transform.TransformVector(Vector3.zero),
			Vector3.down, out RaycastHit hit, 1.8f, GroundLayer);

			Debug.DrawRay(_MapProps[_currentPropIndex].transform.position + _MapProps[_currentPropIndex].transform.TransformVector(Vector3.zero),
			Vector3.down * 1.8f, Color.green);

			Debug.Log($"grounded: {grounded} | _currentPropIndex: {_currentPropIndex}");
		}

		Look();
		Move();
		Jump();
		SpeedControl();

		for (int i = 0; i < items.Length; i++)
		{
			if (Input.GetKeyDown((i + 1).ToString()))
			{
				EquipItem(i);
				break;
			}
		}

		#region  Keybinds

		gunInfo = (GunInfo)items[itemIndex].itemInfo;

		if (gunInfo != null)
		{
			ammoText.SetText($"{gunInfo.currentAmmo} / {gunInfo.currentMagSize}");

			if (gunInfo != null)
			{
				if (gunInfo.isAutomatic)
					GunButton(gunInfo.isAutomatic);
				else
					GunButton();
			}
		}
		else
		{
			ammoText.SetText($"");
			if (Input.GetMouseButtonDown(0))
				items[itemIndex].Use();
		}

		if (Input.GetAxisRaw("Mouse ScrollWheel") > 0f)
		{
			if (itemIndex >= items.Length - 1)
				EquipItem(0);
			else
				EquipItem(itemIndex + 1);
		}
		else if (Input.GetAxisRaw("Mouse ScrollWheel") < 0f)
		{
			if (itemIndex <= 0)
				EquipItem(items.Length - 1);
			else
				EquipItem(itemIndex - 1);
		}

		if (Input.GetMouseButtonDown(0))
			items[itemIndex].Use();

		if (Input.GetKeyDown(KeyCode.R))
			items[itemIndex].GetComponent<SingleShotGun>().ReloadGun();

		// swap props
		if (Input.GetKeyDown(KeyCode.G))
		{
			if (IsHunter) return;
			photonViewPlayer.RPC(nameof(RPC_SwapModel), RpcTarget.All);
		}
		#endregion

		#region Abilities
		// ability
		if (!IsHunter)
		{
			if (IsAuraActive)
			{
				Debug.LogError($"Aura of fear is active!");
				return;
			}
			AbilityKeybinds();
		}
		else    // HUNTER ABILITIES
			AbilityKeybinds();

		void AbilityKeybinds()
		{
			if (Input.GetKeyDown(KeyCode.Q))
			{
				if (_abilities[0].OnCooldown) return;
				StartCoroutine(InitiateAbility(_abilities[0]));
			}
			if (Input.GetKeyDown(KeyCode.E))
			{
				if (_abilities[1].OnCooldown) return;
				StartCoroutine(InitiateAbility(_abilities[1]));
			}
			if (Input.GetKeyDown(KeyCode.C))
			{
				if (_abilities[2].OnCooldown) return;
				StartCoroutine(InitiateAbility(_abilities[2]));
			}
			if (Input.GetKeyDown(KeyCode.V))
			{
				if (_abilities[3].OnCooldown) return;
				StartCoroutine(InitiateAbility(_abilities[3]));
			}
		}

		IEnumerator InitiateAbility(Ability ability)
		{
			GetComponent<AbilityHandler>().UseAbility(ability.abilityType);
			GetComponent<AbilityHandler>().StartIconCooldown(ability.name, ability.AbilityCooldown);

			ability.OnCooldown = true;
			yield return new WaitForSeconds(ability.AbilityCooldown);
			ability.OnCooldown = false;
		}
		#endregion

		// puzzle checker
		if (Timer.Instance.IsSpiritualPowerFull())
			EscapeGate.IsUsable = true;

		if (transform.position.y < -10f)
			transform.SetPositionAndRotation(InitialPosition.Item1, InitialPosition.Item2);
		// Die();

		// check HP
		if (currentHealth <= 0 && CanMove)
		{
			if (IsHunter)
			{
				// stunned while health is not full
				if (canKillHunter)
					Die();

				CanMove = false;
			}
			else
			{
				CanMove = false;
			}
		}
	}

	void FixedUpdate()
	{
		if (!photonViewPlayer.IsMine) return;

		if (!CanMove || lockMovement) return;

		MovePlayer();
	}

	public void TakeDamage(float damage)
	{
		if (IsHunter) // && canKillHunter
		{
			Debug.LogError("Shooting hunter!");
			photonViewPlayer.RPC(nameof(RPC_TakeDamage), photonViewPlayer.Owner, damage);
		}
		else if (!IsHunter)
			photonViewPlayer.RPC(nameof(RPC_TakeDamage), photonViewPlayer.Owner, damage);
	}

	public void AddMagazine(int _ammo)
	{
		if (IsHunter) return;

		((GunInfo)items[0].itemInfo).currentMagSize = _ammo;
	}

	void GunButton(bool isAutomatic = false)
	{
		var input = Input.GetMouseButtonDown(0);

		if (IsProp) return;

		if (isAutomatic)
			input = Input.GetMouseButton(0);

		if (input)
			items[itemIndex].Use();
	}

	void Look()
	{
		transform.Rotate(Input.GetAxisRaw("Mouse X") * mouseSensitivity * Vector3.up);

		verticalLookRotation += Input.GetAxisRaw("Mouse Y") * mouseSensitivity;
		verticalLookRotation = Mathf.Clamp(verticalLookRotation, -90f, 90f);

		cameraHolder.transform.localEulerAngles = Vector3.left * verticalLookRotation;
	}

	void Move()
	{
		// float horizontal = Input.GetAxisRaw("Horizontal");
		// float vertical = Input.GetAxisRaw("Vertical");
		// CheckAnimationMovement = new Vector3(horizontal, 0, vertical);
		// moveAmount = CheckAnimationMovement * (Input.GetKey(KeyCode.LeftShift) ? sprintSpeed + MoveSpeedBurst : walkSpeed + MoveSpeedBurst);

		if (moveDirection.x != 0 || moveDirection.z != 0)
		{
			if (_footstepSFXCooldown <= 0)
			{
				playerNetworkSoundManager.PlayFootstepSFX();
				_footstepSFXCooldown = .8f;
			}
			else
				_footstepSFXCooldown -= Time.deltaTime;
		}
	}

	void MovePlayer()
	{
		if (!photonViewPlayer.IsMine) return;

		moveDirection = orientation.forward * Input.GetAxisRaw("Vertical") + orientation.right * Input.GetAxisRaw("Horizontal");

		if (grounded)
			rb.AddForce(5f * currentSpeed * moveDirection.normalized, ForceMode.Force);
		else
		{
			rb.AddForce(5f * 1.2f * currentSpeed * moveDirection.normalized, ForceMode.Force);
			rb.AddForce(gravityValue * rb.mass * Physics.gravity, ForceMode.Acceleration);
		}

		rb.useGravity = !OnSlope();
	}

	void SpeedControl()
	{
		Vector3 flatVel = new(rb.velocity.x, 0f, rb.velocity.z);

		if (flatVel.magnitude > currentSpeed)
		{
			Vector3 limitedVel = flatVel.normalized * currentSpeed;
			rb.velocity = new(limitedVel.x, rb.velocity.y, limitedVel.z);
		}
	}

	private bool OnSlope()
	{
		if (IsHunter)
		{
			if (Physics.Raycast(transform.position, Vector3.down, out slopeHit, GetComponent<CapsuleCollider>().height * 1.3f))
			{
				float angle = Vector3.Angle(Vector3.up, slopeHit.normal);
				return angle < 65 && angle != 0;
			}
		}
		else
		{
			if (Physics.Raycast(transform.position, Vector3.down, out slopeHit, GetComponentInChildren<CapsuleCollider>().height * 1.3f))
			{
				float angle = Vector3.Angle(Vector3.up, slopeHit.normal);
				return angle < 65 && angle != 0;
			}
		}
		return false;
	}

	private Vector3 GetSlopeMoveDirection() => Vector3.ProjectOnPlane(moveDirection, slopeHit.normal).normalized;

	void Jump()
	{
		if (IsHunter) return;

		if (Input.GetKeyDown(KeyCode.Space) && grounded && !isJumpingOnCooldown)
		{
			StartCoroutine(JumpCooldown());
			rb.AddForce(transform.up * jumpForce);
		}
	}

	IEnumerator JumpCooldown()
	{
		isJumpingOnCooldown = true;
		yield return new WaitForSeconds(1.25f);
		isJumpingOnCooldown = false;
	}

	void EquipItem(int _index)
	{
		if (_index == previousItemIndex)
			return;

		itemIndex = _index;

		items[itemIndex].itemGameObject.SetActive(true);

		if (previousItemIndex != -1)
		{
			items[previousItemIndex].itemGameObject.SetActive(false);
		}

		previousItemIndex = itemIndex;

		if (photonViewPlayer.IsMine)
		{
			if (gameObject.activeSelf)
			{
				Hashtable hash = new()
				{
					{ "itemIndex", itemIndex }
				};
				PhotonNetwork.LocalPlayer.SetCustomProperties(hash);
			}
		}
	}

	public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
	{
		if (changedProps.ContainsKey("itemIndex") && !photonViewPlayer.IsMine && targetPlayer == photonViewPlayer.Owner)
		{
			EquipItem((int)changedProps["itemIndex"]);
		}
	}

	[PunRPC]
	void RPC_TakeDamage(float damage, PhotonMessageInfo info)
	{
		if (currentHealth <= 0)
		{
			currentHealth = 0;
			return;
		}

		currentHealth -= damage;

		healthbarImage.fillAmount = currentHealth / maxHealth;

		UpdatePlayerProperties_HP();
	}

	void Die() => playerManager.Die();

	public void HealPlayer(float amount) => photonViewPlayer.RPC(nameof(RPC_Heal), photonViewPlayer.Owner, amount);

	[PunRPC]
	void RPC_Heal(float amount)
	{
		currentHealth = Mathf.Min(currentHealth + amount, maxHealth);

		healthbarImage.fillAmount = currentHealth / maxHealth;

		UpdatePlayerProperties_HP();
	}

	public void UpdatePlayerProperties_HP()
	{
		var hash = new Hashtable
		{
			{ "currentHealth", currentHealth },
			{ "maxHealth", maxHealth }
		};
		PhotonNetwork.LocalPlayer.SetCustomProperties(hash);

		Debug.Log($"Player HP properties set!");
	}

	#region Stunned

	IEnumerator UpdateStunnedTimer(float duration)
	{
		isStunned = true;
		stunTimerText.gameObject.SetActive(true);
		ShowPlayerOutline();
		StunDuration = duration;

		while (duration > 0)
		{
			duration -= Time.deltaTime;
			stunTimerText.SetText($"STUNNED FOR {Mathf.Round(duration)}");
			yield return null;
		}

		isStunned = false;
		CanMove = true;
		stunTimerText.gameObject.SetActive(false);
		HidePlayerOutline();
	}

	public void ShowPlayerOutline() => photonViewPlayer.RPC(nameof(ShowOutline_RPC), RpcTarget.All);
	[PunRPC]
	void ShowOutline_RPC()
	{
		if (IsHunter)
			gameObject.GetComponent<Outline>().enabled = true;
		else
			_MapProps[_currentPropIndex].GetComponentInChildren<Outline>().enabled = true;
	}

	public void HidePlayerOutline() => photonViewPlayer.RPC(nameof(HideOutline_RPC), RpcTarget.All);
	[PunRPC]
	void HideOutline_RPC()
	{
		if (IsHunter)
			gameObject.GetComponent<Outline>().enabled = false;
		else
			_MapProps[_currentPropIndex].GetComponentInChildren<Outline>().enabled = false;
	}
	#endregion

	#region Props

	[PunRPC]
	void RPC_SwapModel()
	{
		if (!photonViewPlayer.IsMine) return;
		if (isSwappingModel) return;

		isSwappingModel = true;
		photonViewPlayer.RPC(nameof(SwapModelCoroutine), RpcTarget.All);
	}

	[PunRPC]
	IEnumerator SwapModelCoroutine()
	{
		IsProp = true;

		foreach (var prop in _MapProps)
			prop.SetActive(false);

		_currentPropIndex = (_currentPropIndex + 1) % _MapProps.Length;
		_MapProps[_currentPropIndex].SetActive(true);

		yield return new WaitForSeconds(1f); // TODO: SWAP COOLDOWN
		isSwappingModel = false;
	}

	public void SetPropList(int mapNumber) => photonViewPlayer.RPC(nameof(SetPropsList), RpcTarget.All, mapNumber);

	[PunRPC]
	public void SetPropsList(int mapNumber)
	{
		switch (mapNumber)
		{
			case 1:
				_MapProps = props;
				break;
			case 2:
				_MapProps = Map2_props;
				break;
		}
	}

	[PunRPC]
	void SetDefaultProp()
	{
		_currentPropIndex = _MapProps.Length - 1;
		_MapProps[_currentPropIndex].SetActive(true);
	}

	public GameObject GetCurrentProp() => _MapProps[_currentPropIndex];
	#endregion

	#region Abilities

	public void SetAbility(Ability[] _ability)
	{
		for (int i = 0; i < _ability.Length; i++)
		{
			_abilities[i] = _ability[i];
			Debug.Log($"Added ability: {_abilities[i].name}");
		}
	}

	public void StartAuraOfFear() => photonViewPlayer.RPC(nameof(AuraOfFear), RpcTarget.All);

	[PunRPC]
	IEnumerator AuraOfFear()
	{
		Debug.Log($"Starting aura of fear!");
		_propsDefaultSpeed = MoveSpeedBurst;
		MoveSpeedBurst = -6f;
		IsAuraActive = true;
		ShowPlayerOutline();
		yield return new WaitForSeconds(6f);
		MoveSpeedBurst = _propsDefaultSpeed;
		IsAuraActive = false;
		HidePlayerOutline();
	}
	#endregion

	public void PlaySFX(int index, Vector3 followTarget, int maxAudioDistance = 35) => playerNetworkSoundManager.PlayOtherSFX(index, followTarget, maxAudioDistance);

	#region Puzzle

	public void AddOrbToInventory(GameObject spiritOrb)
	{
		if (IsHunter) return;

		Timer.Instance.AddCollectedSpiritOrb(spiritOrb);
		// Timer.Instance.RPC_UpdateSpiritualPowerProgress();
	}

	public void HuntHunter() => photonViewPlayer.RPC(nameof(HuntIsOn), RpcTarget.All);

	[PunRPC]
	void HuntIsOn()
	{
		canKillHunter = true;
		// huntIcon.SetActive(true);

		((GunInfo)items[0].itemInfo).currentMagSize = 150;

		if (IsHunter)
		{
			((GunInfo)items[0].itemInfo).currentMagSize = 0;
			((GunInfo)items[0].itemInfo).currentAmmo = 0;
			Debug.Log($"Hunter Gun Disabled! THE HUNT IS ON!");
		}
	}
	#endregion

	GameObject overlay;
	private void OnTriggerStay(Collider other)
	{
		if (!photonViewPlayer.IsMine) return;
		if (other.CompareTag("Props") && !IsHunter)
		{
			// if (!CanMove && currentHealth <= 100f)
			// 	SaveDownedPlayer();
			if (currentHealth <= 0)
			{
				rescuerProps = other.gameObject;
				rescueOverlay.SetActive(true);
				rescueProgressBar.fillAmount += Time.time / rescueTime;
			}

			if (rescueProgressBar.fillAmount >= 1f)
				SaveDownedPlayer();
		}
	}

	private void OnTriggerExit(Collider other)
	{
		if (!photonViewPlayer.IsMine) return;
		if (rescuerProps != null)
		{
			rescueProgressBar.fillAmount = 0f;
			rescueOverlay.SetActive(false);

			rescuerProps = null;
		}
	}

	void SaveDownedPlayer()
	{
		rescueProgressBar.fillAmount = 0f;
		CanMove = true;
		HealPlayer(100f);
		UpdatePlayerProperties_HP();
		HidePlayerOutline();
	}

	public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
	{
		if (stream.IsWriting)
		{
			stream.SendNext(moveDirection);
			stream.SendNext(_currentPropIndex);
		}
		else
		{
			moveDirection = (Vector3)stream.ReceiveNext();
			_currentPropIndex = (int)stream.ReceiveNext();
		}
	}
}