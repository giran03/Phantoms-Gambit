using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class PlayerNetworkSoundManager : MonoBehaviourPunCallbacks
{
    public AudioSource footstepSource;
    public AudioClip footstepSFX;

    public AudioSource gunShotSource;
    public AudioClip[] gunShotSFX;

    public AudioSource whistleSource;
    public AudioClip[] whistleSFX;

    public AudioSource otherSource;
    public AudioClip[] otherSFX;

    public void PlayFootstepSFX()
    {
        GetComponent<PhotonView>().RPC(nameof(PlayFootstepSFX_RPC), RpcTarget.All);
    }

    [PunRPC]
    public void PlayFootstepSFX_RPC()
    {
        footstepSource.clip = footstepSFX;

        footstepSource.pitch = Random.Range(0.7f, 1.2f);
        footstepSource.volume = Random.Range(.2f, .35f);

        footstepSource.Play();
    }

    public void PlayGunShotSFX(int index)
    {
        GetComponent<PhotonView>().RPC(nameof(PlayGunShotSFX_RPC), RpcTarget.All, index);
    }

    [PunRPC]
    public void PlayGunShotSFX_RPC(int index)
    {
        gunShotSource.clip = gunShotSFX[index];

        gunShotSource.volume = Random.Range(.2f, .35f);

        gunShotSource.Play();
    }

    public void PlayWhistleSFX()
    {
        GetComponent<PhotonView>().RPC(nameof(PlayWhistleSFX_RPC), RpcTarget.All);
    }

    [PunRPC]
    public void PlayWhistleSFX_RPC()
    {
        int randomIndex = Random.Range(0, whistleSFX.Length);
        whistleSource.clip = whistleSFX[randomIndex];

        whistleSource.volume = Random.Range(.2f, .35f);

        whistleSource.Play();
    }

    public void PlayOtherSFX(int index)
    {
        GetComponent<PhotonView>().RPC(nameof(PlayOtherSFX_RPC), RpcTarget.All, index);
    }

    [PunRPC]
    public void PlayOtherSFX_RPC(int index)
    {
        otherSource.clip = otherSFX[index];

        otherSource.volume = Random.Range(.2f, .35f);

        otherSource.Play();
    }
}