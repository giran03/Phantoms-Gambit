using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class PlayerNetworkSoundManager : MonoBehaviourPunCallbacks
{
    public GameObject audioSourcePrefab;
    public AudioSource footstepSource;
    public AudioClip footstepSFX;

    public AudioSource gunShotSource;
    public AudioClip[] gunShotSFX;

    public AudioSource whistleSource;
    public AudioClip[] whistleSFX;

    public AudioSource otherSource;
    public AudioClip[] otherSFX;

    public void PlayFootstepSFX() => GetComponent<PhotonView>().RPC(nameof(PlayFootstepSFX_RPC), RpcTarget.All);

    [PunRPC]
    public void PlayFootstepSFX_RPC()
    {
        footstepSource.clip = footstepSFX;

        footstepSource.pitch = Random.Range(0.7f, 1.2f);
        footstepSource.volume = Random.Range(.2f, .35f);

        footstepSource.Play();
    }

    public void PlayGunShotSFX(int index) => GetComponent<PhotonView>().RPC(nameof(PlayGunShotSFX_RPC), RpcTarget.All, index);

    [PunRPC]
    public void PlayGunShotSFX_RPC(int index)
    {
        gunShotSource.clip = gunShotSFX[index];

        gunShotSource.volume = Random.Range(.2f, .35f);

        gunShotSource.Play();
    }

    public void PlayWhistleSFX() => GetComponent<PhotonView>().RPC(nameof(PlayWhistleSFX_RPC), RpcTarget.All);

    [PunRPC]
    public void PlayWhistleSFX_RPC()
    {
        int randomIndex = Random.Range(0, whistleSFX.Length);
        whistleSource.clip = whistleSFX[randomIndex];

        whistleSource.Play();
    }

    public void PlayOtherSFX(int index, Vector3 spawnAudioSourcePosition, int maxAudioDistance = 35) =>
                GetComponent<PhotonView>().RPC(nameof(PlayOtherSFX_RPC), RpcTarget.All, index, spawnAudioSourcePosition, maxAudioDistance);

    [PunRPC]
    public void PlayOtherSFX_RPC(int index, Vector3 spawnAudioSourcePosition, int maxAudioDistance)
    {
        GameObject audioObject = PhotonNetwork.Instantiate(audioSourcePrefab.name, transform.position, Quaternion.identity);
        AudioSource audioSource = audioObject.GetComponent<AudioSource>();
        audioSource.maxDistance = maxAudioDistance;
        audioSource.clip = otherSFX[index];
        audioSource.Play();
        StartCoroutine(DestroyAudioObjectAfterClipFinished(audioObject, spawnAudioSourcePosition));
    }


    IEnumerator DestroyAudioObjectAfterClipFinished(GameObject audioObject, Vector3 spawnAudioSourcePosition)
    {
        AudioSource audioSource = audioObject.GetComponent<AudioSource>();

        while (audioSource.time < audioSource.clip.length)
        {
            if (spawnAudioSourcePosition != Vector3.zero)
                audioObject.transform.SetPositionAndRotation(spawnAudioSourcePosition, Quaternion.identity);
            else
                audioObject.transform.position = transform.position;

            yield return null;
        }

        PhotonNetwork.Destroy(audioObject);
    }
}