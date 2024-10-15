using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Linq;
using System.IO;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class PlayerManager : MonoBehaviour
{
	PhotonView photonView;

	GameObject controller;

	int kills;
	int deaths;

	List<Player> propsThatHasDied = new();
	List<Player> huntersThatHasDied = new();


	void Awake()
	{
		photonView = GetComponent<PhotonView>();
	}

	void Start()
	{
		if (photonView.IsMine)
		{
			// CreateController();
			SpawnPlayers();
		}

		if(PhotonNetwork.IsMasterClient)
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
		PhotonNetwork.Destroy(controller);

		//TODO: DEATH SCREEN
		Debug.Log("Player died: " + (photonView.Owner.CustomProperties["assignment"].ToString() == "Hunter" ? "Hunter" : "Props"));
		string playerAssignment = photonView.Owner.CustomProperties["assignment"].ToString() == "Hunter" ? "Hunter" : "Props";

		switch (playerAssignment)
		{
			case "Props":
				SpawnPlayers();
				break;
			case "Hunter":
				PhotonNetwork.LoadLevel(2);
				break;
		}


		deaths++;
		Hashtable hash = new Hashtable();
		hash.Add("deaths", deaths);
		PhotonNetwork.LocalPlayer.SetCustomProperties(hash);
	}

	public void GetKill()
	{
		photonView.RPC(nameof(RPC_GetKill), photonView.Owner);
	}

	[PunRPC]
	void RPC_GetKill()
	{
		kills++;

		Hashtable hash = new Hashtable();
		hash.Add("kills", kills);
		PhotonNetwork.LocalPlayer.SetCustomProperties(hash);
	}

	public static PlayerManager Find(Player player)
	{
		return FindObjectsOfType<PlayerManager>().SingleOrDefault(x => x.photonView.Owner == player);
	}

	void SpawnPlayers()
	{
		Transform spawnpoint = SpawnManager.Instance.GetSpawnpoint();

		string assignment = (string)PhotonNetwork.LocalPlayer.CustomProperties["assignment"];

		switch (assignment)
		{
			case "Hunter":
				StartCoroutine(SpawnDelay());
				break;

			case "Props":
				controller = PhotonNetwork.Instantiate(Path.Combine("PhotonPrefabs", "Ghost"), spawnpoint.position, spawnpoint.rotation, 0, new object[] { photonView.ViewID });
				break;
		}

		IEnumerator SpawnDelay()
		{
			Debug.Log("$Spawning in 5 seconds~");
			yield return new WaitForSeconds(5f);
			controller = PhotonNetwork.Instantiate(Path.Combine("PhotonPrefabs", "HunterGhost"), spawnpoint.position, spawnpoint.rotation, 0, new object[] { photonView.ViewID });
		}
	}
}