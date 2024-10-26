using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;
using UnityEngine.UI;

public class RitualPole : MonoBehaviourPunCallbacks
{
    [SerializeField] Image progressBar;
    [SerializeField] float fillSpeed = 5f;
    public bool IsActivated { get; set; } = false;
    bool isActivating;
    List<GameObject> playerList = new();
    [SerializeField] List<GameObject> activateObjects;

    private void Start()
    {
        progressBar.fillAmount = 0f;
    }

    private void Update()
    {
        photonView.RPC(nameof(UpdateProgressBar), RpcTarget.All);
    }

    [PunRPC]
    void UpdateProgressBar()
    {
        if (!IsActivated && !isActivating)
        {
            progressBar.fillAmount -= Time.deltaTime / fillSpeed;
            if (playerList.Count > 0)
            {
                foreach (var players in playerList)
                {
                    players.GetComponentInParent<PlayerController>().HidePlayerOutline();
                }
                playerList.Clear();
            }
        }

        if (progressBar.fillAmount >= .01f && isActivating && !IsActivated)
        {
            foreach (var players in playerList)
            {
                players.GetComponentInParent<PlayerController>().ShowPlayerOutline();
            }
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.gameObject.CompareTag("Props"))
        {
            if (IsActivated) return;

            isActivating = true;

            progressBar.fillAmount += Time.deltaTime / fillSpeed;

            if (!playerList.Contains(other.gameObject))
            {
                playerList.Add(other.gameObject);
                Debug.Log($"Added {other.gameObject} to the list of players");
            }

            if (progressBar.fillAmount >= 1f)
            {
                photonView.RPC(nameof(ActivateGate), RpcTarget.All);

                other.gameObject.GetComponentInParent<PlayerController>().PlaySFX(6, transform.position, 1000);

                activateObjects?.ForEach(x => x.SetActive(true));
                Debug.Log($"This Gate is activated!");
            }
        }
    }

    [PunRPC]
    void ActivateGate()
    {
        PhotonNetwork.Instantiate($"Particles\\Shrine", transform.position, transform.rotation);
        IsActivated = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.CompareTag("Props"))
        {
            if (playerList.Contains(other.gameObject))
            {
                Debug.Log($"Removed {other.gameObject}, not activating gate");
                other.GetComponentInParent<PlayerController>().HidePlayerOutline();
                playerList.Remove(other.gameObject);
            }

            if (playerList.Count <= 0)
                isActivating = false;
        }
    }
}
