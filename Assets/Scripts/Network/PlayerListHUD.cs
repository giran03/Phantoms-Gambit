using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerListHUD : MonoBehaviourPunCallbacks
{
    public Sprite[] playerAvatars;
    public GameObject playerEntryPrefab;
    public Transform playerListContent;
    Dictionary<int, GameObject> playerListDictionary = new();

    List<Player> roomPlayers = new();

    void Start() => RefreshPlayerHUD();

    void RefreshPlayerHUD() => photonView.RPC(nameof(SetHUD), RpcTarget.All);

    [PunRPC]
    IEnumerator SetHUD()
    {
        if (roomPlayers.Count > 0)
        {
            roomPlayers.Clear();
            playerListDictionary.Clear();
        }

        yield return new WaitForSeconds(10f);

        foreach (var player in PhotonNetwork.PlayerList)
        {
            //TODO: DON'T INCLUDE LOCAL PLAYER IN HUD
            // if(player.IsLocal) return;
            if (player.CustomProperties["assignment"].ToString() == "Props")
            {
                GameObject playerEntryInstance = Instantiate(playerEntryPrefab, playerListContent);
                Image playerIcon = playerEntryInstance.GetComponent<Image>();
                playerIcon.sprite = playerAvatars[(int)player.CustomProperties["playerAvatar"]];
                TMP_Text playerName = playerEntryInstance.GetComponentInChildren<TMP_Text>();
                playerName.SetText($"{player.NickName}");

                Transform hpBarTransform = playerEntryInstance.transform.Find("HPbar");

                if (hpBarTransform != null)
                {
                    Image hpBar = hpBarTransform.GetComponent<Image>();
                    hpBar.fillAmount = (float)player.CustomProperties["currentHealth"] / (float)player.CustomProperties["maxHealth"];
                }

                if (!roomPlayers.Contains(player))
                {
                    roomPlayers.Add(player);
                    playerListDictionary.Add(player.ActorNumber, playerEntryInstance);
                }
            }
        }
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
    {
        if (targetPlayer.CustomProperties.ContainsKey("currentHealth"))
        {
            var player = roomPlayers.Find(x => x == targetPlayer);
            if (player != null)
            {
                if (playerListDictionary.TryGetValue(targetPlayer.ActorNumber, out GameObject _gameObject))
                {
                    Image hpBar = _gameObject.transform.Find("HPbar").GetComponent<Image>();
                    hpBar.fillAmount = (float)targetPlayer.CustomProperties["currentHealth"] / (float)targetPlayer.CustomProperties["maxHealth"];
                }
            }
        }
    }
}