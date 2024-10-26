using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Photon.Pun;
using Unity.Burst.Intrinsics;
using UnityEngine;
using UnityEngine.UI;

public class AbilityHandler : MonoBehaviourPun
{
    public GameObject hunterModel;
    [SerializeField] List<Image> skillImages;
    [SerializeField] List<Image> skillImagesCooldown;
    [SerializeField] Ability[] abilities;
    [SerializeField] List<Image> hunterSkillImages;
    [SerializeField] List<Image> hunterSkillImagesCooldown;
    [SerializeField] Ability[] hunterAbilities;
    public AbilityType abilityType;
    Outline _outline;
    List<Collider> props;
    GameObject[] hunters;
    PhotonView _photonview;
    PlayerController _playerController;
    bool _isFilling;
    GameObject spawnedParticle;
    GameObject spawnedParticleSingleShot;
    GameObject spawnedTrap;
    RaycastHit hit;
    private float _propsDefaultSpeed;
    private Collider[] colliders;

    public int MaxTrap { get; set; } = 3;

    public enum AbilityType
    {
        Teleportation_Dash,
        Trap_Setting,
        Sound_Mimicry,
        Panic_Button,

        //hunter abilities
        Aura_of_Fear,
        Invisibility_Cloak,
        Temporal_Vision
    }

    private void Awake()
    {
        _photonview = GetComponent<PhotonView>();
    }

    private void Start()
    {
        if (_photonview.IsMine)
        {
            _playerController = GetComponent<PlayerController>();

            StartCoroutine(DelayedCall());

            if (GetComponent<PlayerController>().IsHunter)
            {
                for (int i = 0; i < hunterAbilities.Length; i++)
                {
                    hunterSkillImages[i].sprite = hunterAbilities[i].abilityImage;
                    hunterSkillImagesCooldown[i].sprite = hunterAbilities[i].abilityImage;
                }
                _playerController.SetAbility(hunterAbilities);

                hunterSkillImagesCooldown.ForEach(x => x.gameObject.SetActive(false));

                for (int i = 0; i < hunterAbilities.Length; i++)
                    hunterAbilities[i].OnCooldown = false;

                hunterModel = GameObject.Find("Hunter Model");
            }
            else
            {
                for (int i = 0; i < abilities.Length; i++)
                {
                    skillImages[i].sprite = abilities[i].abilityImage;
                    skillImagesCooldown[i].sprite = abilities[i].abilityImage;
                }
                _playerController.SetAbility(abilities);

                skillImagesCooldown.ForEach(x => x.gameObject.SetActive(false));

                for (int i = 0; i < abilities.Length; i++)
                    abilities[i].OnCooldown = false;
            }


            // if (PhotonNetwork.PlayerList.Length > 1)
            //     hunterModel = GameObject.Find("Hunter Model");
            // else
            //     Debug.Log($"Player count is too low! {PhotonNetwork.PlayerList.Length}");
        }

        IEnumerator DelayedCall()
        {
            yield return new WaitForSeconds(2f);
            ToggleOutline(true, Color.cyan);
            ToggleOutline(false, Color.red);
        }
    }

    private void Update()
    {
        if (photonView.IsMine)
        {
            if (spawnedParticle != null)
                spawnedParticle.transform.position = transform.position;

            colliders = new Collider[5];
            int count = Physics.OverlapSphereNonAlloc(transform.position, 7f, colliders);
            props = new List<Collider>(count);
            for (int i = 0; i < count; i++)
            {
                if (colliders[i].CompareTag("Props"))
                {
                    if (!props.Contains(colliders[i]))
                        props.Add(colliders[i]);
                }
                else
                    Debug.Log($"Not props! {colliders[i].gameObject.name}");
            }
        }
    }

    void ToggleOutline(bool isProps, Color color, bool condition = false)
    {
        GameObject[] arrayToUse;

        if (isProps)
            arrayToUse = GameObject.FindGameObjectsWithTag("Props");
        else
            arrayToUse = GameObject.FindGameObjectsWithTag("Hunter");

        if (arrayToUse != null)
        {
            for (int i = 0; i < arrayToUse.Length; i++)
            {
                arrayToUse[i].GetComponentInChildren<Outline>().OutlineColor = color;
                arrayToUse[i].GetComponentInChildren<Outline>().OutlineWidth = 8;

                if (condition)
                    arrayToUse[i].GetComponentInParent<PlayerController>().ShowPlayerOutline();
                else
                    arrayToUse[i].GetComponentInParent<PlayerController>().HidePlayerOutline();
            }
        }
        else
            Debug.LogError($"No gameobjects in the array of outline!");
    }

    #region abilities
    public void UseAbility(AbilityType abilityType)
    {
        if (_photonview.IsMine)

            switch (abilityType)
            {
                case AbilityType.Teleportation_Dash:
                    Debug.Log($"Using Teleportation Dash");

                    PlayerController.playerNetworkSoundManager.PlayOtherSFX(2, Vector3.zero);
                    SpawnParticle("Flash", spawnedParticleSingleShot);

                    if (Physics.Raycast(transform.position, transform.forward, out hit, 6f))
                    {
                        if (hit.distance < 6f)
                        {
                            GetComponent<Rigidbody>().MovePosition(hit.point);
                            return;
                        }
                    }

                    GetComponent<Rigidbody>().MovePosition(transform.position + (transform.forward * 6f));
                    break;

                case AbilityType.Trap_Setting:
                    Debug.Log($"Using Trap Setting");

                    if (MaxTrap <= 0) return;
                    MaxTrap--;
                    Vector3 spawnDirection = transform.TransformDirection(Vector3.forward);
                    PlayerController.playerNetworkSoundManager.PlayOtherSFX(3, Vector3.zero);

                    if (Physics.Raycast(transform.position, transform.forward, out hit, 6f))
                    {
                        if (hit.distance < 6f)
                        {
                            spawnedTrap = PhotonNetwork.Instantiate(Path.Combine("PhotonPrefabs\\Traps", "Bear Trap"), hit.point, transform.rotation);
                            MoveForward(0.5f, spawnedTrap.transform);
                            return;
                        }
                    }

                    spawnedTrap = PhotonNetwork.Instantiate(Path.Combine("PhotonPrefabs\\Traps", "Bear Trap"), transform.position + (transform.forward * 6f), transform.rotation);
                    MoveForward(0.5f, spawnedTrap.transform);
                    break;

                case AbilityType.Sound_Mimicry:
                    Debug.Log($"Using Sound Mimicry");
                    PlayerController.playerNetworkSoundManager.PlayWhistleSFX();
                    break;

                case AbilityType.Panic_Button:
                    Debug.Log($"Using Panic Button");
                    GetComponent<PlayerController>().MoveSpeedBurst = 6f;
                    PlayerController.playerNetworkSoundManager.PlayOtherSFX(4, Vector3.zero);
                    StartCoroutine(ResetMoveSpeedAfterDelay());
                    break;

                // hunter abilities

                case AbilityType.Aura_of_Fear:
                    Debug.Log($"Using Aura of Fear");

                    foreach (var _props in props)
                    {
                        Debug.LogError($"_props caught in aura of fear: {_props.gameObject.name}");
                        _props.gameObject.GetComponentInParent<PlayerController>().StartAuraOfFear();
                        props?.Clear();
                    }

                    SpawnParticle("Aura of Fear", spawnedParticle);
                    break;

                case AbilityType.Invisibility_Cloak:
                    Debug.Log($"Using Invisibility Cloak");
                    _photonview.RPC(nameof(InvisibilityCloak_RPC), RpcTarget.All);
                    SpawnParticle("Invisibility", spawnedParticle);
                    break;

                case AbilityType.Temporal_Vision:
                    Debug.Log($"Using Temporal Vision");
                    StartCoroutine(ShowOutlineWithDelay());
                    break;
            }

        void SpawnParticle(string particleName, GameObject gameObjectReference)
        {
            gameObjectReference = PhotonNetwork.Instantiate($"Particles\\{particleName}", transform.position, transform.rotation);
            StartCoroutine(DestroyParticleAfterDelay(gameObjectReference));
        }

        IEnumerator DestroyParticleAfterDelay(GameObject _particle)
        {
            if (_particle != null)
            {
                var particle = _particle.GetComponent<ParticleSystem>();
                yield return new WaitForSeconds(particle.main.duration);
                PhotonNetwork.Destroy(_particle);
            }
        }

        IEnumerator ResetMoveSpeedAfterDelay()
        {
            yield return new WaitForSeconds(1.2f);
            GetComponent<PlayerController>().MoveSpeedBurst = 0f;
        }

        IEnumerator ShowOutlineWithDelay()
        {
            ToggleOutline(true, Color.cyan, true);
            yield return new WaitForSeconds(3f);
            ToggleOutline(true, Color.cyan, false);
        }
    }

    [PunRPC]
    IEnumerator InvisibilityCloak_RPC()
    {
        SetHunterModel_disabled();
        yield return new WaitForSeconds(5f);
        SetHunterModel_enabled();
    }

    void SetHunterModel_disabled()
    {
        hunterModel = GameObject.Find("Hunter Model");
        hunterModel.SetActive(false);
    }

    void SetHunterModel_enabled()
    {
        hunterModel.SetActive(true);
    }

    #endregion
    #region End Abilities
    #endregion

    Vector3 GetGroundPosition(Vector3 position)
    {
        if (Physics.Raycast(position, Vector3.down, out var hit, Mathf.Infinity, LayerMask.GetMask("Ground")))
            return hit.point;

        return position;
    }

    float MoveForward(float distance, Transform _transform)
    {
        Vector3 direction = transform.TransformDirection(Vector3.forward);
        Vector3 rayOrigin = _transform.position + (direction * -0.5f);

        if (Physics.Raycast(rayOrigin, direction, out var hit, distance + 0.5f))
        {
            if (hit.distance < distance)
                _transform.position = hit.point - (direction * 0.5f);
            else
                _transform.position += direction * distance;

            return hit.distance;
        }
        else
        {
            _transform.position += direction * distance;
            return distance;
        }
    }

    #region Cooldown

    public void StartIconCooldown(string iconName, float fillAmount)
    {
        switch (iconName)
        {
            case "Panic Button":
                FillIcon(skillImagesCooldown[0], fillAmount);
                break;

            case "Sound Mimicry":
                FillIcon(skillImagesCooldown[1], fillAmount);
                break;

            case "Teleportation Dash":
                if (GetComponent<PlayerController>().IsHunter)
                    FillIcon(hunterSkillImagesCooldown[3], fillAmount);
                else
                    FillIcon(skillImagesCooldown[2], fillAmount);

                break;

            case "Trap Setting":
                FillIcon(skillImagesCooldown[3], fillAmount);
                break;

            // HUNTER SKILLS
            case "Temporal Vision":
                FillIcon(hunterSkillImagesCooldown[0], fillAmount);
                break;

            case "Aura of Fear":
                FillIcon(hunterSkillImagesCooldown[1], fillAmount);
                break;

            case "Invisibility Cloak":
                FillIcon(hunterSkillImagesCooldown[2], fillAmount);
                break;
        }
    }

    void FillIcon(Image icon, float fillAmount)
    {
        icon.gameObject.SetActive(true);
        icon.fillAmount = 1f;

        StartCoroutine(UpdateMeleeIconOverTime(icon, fillAmount));
    }


    IEnumerator UpdateMeleeIconOverTime(Image icon, float fillAmount)
    {
        _isFilling = true;
        float timeElapsed = 0;
        while (timeElapsed < fillAmount)
        {
            icon.fillAmount = 1f - (timeElapsed / fillAmount);
            timeElapsed += Time.deltaTime;
            yield return null;
        }
        _isFilling = false;
        icon.gameObject.SetActive(false);
    }

    #endregion
}
