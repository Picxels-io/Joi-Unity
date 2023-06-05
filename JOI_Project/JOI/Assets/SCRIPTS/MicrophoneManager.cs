using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Android;

public class MicrophoneManager : MonoBehaviour
{
    public static MicrophoneManager Instance;

    // Solo para el metodo suave
    [SerializeField] private float smoothness = 5f;

    private AudioClip _clip;
    private string _mic;
    private int _sampleResolution = 256;

    private float[] _samples;
    private float _currentLoudness = 0f;

    private bool _isMicAuthorized = true;

    public AudioClip Clip { get => _clip; }

    public float GetLoudness(out int startTime)
    {
        int currentPos = Microphone.GetPosition(_mic);

        // Obtenemos los ultimos samples en la pista de audio
        int start = currentPos - _sampleResolution;
        startTime = 0;

        if (start < 0) return 0;

        startTime = start;

        _clip.GetData(_samples, startTime);

        // Calculamos un promedio de ruido en dichos samples, esto para evitar que el microfono sea tan sensible, por eso no se escoge solo el último
        float loudness = 0f;
        foreach (float sample in _samples)
        {
            loudness += Mathf.Abs(sample);
        }

        return loudness / _sampleResolution;
    }

    public float GetSmoothLoudness(out int startTime)
    {
        _currentLoudness = Mathf.Lerp(_currentLoudness, GetLoudness(out startTime), smoothness * Time.deltaTime);

        return _currentLoudness;
    }

    public bool IsRecording()
    {
        return Microphone.IsRecording(_mic);
    }

    public void StartMicrophone()
    {
        if (!Microphone.IsRecording(_mic) && _isMicAuthorized)
        {
            _clip = Microphone.Start(_mic, true, 60, AudioSettings.outputSampleRate);
        }
    }

    public void StopMicrophone()
    {
        if (Microphone.IsRecording(_mic))
        {
            Microphone.End(_mic);
            _clip = null;
        }
    }

    public void RestartClip()
    {
        _clip = null;
        _clip = Microphone.Start(_mic, true, 60, AudioSettings.outputSampleRate);
    }

    private void Awake()
    {
        Instance = this;
        _samples = new float[_sampleResolution];

        // AskPermissions();
    }

#if UNITY_IOS
    private IEnumerator Start()
    {
        _isMicAuthorized = false;

        yield return Application.RequestUserAuthorization(UserAuthorization.Microphone);
        if (Application.HasUserAuthorization(UserAuthorization.Microphone))
        {
            _isMicAuthorized = true;
        }
    }
#endif

    private void AskPermissions()
    {
#if UNITY_ANDROID
        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            Permission.RequestUserPermission(Permission.Microphone);
        }
        if (!Permission.HasUserAuthorizedPermission(Permission.ExternalStorageWrite))
        {
            Permission.RequestUserPermission(Permission.ExternalStorageWrite);
        }
#endif
    }
}