using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class EDITOR_SceneSwitch : MonoBehaviour
{
    [MenuItem("Level Loader/Load Loading Scene #&1")]
    public static void Loading()
    {
        LoadScene("Assets/_Scenes/Loading.unity");
    }

    [MenuItem("Level Loader/Load Menu Scene #&2")]
    public static void Menu()
    {
        LoadScene("Assets/_Scenes/Menu.unity");
    }
    [MenuItem("Level Loader/Load Game Scene #&3")]
    public static void LoadGame()
    {
        LoadScene("Assets/_Scenes/Game.unity");
    }


    private static void LoadScene(string scenePath)
    {
        // Save just in case
        EditorSceneManager.SaveOpenScenes();
        // Load the given scene
        EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
    }
}
