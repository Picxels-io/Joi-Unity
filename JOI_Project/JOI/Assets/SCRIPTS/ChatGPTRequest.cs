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

    [SerializeField] private ChatGPTRequestSettings _requestSettings;

    private Salsa_RTVoice _womanVoice;
    private Salsa_RTVoice _manVoice;
    private string _currentCoachName = "Joi";

    private Salsa_RTVoice _currentVoice;

    private Queue<string> history = new Queue<string>();
    private List<string> messages = new List<string>();
    private OpenAIApi openai = new OpenAIApi("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");

    private const string CHANGE_CHAR_CMD = "CHANGE_CHARACTER_COMMAND";
    private const string CHANGE_SCENARIO_CMD = "CHANGE_SCENARIO_COMMAND";

    // Quita el nombre del response para que el bot no lo mencione
    private string GetResponseWithNoName(string response, string coachName)
    {
        int startIndex = response.IndexOf(coachName + ":");

        if (startIndex == -1) return "";

        string newResponse = response.Remove(startIndex, coachName.Length + 1);

        return newResponse;
    }

    // Revisa si hay comandos, y devuelve un string sin dichos comandos
    private string CheckCommands(string response)
    {
        string responseNoCmds = response;

        int changeCharacterIndex = response.IndexOf(CHANGE_CHAR_CMD);
        int changeSceneIndex = response.IndexOf(CHANGE_SCENARIO_CMD);

        if (changeCharacterIndex > -1)
        {
            responseNoCmds = responseNoCmds.Remove(changeCharacterIndex, CHANGE_CHAR_CMD.Length);
            responseNoCmds = ChangeCharacterCommandReceived(responseNoCmds, changeCharacterIndex);
        }

        if (changeSceneIndex > -1)
        {
            responseNoCmds = responseNoCmds.Remove(changeSceneIndex, CHANGE_SCENARIO_CMD.Length);
            ChangeScenarioCommandReceived();
        }

        return responseNoCmds;
    }

    private string ChangeCharacterCommandReceived(string response, int commandIndex)
    {
        string newRes = response;

        // Cambiamos el personaje
        string nextCoachName = _currentCoachName == "Joi" ? "Joe" : "Joi";
        Salsa_RTVoice nextVoice = _currentVoice == _womanVoice ? _manVoice : _womanVoice;

        int nextCoachSpeechIndex = newRes.IndexOf(nextCoachName + ":");

        // Asignamos el texto a la siguiente voz
        string nextsSpeech = newRes.Substring(nextCoachSpeechIndex);

        ChatMessage newMsg = new ChatMessage() { Content = nextsSpeech };
        messages.Add(newMsg.Content);

        nextsSpeech = GetResponseWithNoName(nextsSpeech, nextCoachName);
        nextVoice.speakText = nextsSpeech;

        Debug.Log(nextsSpeech);

        // Nos deshacemos del speech del siguiente personaje
        newRes = newRes.Remove(nextCoachSpeechIndex);

        // Creamos el evento para cuando el current coach deje de hablar
        Speaker.Instance.OnSpeakComplete += CurrentCharacterDoneSpeaking;

        return newRes;
    }

    private void CurrentCharacterDoneSpeaking(Wrapper wrapper)
    {
        _currentCoachName = _currentCoachName == "Joi" ? "Joe" : "Joi";

        _currentVoice.transform.parent.gameObject.SetActive(false);
        _currentVoice.speakText = "";
        _currentVoice.audioSrc.clip = null;

        _currentVoice = _currentVoice == _womanVoice ? _manVoice : _womanVoice;
        _currentVoice.transform.parent.gameObject.SetActive(true);

        _currentVoice.speak = true;

        Speaker.Instance.OnSpeakComplete -= CurrentCharacterDoneSpeaking;
    }

    private void ChangeScenarioCommandReceived()
    {

    }

    private void AddRules()
    {
        ChatMessage rulesMsg = new ChatMessage
        {
            Content = "Always follow these rules:\n"
        };

        foreach (ChatMessage msg in _requestSettings.rules)
        {
            rulesMsg.Content += msg.Content + "\n";
        }

        messages.Add(rulesMsg.Content);
    }

    private void AddExample()
    {
        foreach (ChatMessage msg in _requestSettings.conversationExample)
        {
            messages.Add(msg.Content);
        }
    }

    private void AddConversationHistory()
    {
        foreach (ChatMessage msg in _requestSettings.conversationHistory)
        {
            messages.Add(msg.Content);
        }
    }

    private void SendRequest(string newText)
    {
        SendRequestAsync(newText);
    }

    private async void SendRequestAsync(string newText)
    {
        var newMessage = new ChatMessage()
        {
            Content = newText
        };

        // Si la conversación apenas empieza
        if (messages.Count == 0)
        {
            AddRules();
            AddExample();
            AddConversationHistory();
        }

        newMessage.Content = "User: " + newMessage.Content;
        messages.Add(newMessage.Content);

        List<string> list = new List<string>();
        list.Add("Hello");

        // Complete the instruction
        var completionResponse = await openai.CreateChatCompletion(new CreateChatCompletionRequest()
        {
            Model = "text-davinci-003",
            Prompt = messages,
            Temperature = 0.2f,
            N = 1,
        });

        if (completionResponse.Choices != null && completionResponse.Choices.Count > 0)
        {
            string message = completionResponse.Choices[0].Text;
            // message.Content = message.Content.Trim();

            // Debug.Log(message.Content);

            // // Solo hablará el coach actual
            // string resNoCmds = CheckCommands(message.Content);

            // message.Content = resNoCmds;

            // resNoCmds = GetResponseWithNoName(message.Content, _currentCoachName);

            // if (resNoCmds != "")
            // {
            //     messages.Add(message.Content);
            //     SendToTTS(resNoCmds);
            // }
            // else
            // {
            //     string newTxt = "Sorry, i didn't get the message, can you repeat it for me?";
            //     SendToTTS(newTxt);
            //     ChatMessage msg = new ChatMessage { Content = newTxt };
            //     msg.Content = newTxt;
            //     messages.Add(msg.Content);
            // }

            Debug.Log(message);
        }
        else
        {
            Debug.LogWarning("No text was generated from this prompt.");
            string newTxt = "Sorry, i didn't get the message, can you repeat it for me?";
            SendToTTS(newTxt);
            ChatMessage msg = new ChatMessage { Content = newTxt };
            msg.Content = newTxt;
            messages.Add(msg.Content);
        }
    }

    private void SendToTTS(string txt)
    {
        _currentVoice.speakText = txt;
        _currentVoice.speak = true;
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