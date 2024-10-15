using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class DisconnectRoom : MonoBehaviourPunCallbacks
{
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            DisconnectPlayer();
    }

    public void DisconnectPlayer()
    {
        StartCoroutine(DisconnectAndLoad());
    }

    IEnumerator DisconnectAndLoad()
    {
        PhotonNetwork.LeaveRoom();
        while (PhotonNetwork.InRoom)
        {
            yield return null;
        }
        PhotonNetwork.LoadLevel("Menu");
    }
}
