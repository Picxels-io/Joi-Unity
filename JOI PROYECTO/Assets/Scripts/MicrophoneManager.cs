using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MicrophoneManager : MonoBehaviour
{
    public static MicrophoneManager Instance;

    // Solo para el metodo suave
    [SerializeField] private float smoothness = 5f;

    private AudioClip _clip;
    private string _mic;
    private int _sampleResolution = 512;

    private float[] _samples;
    private float _currentLoudness = 0f;

    public AudioClip Clip { get => _clip; }

    public float GetLoudness()
    {
        int currentPos = Microphone.GetPosition(_mic);

        // Obtenemos los ultimos samples en la pista de audio
        int startPos = currentPos - _sampleResolution;
        if (startPos < 0) return 0;

        _clip.GetData(_samples, startPos);

        // Calculamos un promedio de ruido en dichos samples, esto para evitar que el microfono sea tan sensible, por eso no se escoge solo el último
        float loudness = 0f;
        foreach (float sample in _samples)
        {
            loudness += Mathf.Abs(sample);
        }

        return loudness / _sampleResolution;
    }

    public float GetSmoothLoudness()
    {
        _currentLoudness = Mathf.Lerp(_currentLoudness, GetLoudness(), smoothness * Time.deltaTime);

        return _currentLoudness;
    }

    public bool IsRecording()
    {
        return Microphone.IsRecording(_mic);
    }

    public void StartMicrophone()
    {
        if (!Microphone.IsRecording(_mic))
        {
            _clip = Microphone.Start(_mic, true, 20, AudioSettings.outputSampleRate);
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
        _clip = Microphone.Start(_mic, true, 20, AudioSettings.outputSampleRate);
    }

    private void Awake()
    {
        Instance = this;
        _samples = new float[_sampleResolution];
    }

    private void Start()
    {
        _clip = Microphone.Start(_mic, true, 20, AudioSettings.outputSampleRate);
    }
}