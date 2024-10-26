using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class ItemNetworkSFX : MonoBehaviourPunCallbacks
{
    AudioSource _audioSource;
    public AudioClip itemSFX;

    private void Start() => _audioSource = GetComponentInChildren<AudioSource>();

    public void PlayItemSFX() => GetComponentInChildren<PhotonView>().RPC(nameof(PlayItemSFX_RPC), RpcTarget.All);

    [PunRPC]
    public void PlayItemSFX_RPC()
    {
        _audioSource.clip = itemSFX;

        _audioSource.pitch = Random.Range(0.7f, 1.2f);
        _audioSource.volume = Random.Range(.2f, .35f);

        _audioSource.Play();
    }
}