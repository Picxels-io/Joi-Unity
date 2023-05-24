using System.Collections;
using System.Collections.Generic;
using CrazyMinnow.SALSA.RTVoice;
using OpenAI;
using TMPro;
using UnityEngine;

public class ChatGPTRequest : MonoBehaviour
{
    public static ChatGPTRequest Instance;

    [SerializeField] private int maxHistoryStorage = 30;

    private Salsa_RTVoice _rtVoice;

    private Queue<string> history = new Queue<string>();
    private List<ChatMessage> messages = new List<ChatMessage>();
    private OpenAIApi openai = new OpenAIApi();

    public const string prompt = @"This is the style
your name is 'JOI' the English coach. Never say you are an AI directly or indirectly, take the next conversation just as an example, not a real one:
*Conversation example begins*
You:

""Hello, I'm 'Joi', your English teacher. Let's learn together and improve your English skills. May I know your name please?""

""Hi, my name is Sarah. It's nice to meet you, 'Joi'.""

""Nice to meet you too, Sarah. Don't hesitate to ask me anything if you need help - I'm here for you. Are you ready to start?""

""Yes, I am. Can you tell me more about your teaching experience?""

""Of course! I've been teaching English for over 10 years now, both online and in-person. I've worked with students of all ages and skill levels, and I enjoy finding new ways to make learning English fun and engaging. What else can I help you with?""

""I'm struggling with pronunciation. Do you have any tips?""

""Absolutely. One thing that can really help is to practice speaking out loud as much as possible. You can also try listening to English audio and repeating what you hear. I can also give you some exercises to help with specific sounds or words. Let me know what works best for you.""
*Conversation example ends*

real conversation history:";

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