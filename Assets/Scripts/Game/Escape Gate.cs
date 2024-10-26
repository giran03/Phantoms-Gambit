using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Photon.Pun;
using UnityEngine;

public class EscapeGate : MonoBehaviourPunCallbacks
{
    public static EscapeGate Instance;
    public List<GameObject> requiredSpiritOrbs = new();
    List<GameObject> props = new();
    List<GameObject> propsInGame = new();
    PhotonView _photonView;
    private bool huntingHasStarted;

    public static bool IsUsable { get; set; }

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        _photonView = GetComponent<PhotonView>();

        propsInGame = GameObject.FindGameObjectsWithTag("Props").ToList();
        foreach (var item in propsInGame)
        {
            Debug.Log($"Found props: {item.name}");
        }

        requiredSpiritOrbs = GameObject.FindGameObjectsWithTag("Orb").ToList();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsUsable) return;

        if (other.gameObject.CompareTag("Props") && !huntingHasStarted)
        {
            huntingHasStarted = true;
            photonView.RPC(nameof(StartHuntingHunter), RpcTarget.All);
        }
    }

    [PunRPC]
    public void LoadLevel() => PhotonNetwork.LoadLevel(2);

    [PunRPC]
    void StartHuntingHunter()
    {
        foreach (var props in GameObject.FindGameObjectsWithTag("Props"))
            props.GetComponentInParent<PlayerController>().HuntHunter();
        foreach (var hunters in GameObject.FindGameObjectsWithTag("Hunter"))
            hunters.GetComponentInParent<PlayerController>().HuntHunter();
    }

    public int GetOrbsCount() => requiredSpiritOrbs.Count;

    bool CompareLists<T>(List<T> aListA, List<T> aListB) => aListA.Count == aListB.Count && aListA.All(aListB.Contains);
}
