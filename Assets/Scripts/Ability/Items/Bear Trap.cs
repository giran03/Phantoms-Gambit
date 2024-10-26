using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

[RequireComponent(typeof(PhotonView))]
[RequireComponent(typeof(PhotonTransformView))]
public class BearTrap : MonoBehaviourPunCallbacks
{
    GameObject hunterTrapped;
    [SerializeField] ItemNetworkSFX itemNetworkSFX;
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Hunter"))
        {
            if (!other.GetComponentInParent<PlayerController>().IsHunter) return;

            hunterTrapped = other.gameObject;
            itemNetworkSFX.PlayItemSFX();

            other.GetComponentInParent<PlayerController>().TakeDamage(12f);

            photonView.RPC(nameof(EnablePlayerAfterDelay), RpcTarget.All);
        }
    }

    [PunRPC]
    IEnumerator EnablePlayerAfterDelay()
    {
        Debug.Log($"Ooops~ Bear trap!");
        hunterTrapped.GetComponentInParent<PlayerController>().CanMove = false;
        GetComponent<Collider>().enabled = false;

        yield return new WaitForSeconds(hunterTrapped.GetComponentInParent<PlayerController>().StunDuration);

        if (hunterTrapped.GetComponentInParent<PlayerController>().currentHealth > 0)
            hunterTrapped.GetComponentInParent<PlayerController>().CanMove = true;

        if (hunterTrapped.GetComponentInParent<AbilityHandler>().MaxTrap < 3)
            hunterTrapped.GetComponentInParent<AbilityHandler>().MaxTrap++;

        hunterTrapped.GetComponentInParent<PlayerController>().HidePlayerOutline();

        // TODO: network destroy?
        Destroy(gameObject);
    }
}