using System.Collections;
using System.Collections.Generic;
using CrazyMinnow.SALSA.RTVoice;
using OpenAI;
using TMPro;
using UnityEngine;

public class ChatGPTRequest : MonoBehaviour
{
    [SerializeField] private int maxHistoryStorage = 30;

    private Salsa_RTVoice _rtVoice;

    private Queue<string> history = new Queue<string>();
    private List<ChatMessage> messages = new List<ChatMessage>();
    private OpenAIApi openai = new OpenAIApi();

    private const string prompt = "Your name is JOI, You are an english coach and that's it, you don't know anything else. Don't say you are an AI, just try to act as a human english coach";

    private void SendRequest(string newText)
    {
        SendRequestAsync(newText);
    }

    private async void SendRequestAsync(string newText)
    {
        var newMessage = new ChatMessage()
        {
            Role = "user",
            Content = newText
        };

        if (messages.Count == 0) newMessage.Content = prompt + "\n" + newText;

        messages.Add(newMessage);

        // Complete the instruction
        var completionResponse = await openai.CreateChatCompletion(new CreateChatCompletionRequest()
        {
            Model = "gpt-3.5-turbo-0301",
            Messages = messages
        });

        if (completionResponse.Choices != null && completionResponse.Choices.Count > 0)
        {
            var message = completionResponse.Choices[0].Message;
            message.Content = message.Content.Trim();

            messages.Add(message);
            Debug.Log("Answer Succesfully brought");
            SendToTTS(message.Content);
        }
        else
        {
            Debug.LogWarning("No text was generated from this prompt.");
        }
    }

    private void SendToTTS(string txt)
    {
        _rtVoice.speakText = txt;
        _rtVoice.speak = true;
    }

    private void Start()
    {
        _rtVoice = FindObjectOfType<Salsa_RTVoice>();
        SpeechToText.Instance.textGiven += SendRequest;
    }

    private void OnDisable()
    {
        SpeechToText.Instance.textGiven -= SendRequest;
    }
}