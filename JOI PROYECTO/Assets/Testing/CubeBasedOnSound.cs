using System.Collections;
using System.Collections.Generic;
using OpenAI;
using TMPro;
using UnityEngine;

public class CubeBasedOnSound : MonoBehaviour
{
    [SerializeField] private float minScale = 1f;
    [SerializeField] private float maxScale = 10f;
    [SerializeField] private float threshold = 0.1f;
    [SerializeField] private float waitTime = 3f;

    public TextMeshProUGUI message;
    private readonly string fileName = "output.wav";
    private OpenAIApi openai = new OpenAIApi();


    private float currentWaitTime = 0f;

    private Material _mat;

    private void Awake()
    {
        _mat = GetComponent<MeshRenderer>().material;
    }

    private void ReadVoice()
    {
        Debug.Log(MicrophoneManager.Instance.GetLoudness());

        if (MicrophoneManager.Instance.GetLoudness() >= threshold)
        {
            _mat.color = Color.green;
        }
        else
        {
            _mat.color = Color.red;
        }
    }

    private void Update()
    {
        ReadVoice();
    }
}