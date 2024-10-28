using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ButtonsSFX : MonoBehaviour
{
    public void OnHover() => MusicManager.Instance.PlayUiSFX("enter");
    public void OnExit() => MusicManager.Instance.PlayUiSFX("exit");
    public void OnClick() => MusicManager.Instance.PlayUiSFX("click");
}
