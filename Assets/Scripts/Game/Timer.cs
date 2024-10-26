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
    private bool isTimerRunning = true;

    public List<GameObject> collectedSpiritOrbs = new();
    int orbsCount;

    private void Awake()
    {
        Instance = this;
    }

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
                PhotonNetwork.LoadLevel("HuntersWin");
            }

            if (Input.GetKeyDown(KeyCode.F5))
            {
                timeLeft = 2;
            }

            photonView.RPC(nameof(UpdateSpiritualPowerProgress), RpcTarget.All, orbsCount);
            Debug.LogError($"PROGRESS BAR: {orbsCount} / {maxCollectedSpiritOrb}");
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
}