using System.Collections;
using System.Collections.Generic;
using CrazyMinnow.SALSA.RTVoice;
using OpenAI;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

public class ChatGPTRequest : MonoBehaviour
{
    public static ChatGPTRequest Instance;

    [SerializeField] private int maxHistoryStorage = 30;

    private Salsa_RTVoice _rtVoice;

    private Queue<string> history = new Queue<string>();
    private List<ChatMessage> messages = new List<ChatMessage>();
    private OpenAIApi openai = new OpenAIApi("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");

    public const string prompt = @"Welcome to the transformative world of English language learning with Coach Joi! Whether you seek to communicate effectively in professional settings, excel academically, or simply become a confident speaker, Coach Joi is here to guide you on an inspiring journey of personal growth. Through tailored strategies, engaging activities, and a supportive learning environment, Coach Joi will empower you to break through barriers, ignite your passion for English, and achieve your language goals. With a compassionate approach, constructive feedback, and a strong belief in embracing mistakes as valuable learning opportunities, Coach Joi will be your dedicated partner as you unlock your English potential. Let the journey begin!. I AM NOT AN AI, IM JUST A HUMAN ENGLISH COACH

    conversation history:
    Hello there! I'm Joi, your english coach, What's your name?";

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

        if (messages.Count == 0) newMessage.Content = prompt + "\n\n" + newText;

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
            SendToTTS("Sorry, i didn't get the message, can you repeat it for me?");
            SpeechToText.Instance.isProcessingData = false;
            Debug.Log("Mic is enabled");
        }
    }

    private void SendToTTS(string txt)
    {
        _rtVoice.speakText = txt;
        _rtVoice.speak = true;
    }

    private void Awake()
    {
        Instance = this;
        DebugManager.instance.enableRuntimeUI = false;
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