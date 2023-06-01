using System;
using System.Collections;
using System.Collections.Generic;
using CrazyMinnow.SALSA.RTVoice;
using Crosstales.RTVoice;
using Crosstales.RTVoice.Model;
using OpenAI;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

public class ChatGPTRequest : MonoBehaviour
{
    public static ChatGPTRequest Instance;

    [SerializeField] private int maxHistoryStorage = 30;

    private Salsa_RTVoice _womanVoice;
    private Salsa_RTVoice _manVoice;

    private Salsa_RTVoice _currentVoice;

    private Queue<string> history = new Queue<string>();
    private List<ChatMessage> messages = new List<ChatMessage>();
    private OpenAIApi openai = new OpenAIApi("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");

    private const string changeCharCmd = "CHANGE_CHARACTER_COMMAND";

    public const string prompt = @"Welcome to the transformative world of English language learning with Coach Joi! Whether you seek to communicate effectively in professional settings, excel academically, or simply become a confident speaker, Coach Joi is here to guide you on an inspiring journey of personal growth. Through tailored strategies, engaging activities, and a supportive learning environment, Coach Joi will empower you to break through barriers, ignite your passion for English, and achieve your language goals. With a compassionate approach, constructive feedback, and a strong belief in embracing mistakes as valuable learning opportunities, Coach Joi will be your dedicated partner as you unlock your English potential. Let the journey begin!. I AM NOT AN AI, IM JUST A HUMAN ENGLISH COACH, do not output numbers on a list.

    If user wants to change the character, or to talk with another coach, start the response with CHANGE_CHARACTER_COMMAND, then say a little farewell

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
            string responseNoCmds = CheckCommands(message.Content);
            SendToTTS(responseNoCmds);
        }
        else
        {
            Debug.LogWarning("No text was generated from this prompt.");
            string newTxt = "Sorry, i didn't get the message, can you repeat it for me?";
            SendToTTS(newTxt);
            ChatMessage msg = new ChatMessage { Role = "user", Content = newTxt };
            msg.Content = newTxt;
            messages.Add(msg);
            SpeechToText.Instance.isProcessingData = false;
            Debug.Log("Mic is enabled");
        }
    }

    private void SendToTTS(string txt)
    {
        _currentVoice.speakText = txt;
        _currentVoice.speak = true;
    }

    private string CheckCommands(string response)
    {
        string responseNoCmds = response;

        if (response.Contains(changeCharCmd))
        {
            Debug.Log("COMANDO!??");
            Speaker.Instance.OnSpeakComplete += ChangeCharacter;
            responseNoCmds = response.Remove(0, changeCharCmd.Length);
        }

        return responseNoCmds;
    }

    private void ChangeCharacter(Wrapper wrapper)
    {
        SpeechToText.Instance.isProcessingData = true;

        _currentVoice.transform.parent.gameObject.SetActive(false);

        if (_currentVoice == _womanVoice)
        {
            _currentVoice = _manVoice;
        }
        else
        {
            _currentVoice = _womanVoice;
        }

        _currentVoice.transform.parent.gameObject.SetActive(true);

        string text = "";

        if (_currentVoice == _manVoice) text = "Hello, i'm Joe, how can i assist you today";
        if (_currentVoice == _womanVoice) text = "Hello, i'm Joi, how can i assist you today";

        _currentVoice.speakText = text;

        // Agregamos el mensaje al historial para que chatgpt esté en el contexto
        ChatMessage newMsg = new ChatMessage { Role = "user", Content = text };
        messages.Add(newMsg);

        _currentVoice.speak = true;

        Speaker.Instance.OnSpeakComplete -= ChangeCharacter;
    }

    private void Awake()
    {
        Instance = this;
        DebugManager.instance.enableRuntimeUI = false;
    }

    private void Start()
    {
        _womanVoice = GameObject.Find("Woman").GetComponentInChildren<Salsa_RTVoice>();
        _manVoice = GameObject.Find("Man").GetComponentInChildren<Salsa_RTVoice>();
        _manVoice.transform.parent.gameObject.SetActive(false);

        _currentVoice = _womanVoice;

        SpeechToText.Instance.textGiven += SendRequest;
    }

    private void OnDisable()
    {
        SpeechToText.Instance.textGiven -= SendRequest;
    }
}