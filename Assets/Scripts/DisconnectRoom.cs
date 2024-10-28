using Photon.Pun;
using UnityEngine;

public class DisconnectRoom : MonoBehaviourPunCallbacks
{
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            ReturnToMainMenu();
    }
    public void ReturnToMainMenu()
    {
        PhotonNetwork.LeaveRoom();
        PhotonNetwork.LoadLevel("Menu");
    }
    public override void OnLeftRoom()
    {
        Debug.Log($"Left Room!");
        PhotonNetwork.LoadLevel("Menu");
    }
}