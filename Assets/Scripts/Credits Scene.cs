using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CreditsScene : MonoBehaviour
{
    public void Button_ReturnToMenu()
    {
        SceneManager.LoadSceneAsync("Menu");
        SceneManager.UnloadSceneAsync("Credits");
    }
}
