using System.Collections;
using System.Collections.Generic;
using OpenAI;
using UnityEngine;

[CreateAssetMenu(fileName = "ChatGPTSettings", menuName = "ChatGPTSettings")]
public class ChatGPTRequestSettings : ScriptableObject
{
    public List<ChatMessage> rules = new List<ChatMessage>();
    public List<ChatMessage> conversationExample = new List<ChatMessage>();
    public List<ChatMessage> conversationHistory = new List<ChatMessage>();
}