using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

[RequireComponent(typeof(PhotonView))]
[RequireComponent(typeof(PhotonTransformView))]
public class BearTrap : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Props") || other.gameObject.CompareTag("Hunter")) //TODO: CHANGE TO HUNTER
        {
            StartCoroutine(EnablePlayerAfterDelay(other));
        }
    }

    private IEnumerator EnablePlayerAfterDelay(Collider other)
    {
        Debug.Log($"Ooops~ Bear trap!");
        other.GetComponent<PlayerController>().enabled = false;
        yield return new WaitForSeconds(3f);
        other.GetComponent<PlayerController>().enabled = true;
        Destroy(gameObject);
    }
}
