using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

public class ChangeScene : MonoBehaviour
{
    private VideoPlayer _video;

    public void ChangeToScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    public void ExitApp()
    {
        Application.Quit();
    }
}