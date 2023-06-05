using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

public class SplashScreen : MonoBehaviour
{
    [SerializeField] private string nextScene;
    private VideoPlayer _video;
    private bool _changingScene = false;

    private void ChangeScene(UnityEngine.Video.VideoPlayer vp)
    {
        _changingScene = true;
        SceneManager.LoadScene(nextScene);
    }

    private void Awake()
    {
        _video = GetComponent<VideoPlayer>();
        _video.loopPointReached += ChangeScene;
    }

    private void OnDisable()
    {
        _video.loopPointReached -= ChangeScene;
    }
}