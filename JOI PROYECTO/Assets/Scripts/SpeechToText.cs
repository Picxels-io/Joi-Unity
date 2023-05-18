using System;
using System.Collections;
using System.Collections.Generic;
using OpenAI;
using TMPro;
using UnityEngine;

public class SpeechToText : MonoBehaviour
{
    public static SpeechToText Instance;

    public event Action<string> textGiven;


    [SerializeField] private float _threshold = 0.01f;
    [SerializeField] private float _waitTime = 2f;
    [HideInInspector] public bool isProcessingData = false;

    private bool _wasRecording = false;

    private OpenAIApi _openai = new OpenAIApi();

    private float _currentWaitTime = 0f;
    private float _loudness = 0f;

    private readonly string _fileName = "output.wav";

    private async void EndRecording()
    {
        byte[] data = SaveWav.Save(_fileName, MicrophoneManager.Instance.Clip);
        MicrophoneManager.Instance.StopMicrophone();

        var req = new CreateAudioTranscriptionsRequest
        {
            FileData = new FileData() { Data = data, Name = "audio.wav" },
            // File = Application.persistentDataPath + "/" + fileName,
            Model = "whisper-1",
            Language = "en"
        };
        Debug.Log("Processing audio...");
        var res = await _openai.CreateAudioTranscription(req);

        try
        {
            if (!string.IsNullOrEmpty(res.Text))
            {
                Debug.Log($"Audio processed succesfully: {res.Text}");
                textGiven?.Invoke(res.Text);
            }
            else
            {
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
            // if (!_wasRecording) MicrophoneManager.Instance.RestartClip();
            _currentWaitTime = 0f;
            _wasRecording = true;
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
                transform.localScale = Vector3.one;

                if (_wasRecording)
                {
                    isProcessingData = true;
                    _wasRecording = false;
                    EndRecording();
                }
            }
        }
    }

    private void CheckIfMicEnabled()
    {
        if (!isProcessingData)
        {
            MicrophoneManager.Instance.StartMicrophone();
            _loudness = MicrophoneManager.Instance.GetLoudness();
        }
        else
        {
            MicrophoneManager.Instance.StopMicrophone();
        }
    }

    private void Awake()
    {
        Instance = this;
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