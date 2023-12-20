using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using OpenAI;
using TMPro;
using UnityEngine;

public class SpeechToText : MonoBehaviour
{
    public static SpeechToText Instance;

    public event Action<string> textGiven;

    [SerializeField] private float _threshold = 0.01f;
    [SerializeField] private float _waitTime = 2f;
    [SerializeField] private float _updateLoudnessTime = 0.1f;
    [SerializeField] private GameObject _voiceIndicator;
    [HideInInspector] public bool isProcessingData = true;

    AudioSource source;

    private bool _wasRecording = false;

    private OpenAIApi _openai = new OpenAIApi("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");

    private float _currentWaitTime = 0f;
    private float _loudness = 0f;
    private float _currentTimeSinceLastLoudnessUpdate = 0f;

    private int _recordStartSample = 0;
    private int _currentStartSample = 0;

    // Testing only
    private float timeSinceStartedProcessing = 0f;

    private readonly string _fileName = "output.wav";

    private AudioClip TrimClip(AudioClip clip, int start)
    {
        int clipSamples = clip.samples;
        int clipChannels = clip.channels;
        int clipFreq = clip.frequency;

        start = Math.Clamp(start - 20000, 0, 10000000);

        int length = clip.samples - start;

        // Recolecta la información
        float[] data = new float[clipSamples];

        clip.GetData(data, 0);
        MicrophoneManager.Instance.StopMicrophone();

        // Cambiar a lista para eliminar los primeros samples
        // float[] dataList = new float[length];
        List<float> dataList = new List<float>(data);
        dataList.RemoveRange(0, start);

        AudioClip newClip = AudioClip.Create("NewClip", dataList.Count, clipChannels, clipFreq, false);

        newClip.SetData(dataList.ToArray(), 0);

        return newClip;
    }

    private async void EndRecording(int startTime)
    {
        AudioClip trimmedClip = TrimClip(MicrophoneManager.Instance.Clip, startTime);
        AudioClip newTrimmedClip = SaveWav.TrimSilence(trimmedClip, _threshold);
        // source.clip = newTrimmedClip;
        // source.Play();

        byte[] data = SaveWav.Save(_fileName, newTrimmedClip);

        var req = new CreateAudioTranscriptionsRequest
        {
            FileData = new FileData() { Data = data, Name = "audio.wav" },
            // File = Application.persistentDataPath + "/" + fileName,
            Model = "whisper-1",
            Language = "en",
            Prompt = "You are talking to JOI, an english coach, there's also Joe, another coach",
        };
        // Debug.Log("Processing audio...");
        timeSinceStartedProcessing = Time.time;
        var res = await _openai.CreateAudioTranscription(req);

        try
        {
            if (!string.IsNullOrEmpty(res.Text))
            {
                // Debug.Log($"Audio processed succesfully: {res.Text} : {Time.time - timeSinceStartedProcessing }");
                textGiven?.Invoke(res.Text);
            }
            else
            {
                // Debug.Log($"No text was retrieved");
                isProcessingData = false;
            }
        }
        catch (System.Exception)
        {
            Debug.LogError(res.Error);
            isProcessingData = false;
        }

    }

    private void ReadVoice()
    {
        if (isProcessingData || !MicrophoneManager.Instance.IsRecording()) return;

        // Esta hablando lo suficientemente duro?
        if (_loudness >= _threshold)
        {
            if (!_wasRecording) _currentStartSample = _recordStartSample;
            _currentWaitTime = 0f;
            _wasRecording = true;
            _voiceIndicator.SetActive(true);
        }
        else
        {
            if (_currentWaitTime < _waitTime)
            {
                // WaitTime es para que de una espera despues de que la persona termine de hablar
                _currentWaitTime += Time.deltaTime;
            }
            else
            {
                _voiceIndicator.SetActive(false);

                if (_wasRecording)
                {
                    isProcessingData = true;
                    _wasRecording = false;
                    EndRecording(_currentStartSample);
                }
            }
        }
    }

    private void CheckIfMicEnabled()
    {
        if (!isProcessingData)
        {
            MicrophoneManager.Instance.StartMicrophone();
            if (_currentTimeSinceLastLoudnessUpdate >= _updateLoudnessTime)
            {
                _loudness = MicrophoneManager.Instance.GetSmoothLoudness(out _recordStartSample);
                _currentTimeSinceLastLoudnessUpdate = 0f;
            }
            else
            {
                _currentTimeSinceLastLoudnessUpdate += Time.deltaTime;
            }
        }
        else
        {
            MicrophoneManager.Instance.StopMicrophone();
            _currentTimeSinceLastLoudnessUpdate = 0f;
        }
    }

    private void Awake()
    {
        Instance = this;
        _currentTimeSinceLastLoudnessUpdate = 10000f;
        source = GetComponent<AudioSource>();
        isProcessingData = true;
    }

    private void Update()
    {
        CheckIfMicEnabled();
    }

    private void LateUpdate()
    {
        ReadVoice();
    }
}