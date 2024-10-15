using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using TMPro;
using Photon.Realtime;
using System.Linq;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class Launcher : MonoBehaviourPunCallbacks
{
	public static Launcher Instance;

	[SerializeField]
	private TMP_InputField roomNameInputField;
	[SerializeField]
	private TMP_Text errorText;
	[SerializeField]
	private TMP_Text roomNameText;
	[SerializeField]
	private Transform roomListContent;
	[SerializeField]
	private GameObject roomListItemPrefab;
	[SerializeField]
	private Transform playerListContent;
	[SerializeField]
	private GameObject PlayerListItemPrefab;
	[SerializeField]
	GameObject startGameButton;
	int _maxHunters = 1;
	string assignment;
	List<Player> _huntedPlayers = new();

	List<Player> allPlayers = new();

	void Awake()
	{
		Instance = this;
	}

	public void CreateRoom()
	{
		if (string.IsNullOrEmpty(roomNameInputField.text))
			return;

		PhotonNetwork.CreateRoom(roomNameInputField.text);
		MenuManager.Instance.OpenMenu("loading");
	}

	public override void OnJoinedRoom()
	{
		MenuManager.Instance.OpenMenu("room");
		roomNameText.text = PhotonNetwork.CurrentRoom.Name;

		Player[] players = PhotonNetwork.PlayerList;

		foreach (Transform child in playerListContent)
		{
			Destroy(child.gameObject);
		}

		for (int i = 0; i < players.Count(); i++)
		{
			Instantiate(PlayerListItemPrefab, playerListContent).GetComponent<PlayerListItem>().SetUp(players[i]);
		}

		SetAssignmentLocal();

		startGameButton.SetActive(PhotonNetwork.IsMasterClient);
	}

	public override void OnMasterClientSwitched(Player newMasterClient)
	{
		startGameButton.SetActive(PhotonNetwork.IsMasterClient);
	}

	public override void OnCreateRoomFailed(short returnCode, string message)
	{
		errorText.text = "Room Creation Failed: " + message;
		Debug.LogError("Room Creation Failed: " + message);
		MenuManager.Instance.OpenMenu("error");
	}

	public void StartGame()
	{
		PhotonNetwork.LoadLevel(1);
	}

	public void LeaveRoom()
	{
		PhotonNetwork.LeaveRoom();
		MenuManager.Instance.OpenMenu("loading");
	}

	public void JoinRoom(RoomInfo info)
	{
		PhotonNetwork.JoinRoom(info.Name);
		MenuManager.Instance.OpenMenu("loading");
	}

	public override void OnLeftRoom()
	{
		MenuManager.Instance.OpenMenu("title");
	}

	public override void OnRoomListUpdate(List<RoomInfo> roomList)
	{
		foreach (Transform trans in roomListContent)
		{
			Destroy(trans.gameObject);
		}

		for (int i = 0; i < roomList.Count; i++)
		{
			if (roomList[i].RemovedFromList)
				continue;
			Instantiate(roomListItemPrefab, roomListContent).GetComponent<RoomListItem>().SetUp(roomList[i]);
		}
	}

	public override void OnPlayerEnteredRoom(Player newPlayer)
	{
		Instantiate(PlayerListItemPrefab, playerListContent).GetComponent<PlayerListItem>().SetUp(newPlayer);

		SetAssignmentOther(newPlayer);
	}

	void SetAssignment(Player player)
	{
		if (player.CustomProperties.ContainsKey("assignment"))
		{
			player.CustomProperties.Remove("assignment");
			return;
		}

		int randomNum = Random.Range(1, 101);

		if (_maxHunters > 0)
		{
			Debug.Log($"Max hunters: {_maxHunters}");
			_maxHunters--;
			Debug.Log($"Max hunters new: {_maxHunters}");
			assignment = "Hunter";
		}
		else
		{
			assignment = "Props";
		}

		Hashtable hash = new()
		{
			{ "assignment", assignment }
		};

		player.SetCustomProperties(hash);
		Debug.Log($"Assigned: {assignment}");
	}

	void SetAssignmentLocal()
	{
		if (!PhotonNetwork.IsMasterClient) return;

		if (PhotonNetwork.MasterClient.CustomProperties.ContainsKey("assignment"))
			PhotonNetwork.MasterClient.CustomProperties.Remove("assignment");

		assignment = "Props"; // Default: Hunter

		Hashtable hash = new()
		{
			{ "assignment", assignment }
		};

		PhotonNetwork.MasterClient.SetCustomProperties(hash);
		Debug.Log($"Assigned to master: {assignment}");
	}

	void SetAssignmentOther(Player player)
	{
		if (player.CustomProperties.ContainsKey("assignment"))
			player.CustomProperties.Remove("assignment");

		assignment = "Props";

		Hashtable hash = new()
		{
			{ "assignment", assignment }
		};

		player.SetCustomProperties(hash);
		Debug.Log($"Assigned to master: {assignment}");
	}

	void Assign(Player player)
	{
		if (player.CustomProperties.ContainsKey("assignment")) return;

		if (_huntedPlayers.Count == 0)
		{
			_huntedPlayers.Add(player);
			assignment = "Hunter";
		}
		else if (_huntedPlayers.Count > 0)
		{
			assignment = "Props";
		}

		Hashtable hash = new()
		{
			{ "assignment", assignment }
		};
		player.SetCustomProperties(hash);
		Debug.Log($"Assigned: {assignment}");
	}

	IEnumerator AssignDelay()
	{
		yield return new WaitForSeconds(.5f);
		UpdatePlayerList();
	}

	private void UpdatePlayerList()
	{
		List<Player> players = PhotonNetwork.PlayerList.ToList();

		allPlayers.Clear();
		allPlayers = players;

		foreach (Player _player in allPlayers)
		{
			Assign(_player);
			Debug.Log($"Assigning to {_player.NickName}");
		}
	}
}