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
    [MenuItem("Level Loader/Load Map 1 #&3")]
    public static void LoadMap1()
    {
        LoadScene("Assets/_Scenes/Map1.unity");
    }
    [MenuItem("Level Loader/Load Map 2 #&4")]
    public static void LoadMap2()
    {
        LoadScene("Assets/_Scenes/Map2.unity");
    }


    private static void LoadScene(string scenePath)
    {
        // Save just in case
        EditorSceneManager.SaveOpenScenes();
        // Load the given scene
        EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
    }
}
