using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.UI;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class HunterController : MonoBehaviourPunCallbacks, IDamageable
{
    [SerializeField] Image healthbarImage;
    [SerializeField] GameObject ui;
    [SerializeField] GameObject playerModel;

    [SerializeField] GameObject cameraHolder;

    [SerializeField] float mouseSensitivity, sprintSpeed, walkSpeed, jumpForce, smoothTime;

    [SerializeField] Item[] items;

    [SerializeField] Ability[] abilities;

    int itemIndex;
    int previousItemIndex = -1;

    float verticalLookRotation;
    bool grounded;
    Vector3 moveAmount;

    Rigidbody rb;

    public static PhotonView HunterPhotonView;

    const float maxHealth = 100f;
    float currentHealth = maxHealth;

    PlayerManager playerManager;
    AbilityHandler abilityHandler;
    Outline _outline;
    bool canMove = true;
    Animator _animator;
    string _currentAnimation;
    bool _canAttack = true;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        HunterPhotonView = GetComponent<PhotonView>();
        _animator = GetComponent<Animator>();

        playerManager = PhotonView.Find((int)HunterPhotonView.InstantiationData[0]).GetComponent<PlayerManager>();
        abilityHandler = GetComponent<AbilityHandler>();
        _outline = GetComponent<Outline>();
    }

    void Start()
    {
        if (HunterPhotonView.IsMine)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            _outline.enabled = false;

            // EquipItem(0);
            ChangeAnimation("Idle");
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
        if (!HunterPhotonView.IsMine) return;

        Look();
        Move();
        Jump();

        for (int i = 0; i < items.Length; i++)
        {
            if (Input.GetKeyDown((i + 1).ToString()))
            {
                EquipItem(i);
                break;
            }
        }

        if (Input.GetAxisRaw("Mouse ScrollWheel") > 0f)
        {
            if (itemIndex >= items.Length - 1)
            {
                EquipItem(0);
            }
            else
            {
                EquipItem(itemIndex + 1);
            }
        }
        else if (Input.GetAxisRaw("Mouse ScrollWheel") < 0f)
        {
            if (itemIndex <= 0)
            {
                EquipItem(items.Length - 1);
            }
            else
            {
                EquipItem(itemIndex - 1);
            }
        }

        // ability
        if (abilities.Length != 0)
        {
            if (Input.GetKeyDown(KeyCode.Q))
            {
                if (abilities[0].OnCooldown) return;
                StartCoroutine(InitiateAbility(abilities[0]));
            }
            if (Input.GetKeyDown(KeyCode.E))
            {
                if (abilities[1].OnCooldown) return;
                StartCoroutine(InitiateAbility(abilities[1]));
            }
            if (Input.GetKeyDown(KeyCode.V))
            {
                if (abilities[2].OnCooldown) return;
                StartCoroutine(InitiateAbility(abilities[2]));
            }
        }
        else
            Debug.Log($"No Abilities!");

        // attack
        if (Input.GetMouseButtonDown(0))
            StartCoroutine(AttackCooldown());

        IEnumerator AttackCooldown()
        {
            Debug.Log($"Hunter is attacking!~");
            _canAttack = false;
            ChangeAnimation("MeleeAttack_TwoHanded");
            rb.constraints = RigidbodyConstraints.FreezeAll;

            yield return new WaitForSeconds(_animator.GetCurrentAnimatorStateInfo(0).length + .8f);

            _canAttack = true;
            rb.constraints = RigidbodyConstraints.None;
            rb.constraints = RigidbodyConstraints.FreezeRotation;
        }

        if (transform.position.y < -10f) // Die if you fall out of the world
        {
            Die();
        }

        CheckAnimation();
    }

    void CheckAnimation()
    {
        if (_currentAnimation == "MeleeAttack_TwoHanded") return;

        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        if (vertical > 0)
            ChangeAnimation("RunForward");
        else if (vertical < 0)
            ChangeAnimation("RunBackward");
        else if (horizontal > 0)
            ChangeAnimation("RunRight");
        else if (horizontal < 0)
            ChangeAnimation("RunLeft");
        else
            ChangeAnimation("Idle");
    }

    public void ChangeAnimation(string animation, float time = 0)
    {
        if (time > 0)
            StartCoroutine(Wait());
        else
            Validate();

        IEnumerator Wait()
        {
            yield return new WaitForSeconds(time - .15f);
            Validate();
        }

        void Validate()
        {
            if (_currentAnimation != animation)
            {
                _currentAnimation = animation;

                if (_currentAnimation == "")
                    CheckAnimation();
                else
                    _animator.CrossFade(animation, .15f);
            }
        }
    }

    IEnumerator InitiateAbility(Ability ability)
    {
        ability.OnCooldown = true;
        abilityHandler.UseAbility(ability.abilityType);
        yield return new WaitForSeconds(ability.AbilityCooldown);
        ability.OnCooldown = false;
    }

    private new void OnDisable()
    {
        for (int i = 0; i < abilities.Length; i++)
            abilities[i].OnCooldown = false;
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

        // moveAmount = Vector3.SmoothDamp(moveAmount, moveDir * (Input.GetKey(KeyCode.LeftShift) ? sprintSpeed + MoveSpeedBurst : walkSpeed + MoveSpeedBurst), ref smoothMoveVelocity, smoothTime);
        moveAmount = moveDir * (Input.GetKey(KeyCode.LeftShift) ? sprintSpeed : walkSpeed);
    }

    void Jump()
    {
        if (Input.GetKeyDown(KeyCode.Space) && grounded)
        {
            rb.AddForce(transform.up * jumpForce);
        }
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

        if (HunterPhotonView.IsMine)
        {
            Hashtable hash = new Hashtable();
            hash.Add("itemIndex", itemIndex);
            PhotonNetwork.LocalPlayer.SetCustomProperties(hash);
        }
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        if (changedProps.ContainsKey("itemIndex") && !HunterPhotonView.IsMine && targetPlayer == HunterPhotonView.Owner)
        {
            EquipItem((int)changedProps["itemIndex"]);
        }
    }

    void FixedUpdate()
    {
        if (!HunterPhotonView.IsMine) return;

        rb.MovePosition(rb.position + transform.TransformDirection(moveAmount) * Time.fixedDeltaTime);
    }

    public void TakeDamage(float damage)
    {
        HunterPhotonView.RPC(nameof(RPC_TakeDamage), HunterPhotonView.Owner, damage);
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
}