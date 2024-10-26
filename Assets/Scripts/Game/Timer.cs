using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Timer : MonoBehaviourPunCallbacks
{
    public static Timer Instance;

    [Header("Timer")]
    [SerializeField] TMP_Text timer;

    [SerializeField] float timeLeft = 5f;
    private bool isTimerRunning = true;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        PhotonNetwork.AutomaticallySyncScene = true;
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
                PhotonNetwork.LoadLevel("HuntersWin");
            }

            if(Input.GetKeyDown(KeyCode.F5))
            {
                timeLeft = 2;
            }
        }
    }

    [PunRPC]
    private void UpdateTimer(float newTime)
    {
        timer.text = $"{Mathf.Round(newTime)}";
    }
}