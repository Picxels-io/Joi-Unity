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

    private readonly string _fileName = "output.wav";

    private async void EndRecording()
    {
        Microphone.End(null);
        byte[] data = SaveWav.Save(_fileName, MicrophoneManager.Instance.Clip);

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
                // _isProcessingData = false;
                Debug.Log($"Audio processed succesfully: {res.Text}");
                textGiven?.Invoke(res.Text);
            }
        }
        catch (System.Exception)
        {
            Debug.LogError(res.Error);
        }

    }

    private void ReadVoice()
    {
        if (isProcessingData) return;
        MicrophoneManager.Instance.StartMicrophone();

        float loudness = 0f;

        // if (!isProcessingData)
        // {
        //     MicrophoneManager.Instance.StartMicrophone();
        loudness = MicrophoneManager.Instance.GetLoudness();
        // }
        // else
        // {
        //     MicrophoneManager.Instance.StopMicrophone();
        // }

        // Esta hablando lo suficientemente duro?
        if (loudness >= _threshold)
        {
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

    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        ReadVoice();
    }
}
