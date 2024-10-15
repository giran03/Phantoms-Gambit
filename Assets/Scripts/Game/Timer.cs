using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using TMPro;
using UnityEngine;

public class Timer : MonoBehaviourPunCallbacks
{
    [SerializeField] TMP_Text timer;

    [SerializeField] float timeLeft = 5f;
    private bool isTimerRunning = true;

    private void Start()
    {
        PhotonNetwork.AutomaticallySyncScene = true;
    }

    [PunRPC]
    private void UpdateTimer(float newTime)
    {
        timer.text = "Time Left: " + Mathf.Round(newTime);
    }

    private void Update()
    {
        if (PhotonNetwork.IsMasterClient && isTimerRunning)
        {
            timeLeft -= Time.deltaTime;
            // UpdateTimer(timeLeft);
            photonView.RPC(nameof(UpdateTimer), RpcTarget.All, timeLeft);
            if (timeLeft <= 0)
            {
                isTimerRunning = false;
                Debug.Log("Game Over");
                PhotonNetwork.LoadLevel(3);
            }
        }
    }
}
