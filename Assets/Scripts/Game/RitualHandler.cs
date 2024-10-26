using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class RitualHandler : MonoBehaviourPunCallbacks
{
    public GameObject[] toriiGates;
    public GameObject EscapeGate;
    public bool isCompleted;
    PhotonView _photonView;

    private void Start()
    {
        _photonView = GetComponent<PhotonView>();
        EscapeGate.SetActive(false);
    }

    private void Update()
    {
        if (!photonView.IsMine) return;

        CheckRitual();

        if (Input.GetKeyDown(KeyCode.U))
        {
            isCompleted = true;
            _photonView.RPC(nameof(SyncRitualComplete), RpcTarget.All, isCompleted);
        }
    }

    void CheckRitual()
    {
        foreach (var gate in toriiGates)
        {
            if (!gate.GetComponent<RitualPole>().IsActivated)
            {
                isCompleted = false;
                break;
            }
            else
            {
                isCompleted = true;
            }
        }

        _photonView.RPC(nameof(SyncRitualComplete), RpcTarget.All, isCompleted);
    }

    [PunRPC]
    void SyncRitualComplete(bool isRitualComplete)
    {
        isCompleted = isRitualComplete;

        if (isCompleted)
        {
            Debug.Log($"Ritual Complete!");
            EscapeGate.SetActive(true);
        }
    }
}
