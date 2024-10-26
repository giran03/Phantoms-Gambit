using UnityEngine;
using UnityEngine.UI;
using Photon.Realtime;
using Photon.Pun;

public class PlayerAvatar : MonoBehaviourPunCallbacks
{
    public Sprite[] avatar_icons;

    [SerializeField] Image uiImage;
    [SerializeField] Button nextButton;
    [SerializeField] Button previousButton;

    ExitGames.Client.Photon.Hashtable playerProperties = new();

    public static PlayerAvatar Instance;
    public Player Player { get; set; }

    int currentIndex = 0;

    private void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        // Initialize UI images with the first sprite
        UpdateUIImages(0);
        CheckLastPlayerIcon();

        nextButton.GetComponent<Button>().onClick.AddListener(() => NextButtonClicked());
        previousButton.GetComponent<Button>().onClick.AddListener(() => PreviousButtonClicked());
    }

    private void OnConnectedToServer()
    {
        Debug.Log($"Im called!");
        UpdateUIImages(0);
    }

    public void NextButtonClicked()
    {
        currentIndex = (currentIndex + 1) % avatar_icons.Length;
        UpdateUIImages(currentIndex);
    }

    public void PreviousButtonClicked()
    {
        currentIndex = (currentIndex - 1 + avatar_icons.Length) % avatar_icons.Length;
        UpdateUIImages(currentIndex);
    }

    void UpdateUIImages(int index)
    {
        uiImage.sprite = avatar_icons[index];
        playerProperties["playerAvatar"] = index;
        PhotonNetwork.SetPlayerCustomProperties(playerProperties);
    }

    public void CheckLastPlayerIcon()
    {
        if (PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey("playerAvatar"))
        {
            uiImage.sprite = avatar_icons[(int)PhotonNetwork.LocalPlayer.CustomProperties["playerAvatar"]];
            playerProperties["playerAvatar"] = avatar_icons[(int)PhotonNetwork.LocalPlayer.CustomProperties["playerAvatar"]];
        }
        else
            playerProperties["playerAvatar"] = 0;

        Debug.Log($"Updated player avatar of {PhotonNetwork.LocalPlayer.NickName} to {playerProperties["playerAvatar"]}");
    }
}