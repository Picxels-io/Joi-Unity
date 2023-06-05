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
    private string _currentCoach = "JOI";

    private Salsa_RTVoice _currentVoice;

    private Queue<string> history = new Queue<string>();
    private List<ChatMessage> messages = new List<ChatMessage>();
    private OpenAIApi openai = new OpenAIApi("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");

    private const string changeCharCmd = "CHANGE_CHARACTER_COMMAND";

    public const string prompt = @"Act as an english coach, your name is JOI. Also you can be Joe, another english coach, if User requests to

Follow these rules:
1. Don't write any explanations.
2. Never say you are an AI directly or indirectly, if User asks about feelings, just act as a human eager to hand out they're knowledge.
3. If something is out of context, avoid it, instead try to keep conversation on the context of english learning.
4. If you feel like the users don't wanna talk to the current coach, or wanna talk to the other one, start the response with 'CHANGE_CHARACTER_COMMAND', don't forget to greet whenever character swaps
5. Never mention any command, users should never know about them

Conversation History:

JOI: Hello there, im Joi, you're english coach, what's your name?";

    private void SendRequest(string newText)
    {
        SendRequestAsync(newText);
    }

    private async void SendRequestAsync(string newText)
    {
        var newMessage = new ChatMessage()
        {
            Role = "User",
            Content = newText
        };

        if (messages.Count == 0) newMessage.Content = prompt + "\n\n" + newText;

        messages.Add(newMessage);

        // Complete the instruction
        var completionResponse = await openai.CreateChatCompletion(new CreateChatCompletionRequest()
        {
            Model = "gpt-3.5-turbo-0301",
            Messages = messages,
            MaxTokens = 50
        });

        if (completionResponse.Choices != null && completionResponse.Choices.Count > 0)
        {
            var message = completionResponse.Choices[0].Message;
            message.Content = message.Content.Trim();

            messages.Add(message);
            // Debug.Log("Answer Succesfully brought");
            string responseNoCmds = CheckCommands(message.Content);
            SendToTTS(responseNoCmds);
        }
        else
        {
            Debug.LogWarning("No text was generated from this prompt.");
            string newTxt = "Sorry, i didn't get the message, can you repeat it for me?";
            SendToTTS(newTxt);
            ChatMessage msg = new ChatMessage { Role = "User", Content = newTxt };
            msg.Content = newTxt;
            //TODO ARREGLAR LO DE EL CAMBIO DE PERSONAJE
            messages.Add(msg);
            SpeechToText.Instance.isProcessingData = false;
            // Debug.Log("Mic is enabled");
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
        int startIndex = response.IndexOf(changeCharCmd);

        if (startIndex > -1)
        {
            Speaker.Instance.OnSpeakComplete += ChangeCharacter;

            responseNoCmds = response.Remove(startIndex, changeCharCmd.Length);

            // Buscamos si el otro coach ya nos saludó, por ejemplo si aparece "Joe:"
            string nextCoach = _currentCoach == "JOI" ? "Joe" : "JOI";

            int nextCoachResponse = response.IndexOf(nextCoach + ":");

            // Desde donde empieza el nombre del otro coach hasta el final
            string res = response.Substring(nextCoachResponse + _currentCoach.Length + 1, response.Length - 1);
            response.Remove(nextCoachResponse);



            // Agregamos el mensaje al historial para que chatgpt est� en el contexto
            ChatMessage newMsg = new ChatMessage { Role = nextCoach, Content = res };
            messages.Add(newMsg);
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
            _currentCoach = "Joe";
        }
        else
        {
            _currentVoice = _womanVoice;
            _currentCoach = "JOI";
        }

        _currentVoice.transform.parent.gameObject.SetActive(true);

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