using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Photon.Pun;
using UnityEngine;

public class EscapeGate : MonoBehaviourPunCallbacks
{
    List<GameObject> props = new();
    List<GameObject> propsInGame = new();
    PlayerManager playerManager;
    PhotonView _photonView;

    private void Start()
    {
        _photonView = GetComponent<PhotonView>();

        propsInGame = GameObject.FindGameObjectsWithTag("Props").ToList();
        foreach (var item in propsInGame)
        {
            Debug.Log($"Found props: {item.name}");
        }
    }
    private void Update()
    {
        if (props.Count > 0)
            if (propsInGame.SequenceEqual(props))
            {
                Debug.Log($"It is equal!");
            }
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Props"))
        {
            //game win
            if (!props.Contains(other.gameObject))
            {
                props.Add(other.gameObject);

                _photonView.RPC(nameof(LoadLevel), RpcTarget.All);
            }
        }
    }

    [PunRPC]
    public void LoadLevel()
    {
        PhotonNetwork.LoadLevel(2);
    }
}
