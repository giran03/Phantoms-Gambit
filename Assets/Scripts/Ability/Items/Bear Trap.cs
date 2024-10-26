using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

[RequireComponent(typeof(PhotonView))]
[RequireComponent(typeof(PhotonTransformView))]
public class BearTrap : MonoBehaviourPunCallbacks
{
    [SerializeField] ItemNetworkSFX itemNetworkSFX;
    private void OnTriggerEnter(Collider other)
    {
        if(!photonView.IsMine) return;

        if (other.gameObject.CompareTag("Hunter"))
        {
            itemNetworkSFX.PlayItemSFX();

            other.GetComponentInParent<PlayerController>().TakeDamage(12f);

            StartCoroutine(EnablePlayerAfterDelay(other));
        }
    }

    private IEnumerator EnablePlayerAfterDelay(Collider other)
    {
        Debug.Log($"Ooops~ Bear trap!");
        other.GetComponentInParent<PlayerController>().CanMove = false;
        GetComponent<Collider>().enabled = false;

        yield return new WaitForSeconds(other.GetComponentInParent<PlayerController>().StunDuration);

        if (other.GetComponentInParent<PlayerController>().currentHealth > 0)
            other.GetComponentInParent<PlayerController>().CanMove = true;

        if (other.GetComponentInParent<AbilityHandler>().MaxTrap < 3)
            other.GetComponentInParent<AbilityHandler>().MaxTrap++;

        other.GetComponentInParent<PlayerController>().HidePlayerOutline();
        PhotonNetwork.Destroy(gameObject);
    }
}