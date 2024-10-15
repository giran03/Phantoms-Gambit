using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class EndScreen : MonoBehaviourPunCallbacks
{
    private void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
    public void ReturnToMainMenu()
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
