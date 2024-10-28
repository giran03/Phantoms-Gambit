using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class SpiritOrb : MonoBehaviourPunCallbacks
{
    [SerializeField] GameObject spiritOrb;

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Props"))
        {
            other.GetComponentInParent<PlayerController>().playerNetworkSoundManager.PlayOtherSFX(5, transform.position);
            other.GetComponentInParent<PlayerController>().AddOrbToInventory(spiritOrb);
            GetComponent<PhotonView>().RPC(nameof(RPC_SetInactive), RpcTarget.All);
        }
    }

    [PunRPC]
    void RPC_SetInactive() => gameObject.SetActive(false);

    IEnumerator Respawn()
    {
        spiritOrb.SetActive(false);
        GetComponent<CapsuleCollider>().enabled = false;
        Debug.Log($"ORBRespawning in 120 seconds");
        yield return new WaitForSeconds(120);
        spiritOrb.SetActive(true);
        GetComponent<CapsuleCollider>().enabled = true;
    }
}