using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using TMPro;
using Photon.Realtime;
using System.Linq;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class Launcher : MonoBehaviourPunCallbacks
{
	public static Launcher Instance;

	[SerializeField] TMP_InputField roomNameInputField;
	[SerializeField] TMP_Text errorText;
	[SerializeField] TMP_Text roomNameText;
	[SerializeField] Transform roomListContent;
	[SerializeField] GameObject roomListItemPrefab;
	[SerializeField] Transform playerListContent;
	[SerializeField] GameObject PlayerListItemPrefab;
	[SerializeField] GameObject startGameButton;
	[SerializeField] GameObject infoText;

	const int minimumPlayers = 2;
	int _maxHunters = 1;
	string assignment;
	List<Player> _huntedPlayers = new();
	bool isClicked = false;

	void Awake() => Instance = this;

	private void Start()
	{
		Cursor.lockState = CursorLockMode.None;
		Cursor.visible = true;

		//bgm
		MusicManager.Instance.PlayMusic("main");
	}

	private void Update() => RoomPlayers();

	void RoomPlayers()
	{
		bool playerRequired = PhotonNetwork.PlayerList.Length >= minimumPlayers;
		if (!isClicked)
			startGameButton.GetComponent<Button>().interactable = playerRequired;
		infoText.SetActive(!playerRequired);
	}

	public void CreateRoom()
	{
		if (string.IsNullOrEmpty(roomNameInputField.text))
			return;

		RoomOptions roomOptions = new()
		{
			BroadcastPropsChangeToAll = true,
			MaxPlayers = 5,
		};
		PhotonNetwork.CreateRoom(roomNameInputField.text, roomOptions);
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
		startGameButton.SetActive(PhotonNetwork.IsMasterClient);

		//TODO: REMOVE ME!!
		// SetAssignmentProps(PhotonNetwork.LocalPlayer);

		// set random assignments
		if (PhotonNetwork.PlayerList.Length < minimumPlayers - 1) return;

		if (_huntedPlayers.Count < 1)
		{
			int randomIndex = Random.Range(0, PhotonNetwork.PlayerList.Length);
			_huntedPlayers.Add(PhotonNetwork.PlayerList[randomIndex]);
			SetAssignmentHunter(PhotonNetwork.PlayerList[randomIndex]);
		}

		foreach (var player in PhotonNetwork.PlayerList)
			if (!_huntedPlayers.Contains(player))
				SetAssignmentProps(player);
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
		isClicked = true;
		List<string> mapNames = new() { "Map1", "Map2" };
		string randomMap = mapNames[Random.Range(0, mapNames.Count)];
		Debug.Log($"Random map index {randomMap}");
		PhotonNetwork.LoadLevel(randomMap);
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
	}

	void SetAssignmentHunter(Player player)
	{
		if (player.CustomProperties.ContainsKey("assignment"))
			player.CustomProperties.Remove("assignment");

		assignment = "Hunter"; // Default: Hunter

		Hashtable hash = new()
		{
			{ "assignment", assignment }
		};
		player.SetCustomProperties(hash);
		Debug.Log($"Assigned to master: {assignment}");
	}

	void SetAssignmentProps(Player player)
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

	public void Button_RefreshRoomlist() => PhotonNetwork.JoinLobby();

	public void Button_Credits()
	{
		SceneManager.LoadSceneAsync("Credits");
		SceneManager.UnloadSceneAsync("Menu");
	}
}