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

    private void Awake()
    {
        _video = GetComponent<VideoPlayer>();
    }

    private void LateUpdate()
    {
        if (_video.isPrepared && !_video.isPlaying && !_changingScene)
        {
            _changingScene = true;
            SceneManager.LoadScene(nextScene);
        }
    }
}