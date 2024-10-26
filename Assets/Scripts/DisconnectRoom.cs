using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class DisconnectRoom : MonoBehaviourPunCallbacks
{
    private void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            ReturnToMainMenu();
    }
    public void ReturnToMainMenu()
    {
        PhotonNetwork.LeaveRoom();
    }
    public override void OnLeftRoom()
    {
        Debug.Log($"Left Room!");
        PhotonNetwork.LoadLevel("Menu");
    }
}
