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
    [SerializeField] GameObject[] ammoBoxes;

    private void Start()
    {
        progressBar.fillAmount = 0f;
    }

    private void Update()
    {
        if (!photonView.IsMine) return;

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
                    players.GetComponentInParent<Outline>().enabled = false;
                }
                playerList.Clear();
            }
        }

        if (progressBar.fillAmount >= .1f && isActivating && !IsActivated)
        {
            foreach (var players in playerList)
            {
                players.GetComponentInParent<Outline>().enabled = true;
            }
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.gameObject.CompareTag("Props"))
        {
            isActivating = true;

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

                other.gameObject.GetComponentInParent<PlayerController>().PlaySFX(0);

                if (ammoBoxes.Length > 0)
                    for (int i = 0; i < ammoBoxes.Length; i++)
                    {
                        ammoBoxes[i].SetActive(true);
                        Debug.Log($"Activated ammos!");
                    }
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.CompareTag("Props"))
            isActivating = false;
    }
}
