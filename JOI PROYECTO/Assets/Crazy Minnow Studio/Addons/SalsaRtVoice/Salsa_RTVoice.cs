using UnityEngine;
using Crosstales.RTVoice;

namespace CrazyMinnow.SALSA.RTVoice
{
    /// <summary>
    /// SALSA / RT-Voice integration example using the RT-Voice Speaker.Speak method
    /// </summary>
    [AddComponentMenu("Crazy Minnow Studio/SALSA LipSync/Add-ons/Salsa_RTVoice")]
    public class Salsa_RTVoice : MonoBehaviour
    {
        public AudioSource audioSrc; // AudioSource used by SALSA
        public string speakText = "This is a test using SALSA with RT-Voice"; // Text to pass to Speak
        public bool speak; // Inspector button to fire the speak event

        private bool _isSpeaking = false;

        private void SpeakCompleted(Crosstales.RTVoice.Model.Wrapper wrapper)
        {
            SpeechToText.Instance.isProcessingData = false;
            Debug.Log("Mic is enabled");
        }

        /// <summary>
        /// Get the AudioSource component used by SALSA
        /// </summary>
        void Awake()
        {
            if (!audioSrc) audioSrc = GetComponent<AudioSource>();
            Speaker.Instance.OnSpeakComplete += SpeakCompleted;
        }

        private void OnDisable()
        {
            Speaker.Instance.OnSpeakComplete -= SpeakCompleted;
        }

        /// <summary>
        /// This is only used for testing and can be deleted in an implementation where you
        /// make your own call's to [Speaker.Speak]. Click [Speak] in this inspector
        /// to demonstrate send the [speakText] to the [Speaker.SpeakNative] RT-Voice method.
        /// </summary>
        void LateUpdate()
        {
            // Debug.Log(Speaker.Instance.isSpeaking);

            if (speak)
            {
                speak = false;
                _isSpeaking = true;
                Speaker.Instance.Speak(speakText, audioSrc, Speaker.Instance.VoicesForCulture("en")[0]);
            }
        }
    }
}