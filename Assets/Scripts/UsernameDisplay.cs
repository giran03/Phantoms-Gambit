using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UsernameDisplay : MonoBehaviour
{
	[SerializeField] PhotonView playerPV;
	[SerializeField] Image playerAvatar;
	[SerializeField] TMP_Text text;


	void Start()
	{
		if (GetComponentInParent<PlayerController>().IsHunter) return;

		text.text = playerPV.Owner.NickName;
		playerAvatar.sprite = GetComponentInChildren<PlayerListHUD>().playerAvatars[(int)playerPV.Owner.CustomProperties["playerAvatar"]];
	}
}
