using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Timer : MonoBehaviourPunCallbacks
{
    public static Timer Instance;

    [Header("Timer")]
    [SerializeField] GameObject huntIcon;
    [SerializeField] Image spiritualPowerProgress;
    [SerializeField] TMP_Text timer;
    [SerializeField] float maxCollectedSpiritOrb;
    public int collectedOrbs;

    [SerializeField] float timeLeft = 5f;
    [SerializeField] bool isTimerRunning = false;

    public List<GameObject> collectedSpiritOrbs = new();
    int orbsCount;

    public List<GameObject> downedPropsPlayers = new();
    public int downedPlayersCount = 0;

    private void Awake() => Instance = this;

    public override void OnDisable() => StopAllCoroutines();

    private void Start()
    {
        PhotonNetwork.AutomaticallySyncScene = true;
        spiritualPowerProgress.fillAmount = 0f;
    }

    private void Update()
    {
        if (PhotonNetwork.IsMasterClient && isTimerRunning)
        {
            timeLeft -= Time.deltaTime;

            photonView.RPC(nameof(UpdateTimer), RpcTarget.All, timeLeft);
            if (timeLeft <= 0)
            {
                isTimerRunning = false;
                Debug.Log("Game Over");
                if (PhotonNetwork.LocalPlayer.CustomProperties["assignment"].ToString() == "Hunter")
                    MusicManager.Instance.PlayMusic("win");
                else
                    MusicManager.Instance.PlayMusic("loss");
                PhotonNetwork.LoadLevel("HuntersWin");
            }

            if (Input.GetKeyDown(KeyCode.F4))
            {
                if (Input.GetKeyDown(KeyCode.F5))
                {
                    timeLeft = 2;
                }
            }

            photonView.RPC(nameof(UpdateSpiritualPowerProgress), RpcTarget.All, orbsCount);
        }
    }

    [PunRPC]
    private void UpdateTimer(float newTime) => timer.text = $"{Mathf.Round(newTime)}";

    [PunRPC]
    void UpdateSpiritualPowerProgress(int orbsCount) => spiritualPowerProgress.fillAmount = orbsCount / maxCollectedSpiritOrb;

    public void AddCollectedSpiritOrb(GameObject spiritOrb)
    {
        if (!collectedSpiritOrbs.Contains(spiritOrb))
            collectedSpiritOrbs.Add(spiritOrb);
        else
            Debug.LogError($"Already have this orb!");

        photonView.RPC(nameof(UpdateCount), RpcTarget.MasterClient, 1);
    }

    [PunRPC]
    void UpdateCount(int newCount) => orbsCount += newCount;

    public bool IsSpiritualPowerFull() => spiritualPowerProgress.fillAmount >= 1f;

    public void StartHunt() => photonView.RPC(nameof(ActivateHuntIcon), RpcTarget.All);

    [PunRPC]
    void ActivateHuntIcon() => huntIcon.SetActive(true);


    public void AddDownedPlayersToList(GameObject _propsPlayer)
    {
        if (!downedPropsPlayers.Contains(_propsPlayer))
            downedPropsPlayers.Add(_propsPlayer);

        photonView.RPC(nameof(AddDownedPlayers_RPC), RpcTarget.MasterClient);
    }

    public void RemoveDownedPlayersToList(GameObject _propsPlayer)
    {
        if (downedPropsPlayers.Contains(_propsPlayer))
            downedPropsPlayers.Remove(_propsPlayer);

        photonView.RPC(nameof(DescreaseDownedPlayers_RPC), RpcTarget.MasterClient);
    }

    [PunRPC]
    void AddDownedPlayers_RPC() => downedPlayersCount++;

    [PunRPC]
    void DescreaseDownedPlayers_RPC() => downedPlayersCount--;
}