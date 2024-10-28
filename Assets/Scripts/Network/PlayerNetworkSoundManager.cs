using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class PlayerNetworkSoundManager : MonoBehaviourPunCallbacks
{
    public GameObject audioSourcePrefab;
    AudioSource footstepSource;
    public AudioClip footstepSFX;

    public AudioSource gunShotSource;
    public AudioClip[] gunShotSFX;

    public AudioSource whistleSource;
    public AudioClip[] whistleSFX;

    public AudioSource otherSource;
    public AudioClip[] otherSFX;

    GameObject spawnedAudioSource;
    PhotonView _photonView;

    public void PlayFootstepSFX() => _photonView.RPC(nameof(PlayFootstepSFX_RPC), RpcTarget.All);
    
    private void Start()
    {
        if(photonView.IsMine)
        {
            _photonView = GetComponent<PhotonView>();
            footstepSource = GetComponentInParent<AudioSource>();
        }
    }

    [PunRPC]
    public void PlayFootstepSFX_RPC()
    {
        // footstepSource.clip = footstepSFX;
        // footstepSource.pitch = Random.Range(0.7f, 1.2f);
        // footstepSource.volume = Random.Range(.2f, .35f);
        // footstepSource.Play();

        var spawnedAudioSource = PhotonNetwork.Instantiate(audioSourcePrefab.name, transform.position, Quaternion.identity);
        var audioSource = spawnedAudioSource.GetComponent<AudioSource>();
        audioSource.pitch = Random.Range(0.7f, 1.2f);
        audioSource.volume = Random.Range(.2f, .35f);
        audioSource.clip = footstepSFX;
        audioSource.Play();
        StartCoroutine(DestroyAudioObjectAfterClipFinished(spawnedAudioSource, transform.position));
    }

    public void PlayGunShotSFX(int index) => _photonView.RPC(nameof(PlayGunShotSFX_RPC), RpcTarget.All, index);

    [PunRPC]
    public void PlayGunShotSFX_RPC(int index)
    {
        gunShotSource.clip = gunShotSFX[index];
        gunShotSource.volume = Random.Range(.2f, .35f);
        gunShotSource.Play();

        // var spawnedAudioSource = PhotonNetwork.Instantiate(audioSourcePrefab.name, transform.position, Quaternion.identity);
        // var audioSource = spawnedAudioSource.GetComponent<AudioSource>();
        // audioSource.volume = Random.Range(.2f, .35f);
        // audioSource.clip = gunShotSFX[index];
        // audioSource.Play();
        // StartCoroutine(DestroyAudioObjectAfterClipFinished(spawnedAudioSource, transform.position));
    }

    public void PlayWhistleSFX() => _photonView.RPC(nameof(PlayWhistleSFX_RPC), RpcTarget.All);

    [PunRPC]
    public void PlayWhistleSFX_RPC()
    {
        int randomIndex = Random.Range(0, whistleSFX.Length);
        whistleSource.clip = whistleSFX[randomIndex];
        whistleSource.Play();

        // var spawnedAudioSource = PhotonNetwork.Instantiate(audioSourcePrefab.name, transform.position, Quaternion.identity);
        // var audioSource = spawnedAudioSource.GetComponent<AudioSource>();
        // audioSource.clip = whistleSFX[Random.Range(0, whistleSFX.Length)];
        // audioSource.Play();
        // StartCoroutine(DestroyAudioObjectAfterClipFinished(spawnedAudioSource, transform.position));
    }

    public void PlayOtherSFX(int index, Vector3 spawnAudioSourcePosition, int maxAudioDistance = 35) =>
                _photonView.RPC(nameof(PlayOtherSFX_RPC), RpcTarget.All, index, spawnAudioSourcePosition, maxAudioDistance);

    [PunRPC]
    public void PlayOtherSFX_RPC(int index, Vector3 spawnAudioSourcePosition, int maxAudioDistance)
    {
        spawnedAudioSource = PhotonNetwork.Instantiate(audioSourcePrefab.name, transform.position, Quaternion.identity);
        AudioSource audioSource = spawnedAudioSource.GetComponent<AudioSource>();
        audioSource.maxDistance = maxAudioDistance;
        audioSource.clip = otherSFX[index];
        audioSource.Play();
        StartCoroutine(DestroyAudioObjectAfterClipFinished(spawnedAudioSource, spawnAudioSourcePosition));
    }

    [PunRPC]
    void StopCurrentSFX_RPC()
    {
        AudioSource audioSource = spawnedAudioSource.GetComponent<AudioSource>();
        audioSource.Stop();
        if (spawnedAudioSource != null)
            PhotonNetwork.Destroy(spawnedAudioSource);
    }


    IEnumerator DestroyAudioObjectAfterClipFinished(GameObject audioObject, Vector3 spawnAudioSourcePosition)
    {
        AudioSource audioSource = audioObject.GetComponent<AudioSource>();

        if (audioObject != null)
        {
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
}