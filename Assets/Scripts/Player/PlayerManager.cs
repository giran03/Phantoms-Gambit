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
	const float hunterSpawnTime = 5f;
	const float propsSpawnDelay = 10f;

	// hunter
	GameObject hunterOverlay;
	TMP_Text hunterSpawnText;

	// props
	GameObject propsOverlay;
	TMP_Text propsSpawnText;

	GameObject hudTimer;
	GameObject progressOverlay;


	PhotonView photonView;
	GameObject controller;

	int kills;
	int deaths;
	bool _isSpawning;

	GameObject[] _props;
	List<GameObject> _downedProps = new();
	bool _gameDone = false;

	void Awake()
	{
		photonView = GetComponent<PhotonView>();
	}

	void Start()
	{
		if (photonView.IsMine)
		{
			Cursor.lockState = CursorLockMode.Locked;
			Cursor.visible = false;

			propsOverlay = GameObject.Find("Props Overlay");
			hunterOverlay = GameObject.Find("Hunter Overlay");

			hudTimer = GameObject.Find("Timer Underlay");
			progressOverlay = GameObject.Find("Props Progress Underlay");

			hunterSpawnText = GameObject.Find("Hunter Spawn Timer").GetComponent<TMP_Text>();
			propsSpawnText = GameObject.Find("Props Spawn Timer").GetComponent<TMP_Text>();

			propsOverlay.SetActive(false);
			hunterOverlay.SetActive(false);
			// CreateController();
			StartCoroutine(SpawnPlayers());

			var hash = new Hashtable
			{
				{ "isLoaded", true }
			};
			PhotonNetwork.LocalPlayer.SetCustomProperties(hash);

			Debug.Log($"Assignment: {photonView.Owner.CustomProperties["assignment"]}");

			StartCoroutine(FindAllProps());

			//bgm
			MusicManager.Instance.PlayMusic("game");
		}

		if (PhotonNetwork.IsMasterClient)
		{
			PhotonNetwork.AutomaticallySyncScene = true;
			Debug.Log($"Syncing scenes!");
		}
	}

	IEnumerator FindAllProps()
	{
		yield return new WaitForSeconds(15f);
		_props = GameObject.FindGameObjectsWithTag("Props");
		Debug.LogError($"Found Props players in scene: {_props.Length}");
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
				if (PhotonNetwork.LocalPlayer.CustomProperties["assignment"].ToString() == "Hunter")
					MusicManager.Instance.PlayMusic("loss");
				else
					MusicManager.Instance.PlayMusic("win");
				photonView.RPC(nameof(PropsWin), RpcTarget.All);
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

	public void CheckPropsAlive()
	{
		Debug.LogError($"Checking props alive~ {_props.Length} | {Timer.Instance.downedPlayersCount}");
		if (_props.Length > 0)
		{
			foreach (var props in _props)
			{
				if (props.GetComponentInParent<PlayerController>().currentHealth <= 0 && !props.GetComponentInParent<PlayerController>().CanMove)
				{
					if (!Timer.Instance.downedPropsPlayers.Contains(props))
						Timer.Instance.AddDownedPlayersToList(props);

					Debug.LogError($"Added {props} to downed props list");
				}
				else if (props.GetComponentInParent<PlayerController>().currentHealth > 0 && props.GetComponentInParent<PlayerController>().CanMove)
				{
					if (Timer.Instance.downedPropsPlayers.Contains(props))
						Timer.Instance.RemoveDownedPlayersToList(props);

					Debug.LogError($"Removed {props} from downed props list");
				}
			}
		}

		if (_gameDone) return;

		if (Timer.Instance.downedPlayersCount == _props.Length)
		{
			_gameDone = true;
			Debug.LogError($"HUNTERS WIN!");
			if (PhotonNetwork.LocalPlayer.CustomProperties["assignment"].ToString() == "Hunter")
				MusicManager.Instance.PlayMusic("win");
			else
				MusicManager.Instance.PlayMusic("loss");
			photonView.RPC(nameof(HuntersWin), RpcTarget.All);
		}
	}

	[PunRPC]
	void PropsWin() => PhotonNetwork.LoadLevel("PropsWin");

	[PunRPC]
	void HuntersWin() => PhotonNetwork.LoadLevel("HuntersWin");

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
				StartCoroutine(HunterSpawnDelay(propsSpawnDelay + hunterSpawnTime));
				StartCoroutine(HunterSpawnTimerText(propsSpawnDelay + hunterSpawnTime));
				break;
			case "Props":
				StartCoroutine(PropsSpawnDelay(propsSpawnDelay));
				StartCoroutine(PropsSpawnTimerText(propsSpawnDelay));
				break;
		}

		IEnumerator PropsSpawnDelay(float spawnTime)
		{
			Debug.LogError($"GLOBAL SPAWN TIMER {spawnTime}~");
			propsOverlay.SetActive(true);
			hudTimer.SetActive(false);
			progressOverlay.SetActive(false);
			yield return new WaitForSeconds(spawnTime);
			hudTimer.SetActive(true);
			progressOverlay.SetActive(true);
			propsOverlay.SetActive(false);
			controller = PhotonNetwork.Instantiate(Path.Combine("PhotonPrefabs", "Ghost"), spawnpoint.position, spawnpoint.rotation, 0, new object[] { photonView.ViewID });

		}

		IEnumerator HunterSpawnDelay(float spawnTime)
		{
			hunterOverlay.SetActive(true);
			hudTimer.SetActive(false);
			progressOverlay.SetActive(false);
			yield return new WaitForSeconds(spawnTime);
			hudTimer.SetActive(true);
			progressOverlay.SetActive(true);
			hunterOverlay.SetActive(false);
			controller = PhotonNetwork.Instantiate(Path.Combine("PhotonPrefabs", "HunterGhost"), spawnpoint.position, spawnpoint.rotation, 0, new object[] { photonView.ViewID });
		}

		IEnumerator HunterSpawnTimerText(float duration)
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

		IEnumerator PropsSpawnTimerText(float duration)
		{
			_isSpawning = true;
			while (duration > 0)
			{
				duration -= Time.deltaTime;
				propsSpawnText.SetText($"spawning in {Mathf.Round(duration)}");
				yield return null;
			}
			_isSpawning = false;
		}
	}
}