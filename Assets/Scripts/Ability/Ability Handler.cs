using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Photon.Pun;
using UnityEngine;

public class AbilityHandler : MonoBehaviourPunCallbacks
{
    [SerializeField] Ability[] abilities;
    public AbilityType abilityType;
    Outline _outline;
    GameObject[] props;
    GameObject[] hunters;
    PhotonView _photonview;
    PlayerController _playerController;

    public enum AbilityType
    {
        Illusion_Clone,
        Environmental_Camouflage,
        Echo_Location,
        Teleportation_Dash,
        Trap_Setting,
        Sound_Mimicry,
        Camouflage_Shift,
        Panic_Button,
        Whispering_Winds
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

            props = GameObject.FindGameObjectsWithTag("Props");
            hunters = GameObject.FindGameObjectsWithTag("Hunter");

            ToggleOutline(props, Color.blue);
            ToggleOutline(hunters, Color.red);

            // pick three random abilities from the array
            Ability[] randomAbilities = abilities.OrderBy(a => Random.value).Take(3).ToArray();

            // assign the abilityType to the first ability in the array
            abilityType = randomAbilities[0].abilityType;

            foreach (var item in randomAbilities)
            {
                Debug.Log($"item: {item.abilityType}");
            }

            _playerController.SetAbility(randomAbilities);
        }
    }

    private void Update()
    {
        if (_photonview.IsMine)
        {

        }
    }

    void ToggleOutline(GameObject[] gameObjectsArr, Color color, bool condition = false)
    {
        for (int i = 0; i < gameObjectsArr.Length; i++)
        {
            gameObjectsArr[i].GetComponentInParent<Outline>().OutlineColor = color;
            gameObjectsArr[i].GetComponentInParent<Outline>().enabled = condition;
            Debug.Log($"Disabled outline for {gameObjectsArr[i]}");
        }
    }

    public void UseAbility(AbilityType abilityType)
    {
        if (_photonview.IsMine)

            if (GetComponent<PlayerController>().IsHunter) return;

        switch (abilityType)
        {
            case AbilityType.Illusion_Clone:
                Debug.Log($"Using Illusion Clone");
                var spawnedClone = PhotonNetwork.Instantiate(Path.Combine("PhotonPrefabs/Clone", "Player Clone"), transform.position, transform.rotation);
                break;

            case AbilityType.Environmental_Camouflage:
                Debug.Log($"Using Environmental Camouflage");
                break;

            case AbilityType.Echo_Location:
                Debug.Log($"Using Echo Location");
                StartCoroutine(ShowOutlineWithDelay());
                break;

            case AbilityType.Teleportation_Dash:
                Debug.Log($"Using Teleportation Dash");

                RaycastHit hit;
                if (Physics.Raycast(transform.position, transform.forward, out hit, 3f))
                {
                    if (hit.distance < 3f)
                    {
                        Debug.Log("Too close to a wall");
                        return;
                    }
                }

                GetComponent<Rigidbody>().MovePosition(transform.position + (transform.forward * 3f));
                break;

            case AbilityType.Trap_Setting:
                //TODO: ADD MORE TRAPS!
                Debug.Log($"Using Trap Setting");
                Vector3 spawnDirection = transform.TransformDirection(Vector3.forward);

                var spawnedTrap = PhotonNetwork.Instantiate(Path.Combine("PhotonPrefabs\\Traps", "Bear Trap"), GetGroundPosition(_playerController.GetCurrentProp().transform.position + (spawnDirection * 2f)), transform.rotation);
                MoveForward(2f, spawnedTrap.transform);
                break;

            case AbilityType.Sound_Mimicry:
                Debug.Log($"Using Sound Mimicry");
                PlayerController.playerNetworkSoundManager.PlayWhistleSFX();
                break;

            case AbilityType.Camouflage_Shift:
                Debug.Log($"Using Camouflage Shift");
                break;

            case AbilityType.Panic_Button:
                Debug.Log($"Using Panic Button");
                GetComponent<PlayerController>().MoveSpeedBurst = 6f;
                StartCoroutine(ResetMoveSpeedAfterDelay());
                break;

            case AbilityType.Whispering_Winds:
                Debug.Log($"Using Whispering Winds");
                break;
        }

        IEnumerator ResetMoveSpeedAfterDelay()
        {
            yield return new WaitForSeconds(1.2f);
            GetComponent<PlayerController>().MoveSpeedBurst = 0f;
        }

        IEnumerator ShowOutlineWithDelay()
        {
            ToggleOutline(props, Color.blue, true);
            ToggleOutline(hunters, Color.red, true);

            yield return new WaitForSeconds(3f);

            ToggleOutline(props, Color.blue);
            ToggleOutline(hunters, Color.red);
        }
    }

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

}
