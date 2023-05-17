using System.Collections;
using System.Collections.Generic;
using OpenAI;
using TMPro;
using UnityEngine;

public class ChatGPTRequest : MonoBehaviour
{
    public TextMeshProUGUI msg;

    [SerializeField] private int maxHistoryStorage = 30;

    private Queue<string> history = new Queue<string>();
    private List<ChatMessage> messages = new List<ChatMessage>();
    private OpenAIApi openai = new OpenAIApi();

    private const string prompt = "You are an english coach and that's it, you don't know anything else. Don't say you are an AI, just try to act as a human english coach";

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

            msg.text = message.Content;

            messages.Add(message);
        }
        else
        {
            Debug.LogWarning("No text was generated from this prompt.");
        }

        Debug.Log("it was a success");
        SpeechToText.Instance.isProcessingData = false;
    }

    private void Start()
    {
        SpeechToText.Instance.textGiven += SendRequest;
    }

    private void OnDisable()
    {
        SpeechToText.Instance.textGiven -= SendRequest;
    }
}