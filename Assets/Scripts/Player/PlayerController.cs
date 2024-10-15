using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class PlayerController : MonoBehaviourPunCallbacks, IDamageable, IPunObservable
{
	[SerializeField] Image healthbarImage;
	[SerializeField] GameObject ui;

	[SerializeField] GameObject cameraHolder;

	[SerializeField] float mouseSensitivity, sprintSpeed, walkSpeed, jumpForce, smoothTime;

	[SerializeField] Item[] items;

	[Header("Props")]
	[SerializeField] GameObject[] props;
	bool isSwappingModel = false;
	int _currentPropIndex;
	bool IsProp;

	[Header("Configs")]
	public bool IsHunter;
	[SerializeField] float gravityValue = 2f;
	[SerializeField] Transform orientation;
	[SerializeField] LayerMask GroundLayer;
	[SerializeField] SkinnedMeshRenderer _ghostModel;
	[SerializeField] CapsuleCollider _ghostModelCollider;
	float _footstepSFXCooldown = 0f;
	float currentSpeed;
	Vector3 moveDirection;
	RaycastHit slopeHit;

	[Header("HUD")]
	[SerializeField] TMP_Text ammoText;
	[SerializeField] TMP_Text q_skillText;
	[SerializeField] TMP_Text e_skillText;
	[SerializeField] TMP_Text v_skillText;

	[Header("Abilities")]
	[SerializeField] float yeh = 0f;
	Ability[] _abilities = new Ability[3];
	public float MoveSpeedBurst { get; set; }

	int itemIndex;
	int previousItemIndex = -1;

	float verticalLookRotation;
	bool grounded;
	bool isJumpingOnCooldown;

	Vector3 smoothMoveVelocity;
	Vector3 moveAmount;
	Rigidbody rb;

	[HideInInspector] public PhotonView photonViewPlayer;

	public const float maxHealth = 100f;
	public float currentHealth = maxHealth;

	PlayerManager playerManager;

	// hunter vars
	string _currentAnimation;
	GunInfo gunInfo;

	// IPunobservable vars
	Vector3 CheckAnimationMovement;

	public static PlayerNetworkSoundManager playerNetworkSoundManager;
	int deaths = 0;
	bool canRespawn = true;

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
			Cursor.lockState = CursorLockMode.Locked;
			Cursor.visible = false;
			EquipItem(0);
		}
		else
		{
			Destroy(GetComponentInChildren<Camera>().gameObject);
			Destroy(rb);
			Destroy(ui);
		}

		if (!IsHunter) return;
	}

	void Update()
	{
		// moveAmount = CheckAnimationMovement * (Input.GetKey(KeyCode.LeftShift) ? sprintSpeed + MoveSpeedBurst : walkSpeed + MoveSpeedBurst);
		currentSpeed = Input.GetKey(KeyCode.LeftShift) ? sprintSpeed + MoveSpeedBurst : walkSpeed + MoveSpeedBurst;

		if (!photonViewPlayer.IsMine)
			return;

		if (!IsHunter)
		{
			var objectHeight = props[_currentPropIndex].GetComponent<CapsuleCollider>().height / 2 + .5f;
			grounded = Physics.Raycast(props[_currentPropIndex].transform.position, Vector3.down, props[_currentPropIndex].GetComponentInChildren<Renderer>().bounds.size.y * .7f, GroundLayer);
		}

		Look();
		// Move();
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

		#region  Item
		gunInfo = (GunInfo)items[itemIndex].itemInfo;

		if (gunInfo != null)
		{
			ammoText.SetText($"AMMO: {gunInfo.currentAmmo} / {gunInfo.currentMagSize}");

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
		{
			items[itemIndex].Use();
		}

		if (Input.GetKeyDown(KeyCode.R))
		{
			items[itemIndex].GetComponent<SingleShotGun>().ReloadGun();
		}
		#endregion

		#region Abilities
		// ability
		if (!IsHunter)
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
			if (Input.GetKeyDown(KeyCode.V))
			{
				if (_abilities[2].OnCooldown) return;
				StartCoroutine(InitiateAbility(_abilities[2]));
			}
		}

		IEnumerator InitiateAbility(Ability ability)
		{
			ability.OnCooldown = true;
			GetComponent<AbilityHandler>().UseAbility(ability.abilityType);
			yield return new WaitForSeconds(ability.AbilityCooldown);
			ability.OnCooldown = false;
		}
		#endregion

		// swap props
		if (Input.GetKeyDown(KeyCode.G))
		{
			if (IsHunter) return;

			photonViewPlayer.RPC(nameof(RPC_SwapModel), RpcTarget.All);
		}

		if (transform.position.y < -10f) // Die if you fall out of the world
		{
			Die();
		}
	}

	void FixedUpdate()
	{
		if (!photonViewPlayer.IsMine) return;

		MovePlayer();
		// rb.MovePosition(rb.position + transform.TransformDirection(moveAmount) * Time.fixedDeltaTime);
	}

	public void TakeDamage(float damage)
	{
		photonViewPlayer.RPC(nameof(RPC_TakeDamage), photonViewPlayer.Owner, damage);
	}

	public void AddMagazine(int _ammo)
	{
		gunInfo.currentMagSize = _ammo;
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
		Vector3 moveDir = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical")).normalized;

		float horizontal = Input.GetAxisRaw("Horizontal");
		float vertical = Input.GetAxisRaw("Vertical");
		CheckAnimationMovement = new Vector3(horizontal, 0, vertical);

		// moveAmount = Vector3.SmoothDamp(moveAmount, moveDir * (Input.GetKey(KeyCode.LeftShift) ? sprintSpeed : walkSpeed), ref smoothMoveVelocity, smoothTime);
		moveAmount = CheckAnimationMovement * (Input.GetKey(KeyCode.LeftShift) ? sprintSpeed + MoveSpeedBurst : walkSpeed + MoveSpeedBurst);

		if (moveAmount.x != 0 || moveAmount.z != 0)
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
		if (photonView.IsMine)
		{
			moveDirection = orientation.forward * Input.GetAxisRaw("Vertical") + orientation.right * Input.GetAxisRaw("Horizontal");

			if (grounded)
			{
				Debug.Log($"CAN MOVE!");
				rb.AddForce(5f * currentSpeed * moveDirection.normalized, ForceMode.Force);
			}
			else
			{
				rb.AddForce(5f * 1.2f * currentSpeed * moveDirection.normalized, ForceMode.Force);
				rb.AddForce(gravityValue * rb.mass * Physics.gravity, ForceMode.Acceleration);
			}

			// turn gravity off while on slope
			rb.useGravity = !OnSlope();
		}
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
		if (Physics.Raycast(transform.position, Vector3.down, out slopeHit, props[_currentPropIndex].GetComponentInChildren<Renderer>().bounds.size.y * .7f))
		{
			float angle = Vector3.Angle(Vector3.up, slopeHit.normal);
			return angle < 55 && angle != 0;
		}
		return false;
	}

	private Vector3 GetSlopeMoveDirection()
	{
		return Vector3.ProjectOnPlane(moveDirection, slopeHit.normal).normalized;
	}

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
		yield return new WaitForSeconds(2f);
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
		currentHealth -= damage;

		healthbarImage.fillAmount = currentHealth / maxHealth;

		if (currentHealth <= 0)
		{
			Die();
			PlayerManager.Find(info.Sender).GetKill();
		}
	}

	void Die()
	{
		playerManager.Die();
	}

	public void HealPlayer(float amount)
	{
		photonViewPlayer.RPC(nameof(RPC_Heal), photonViewPlayer.Owner, amount);
	}

	[PunRPC]
	void RPC_Heal(float amount)
	{
		currentHealth = Mathf.Min(currentHealth + amount, maxHealth);

		healthbarImage.fillAmount = currentHealth / maxHealth;
	}

	#region Props

	[PunRPC]
	void RPC_SwapModel()
	{
		if (isSwappingModel) return;

		isSwappingModel = true;
		StartCoroutine(SwapModelCoroutine());
	}

	IEnumerator SwapModelCoroutine()
	{
		if (_currentPropIndex == 0)
		{
			_ghostModel.enabled = false;
			_ghostModelCollider.enabled = false;
		}

		IsProp = true;

		foreach (var prop in props)
			prop.SetActive(false);

		_currentPropIndex = (_currentPropIndex + 1) % props.Length;
		props[_currentPropIndex].SetActive(true);

		yield return new WaitForSeconds(1f); // TODO: SWAP COOLDOWN
		isSwappingModel = false;
	}

	public GameObject GetCurrentProp() => props[_currentPropIndex];
	#endregion

	#region Abilities

	public void SetAbility(Ability[] _ability)
	{
		if (IsHunter) return;

		for (int i = 0; i < _ability.Length; i++)
		{
			_abilities[i] = _ability[i];
			Debug.Log($"Added ability: {_abilities[i].name}");
		}
		q_skillText.SetText($"Q: {_abilities[0].name}");
		e_skillText.SetText($"E: {_abilities[1].name}");
		v_skillText.SetText($"V: {_abilities[2].name}");
	}
	#endregion

	public void PlaySFX(int index) => playerNetworkSoundManager.PlayOtherSFX(index);

	public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
	{
		if (stream.IsWriting)
		{
			stream.SendNext(CheckAnimationMovement);
		}
		else
		{
			CheckAnimationMovement = (Vector3)stream.ReceiveNext();
		}
	}
}