using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class AmmoBox : MonoBehaviourPunCallbacks
{
    GameObject ammoCrate;
    BoxCollider boxCollider;
    private bool isRespawning;

    private void Start()
    {
        ammoCrate = transform.GetChild(0).gameObject;
        boxCollider = GetComponent<BoxCollider>();
    }

    public override void OnDisable() => StopAllCoroutines();

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Props"))
        {
            RespawnAmmoBox();
            other.gameObject.GetComponentInParent<PlayerController>().AddMagazine(Random.Range(2, 4));
            other.gameObject.GetComponentInParent<PlayerController>().PlaySFX(1, transform.position);
        }
    }

    public void RespawnAmmoBox() => photonView.RPC(nameof(RespawnStart), RpcTarget.All);

    [PunRPC]
    IEnumerator RespawnStart()
    {
        isRespawning = true;
        ammoCrate.SetActive(false);
        boxCollider.enabled = false;

        yield return new WaitForSeconds(30);

        isRespawning = false;
        ammoCrate.SetActive(true);
        boxCollider.enabled = true;
    }
}