using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AmmoBox : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Props"))
        {
            Destroy(gameObject);
            other.gameObject.GetComponentInParent<PlayerController>().AddMagazine(Random.Range(1, 2));
            other.gameObject.GetComponentInParent<PlayerController>().PlaySFX(1);
        }
    }
}
