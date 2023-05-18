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
    private int _recordStartSample = 0;

    // Testing only
    private float timeSinceStartedProcessing = 0f;

    private readonly string _fileName = "output.wav";

    // private void TrimClip(AudioClip clip, int start)
    // {
    //     int clipSamples = clip.samples;
    //     float[] data = new float[clipSamples];

    //     MicrophoneManager.Instance.StopMicrophone();
    //     clip.GetData(data, 0);

    //     float[] newData = new float[clipSamples - start];
    //     Debug.Log("Start copying");
    //     Array.Copy(data, start, newData, 0, clip.samples - start);
    //     Debug.Log("Copied");

    //     clip.SetData(newData, 0);
    // }

    private async void EndRecording(int startTime)
    {
        byte[] data = SaveWav.Save(_fileName, MicrophoneManager.Instance.Clip);

        var req = new CreateAudioTranscriptionsRequest
        {
            FileData = new FileData() { Data = data, Name = "audio.wav" },
            // File = Application.persistentDataPath + "/" + fileName,
            Model = "whisper-1",
            Language = "en",
            Prompt = ChatGPTRequest.prompt
        };
        Debug.Log("Processing audio...");
        timeSinceStartedProcessing = Time.time;
        var res = await _openai.CreateAudioTranscription(req);

        try
        {
            if (!string.IsNullOrEmpty(res.Text))
            {
                Debug.Log($"Audio processed succesfully: {res.Text} : {Time.time - timeSinceStartedProcessing }");
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

        int start = _recordStartSample;

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
                    EndRecording(start);
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