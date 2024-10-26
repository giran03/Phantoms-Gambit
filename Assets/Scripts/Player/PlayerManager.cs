using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Linq;
using System.IO;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using TMPro;

public class PlayerManager : MonoBehaviourPunCallbacks
{
	//TODO: ADUST VALUE
	const float hunterSpawnTime = 2f;
	GameObject hunterOverlay;
	GameObject hudTimer;
	TMP_Text hunterSpawnText;

	PhotonView photonView;

	GameObject controller;

	int kills;
	int deaths;

	private bool _isSpawning;

	void Awake()
	{
		photonView = GetComponent<PhotonView>();
	}

	void Start()
	{
		if (photonView.IsMine)
		{
			hunterOverlay = GameObject.Find("Hunter Overlay");
			hudTimer = GameObject.Find("Timer Underlay");
			hunterSpawnText = GameObject.Find("Hunter Spawn Timer").GetComponent<TMP_Text>();

			hunterOverlay.SetActive(false);
			// CreateController();
			StartCoroutine(SpawnPlayers());

			var hash = new Hashtable
			{
				{ "isLoaded", true }
			};
			PhotonNetwork.LocalPlayer.SetCustomProperties(hash);

			Debug.Log($"Assignment: {photonView.Owner.CustomProperties["assignment"]}");
		}

		if (PhotonNetwork.IsMasterClient)
		{
			PhotonNetwork.AutomaticallySyncScene = true;
			Debug.Log($"Syncing scenes!");
		}
	}

	void CreateController()
	{
		Transform spawnpoint = SpawnManager.Instance.GetSpawnpoint();
		controller = PhotonNetwork.Instantiate(Path.Combine("PhotonPrefabs", "HunterGhost"), spawnpoint.position, spawnpoint.rotation, 0, new object[] { photonView.ViewID });
	}

	public void Die()
	{
		//TODO: DEATH SCREEN
		Debug.Log("Player died: " + (photonView.Owner.CustomProperties["assignment"].ToString() == "Hunter" ? "Hunter" : "Props"));
		string playerAssignment = photonView.Owner.CustomProperties["assignment"].ToString() == "Hunter" ? "Hunter" : "Props";
		// SpawnPlayers();

		switch (playerAssignment)
		{
			case "Props":
				StartCoroutine(SpawnPlayers());
				break;
			case "Hunter":
				photonView.RPC(nameof(LoadLevel), RpcTarget.All);
				break;
		}


		deaths++;
		Hashtable hash = new()
		{
			{ "deaths", deaths }
		};
		PhotonNetwork.LocalPlayer.SetCustomProperties(hash);

		PhotonNetwork.Destroy(controller);
	}

	[PunRPC]
	void LoadLevel() => PhotonNetwork.LoadLevel("PropsWin");

	public void GetKill() => photonView.RPC(nameof(RPC_GetKill), photonView.Owner);

	[PunRPC]
	void RPC_GetKill()
	{
		kills++;
		Hashtable hash = new()
		{
			{ "kills", kills }
		};
		PhotonNetwork.LocalPlayer.SetCustomProperties(hash);
	}

	public static PlayerManager Find(Player player) => FindObjectsOfType<PlayerManager>().SingleOrDefault(x => x.photonView.Owner == player);

	IEnumerator SpawnPlayers()
	{
		while (PhotonNetwork.PlayerList.Length > 0 && PhotonNetwork.PlayerList.Any(x => x.CustomProperties["isLoaded"] == null || !(bool)x.CustomProperties["isLoaded"]))
			yield return null;

		Transform spawnpoint = SpawnManager.Instance.GetSpawnpoint();

		string assignment = (string)PhotonNetwork.LocalPlayer.CustomProperties["assignment"];

		switch (assignment)
		{
			case "Hunter":
				StartCoroutine(SpawnDelay(hunterSpawnTime));
				StartCoroutine(SpawnTimerText(hunterSpawnTime));
				break;

			case "Props":
				controller = PhotonNetwork.Instantiate(Path.Combine("PhotonPrefabs", "Ghost"), spawnpoint.position, spawnpoint.rotation, 0, new object[] { photonView.ViewID });
				break;
		}

		IEnumerator SpawnDelay(float spawnTime)
		{
			hunterOverlay.SetActive(true);
			hudTimer.SetActive(false);
			yield return new WaitForSeconds(spawnTime);
			hudTimer.SetActive(true);
			hunterOverlay.SetActive(false);
			controller = PhotonNetwork.Instantiate(Path.Combine("PhotonPrefabs", "HunterGhost"), spawnpoint.position, spawnpoint.rotation, 0, new object[] { photonView.ViewID });
		}

		IEnumerator SpawnTimerText(float duration)
		{
			_isSpawning = true;
			while (duration > 0)
			{
				duration -= Time.deltaTime;
				hunterSpawnText.SetText($"spawning in {Mathf.Round(duration)}");
				yield return null;
			}
			_isSpawning = false;
		}
	}
}