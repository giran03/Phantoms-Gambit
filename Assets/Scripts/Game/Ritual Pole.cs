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
        if (!photonView.IsMine) return;

        if (other.gameObject.CompareTag("Props"))
        {
            if (IsActivated) return;

            isActivating = true;
            other.GetComponentInParent<PlayerController>().ShowPlayerOutline();

            progressBar.fillAmount += Time.deltaTime / fillSpeed;

            if (!playerList.Contains(other.gameObject))
            {
                playerList.Add(other.gameObject);
                Debug.Log($"Added {other.gameObject} to the list of players");
            }

            if (progressBar.fillAmount >= 1f)
            {
                IsActivated = true;
                Debug.Log($"This Gate is activated!");

                other.gameObject.GetComponentInParent<PlayerController>().PlaySFX(6, transform.position, 500);

                activateObjects?.ForEach(x => x.SetActive(true));
            }

            photonView.RPC(nameof(UpdateProgressBar), RpcTarget.All);
        }
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
