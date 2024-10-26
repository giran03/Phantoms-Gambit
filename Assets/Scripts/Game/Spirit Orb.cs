using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class SpiritOrb : MonoBehaviourPunCallbacks
{
    private void OnTriggerEnter(Collider other)
    {
        if (!photonView.IsMine) return;

        if (other.gameObject.CompareTag("Props"))
        {
            PlayerController.playerNetworkSoundManager.PlayOtherSFX(5, transform.position);
            other.GetComponentInParent<PlayerController>().AddOrbToInventory(gameObject);
            GetComponent<PhotonView>().RPC(nameof(RPC_SetInactive), RpcTarget.All);
        }
    }

    [PunRPC]
    void RPC_SetInactive() => gameObject.SetActive(false);
}