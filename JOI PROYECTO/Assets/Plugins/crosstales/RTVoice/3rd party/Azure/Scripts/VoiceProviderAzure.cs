using UnityEngine;
using System.Collections;
using System.Linq;

namespace Crosstales.RTVoice.Azure
{
   /// <summary>Azure (Bing Speech) voice provider.</summary>
   [HelpURL("https://crosstales.com/media/data/assets/rtvoice/api/class_crosstales_1_1_r_t_voice_1_1_azure_1_1_voice_provider_azure.html")]
   //[ExecuteInEditMode]
   public class VoiceProviderAzure : Crosstales.RTVoice.Provider.BaseCustomVoiceProvider
   {
      #region Variables

      [UnityEngine.Serialization.FormerlySerializedAsAttribute("APIKey")] [Header("Azure Connection"), Tooltip("API-key to access Azure."), SerializeField]
      private string apiKey = string.Empty;

      [UnityEngine.Serialization.FormerlySerializedAsAttribute("Endpoint")] [Tooltip("Endpoint to access Azure."), SerializeField]
      private string endpoint = "https://westus.api.cognitive.microsoft.com/sts/v1.0/issueToken";

      [UnityEngine.Serialization.FormerlySerializedAsAttribute("RequestUri")] [Tooltip("Request URI associated with the API-key."), SerializeField]
      private string requestUri = "https://westus.tts.speech.microsoft.com/cognitiveservices/v1";


      [UnityEngine.Serialization.FormerlySerializedAsAttribute("SampleRate")] [Header("Voice Settings"), Tooltip("Desired sample rate in Hz (default: 24000)."), SerializeField]
      private SampleRate sampleRate = SampleRate._24000Hz;

      private string accessToken;
#if NET_4_6 || NET_STANDARD_2_0
      private bool isReady;
#endif

      private bool isLoading;

      #endregion


      #region Properties

      /// <summary>API-key to access Azure.</summary>
#if CT_DEVELOP
      public string APIKey
      {
         get => string.IsNullOrEmpty(apiKey) ? APIKeys.Azure : apiKey;
         set => apiKey = value;
      }
#else
      public string APIKey
      {
         get => apiKey;
         set => apiKey = value;
      }
#endif
      /// <summary>Endpoint to access Azure.</summary>
      public string Endpoint
      {
         get => endpoint;
         set => endpoint = value;
      }

      /// <summary>Request URI associated with the API-key.</summary>
      public string RequestUri
      {
         get => requestUri;
         set => requestUri = value;
      }

      /// <summary>Desired sample rate in Hz.</summary>
      public SampleRate SampleRate
      {
         get => sampleRate;
         set => sampleRate = value;
      }

      public override string AudioFileExtension => ".wav";

      public override AudioType AudioFileType => AudioType.WAV;

      public override string DefaultVoiceName => "JessaRUS";

      public override bool isWorkingInEditor => false;

      public override bool isWorkingInPlaymode => true;

      public override bool isPlatformSupported => !Crosstales.RTVoice.Util.Helper.isWebPlatform;

      public override int MaxTextLength => 256000;

      public override bool isSpeakNativeSupported => false;

      public override bool isSpeakSupported => true;

      public override bool isSSMLSupported => true;

      public override bool isOnlineService => true;

      public override bool hasCoRoutines => true;

      public override bool isIL2CPPSupported => true;

      public override bool hasVoicesInEditor => true;

      public override int MaxSimultaneousSpeeches => 0;

      /// <summary>Indicates if the API key is valid.</summary>
      /// <returns>True if the API key is valid.</returns>
      public bool isValidAPIKey => APIKey?.Length >= 32;

      /// <summary>Indicates if the endpoint is valid.</summary>
      /// <returns>True if the endpoint is valid.</returns>
      public bool isValidEndpoint => !string.IsNullOrEmpty(endpoint) && endpoint.Contains("api.cognitive.microsoft.com");

      /// <summary>Indicates if the request URI is valid.</summary>
      /// <returns>True if the request URI is valid.</returns>
      public bool isValidRequestUri => !string.IsNullOrEmpty(requestUri) && requestUri.Contains("tts.speech.microsoft.com");

      #endregion


      #region MonoBehaviour methods

#if CT_DEVELOP
      private void Awake()
      {
         Endpoint = "https://westeurope.api.cognitive.microsoft.com/sts/v1.0/issuetoken";
         RequestUri = "https://westeurope.tts.speech.microsoft.com/cognitiveservices/v1";
      }
#endif

      protected override void Start()
      {
         base.Start();
#if NET_4_6 || NET_STANDARD_2_0
         StartCoroutine(resetConnection());
#else
         Debug.LogError("'VoiceProviderAzure' is only supported in .NET4.x or .NET Standard 2.0!", this);
#endif
      }

      #endregion


      #region Implemented methods

      public override void Load(bool forceReload = false)
      {
         if (!isLoading)
         {
            isLoading = true;

            if (forceReload)
               cachedVoices.Clear();

            Invoke(nameof(load), 0.1f);
         }
      }

      public override IEnumerator Generate(Crosstales.RTVoice.Model.Wrapper wrapper)
      {
#if !UNITY_WEBGL
#if NET_4_6 || NET_STANDARD_2_0
         if (!isReady)
            yield return connect(wrapper);

         if (!isReady)
         {
            Debug.LogWarning("Not connected to Azure! Did you enter the correct API-key?", this);
         }
         else
         {
            if (wrapper == null)
            {
               Debug.LogWarning("'wrapper' is null!", this);
            }
            else
            {
               if (string.IsNullOrEmpty(wrapper.Text))
               {
                  Debug.LogWarning("'wrapper.Text' is null or empty!", this);
               }
               else
               {
                  if (!Crosstales.Common.Util.NetworkHelper.isInternetAvailable)
                  {
                     const string errorMessage = "Internet is not available - can't use Azure right now!";
                     Debug.LogError(errorMessage, this);
                     onErrorInfo(wrapper, errorMessage);
                  }
                  else
                  {
                     yield return null; //return to the main process (uid)
                     silence = false;
                     bool success = false;

                     onSpeakAudioGenerationStart(wrapper);

                     Crosstales.RTVoice.Azure.Synthesize synthesizer = new Crosstales.RTVoice.Azure.Synthesize();
                     string outputFile = getOutputFile(wrapper.Uid, Crosstales.Common.Util.BaseHelper.isWebPlatform);

                     System.Threading.Tasks.Task<System.IO.Stream> speakTask = synthesizer.Speak(System.Threading.CancellationToken.None, new Crosstales.RTVoice.Azure.Synthesize.InputOptions
                     {
                        RequestUri = new System.Uri(requestUri),

                        Text = prepareText(wrapper),
                        VoiceType = getVoiceGender(wrapper),
                        Locale = getVoiceCulture(wrapper),
                        VoiceName = getVoiceID(wrapper),
                        OutputFormat = sampleRate == SampleRate._16000Hz ? Crosstales.RTVoice.Azure.AudioOutputFormat.Riff16Khz16BitMonoPcm : Crosstales.RTVoice.Azure.AudioOutputFormat.Riff24Khz16BitMonoPcm,
                        AuthorizationToken = "Bearer " + accessToken
                     });

                     do
                     {
                        yield return null;
                     } while (!speakTask.IsCompleted);

                     try
                     {
                        System.IO.File.WriteAllBytes(outputFile, speakTask.Result.CTReadFully());
                        success = true;
                     }
                     catch (System.Exception ex)
                     {
                        string errorMessage = "Could not create output file: " + outputFile + System.Environment.NewLine + "Error: " + ex;
                        Debug.LogError(errorMessage, this);
                        onErrorInfo(wrapper, errorMessage);
                     }

                     if (success)
                        processAudioFile(wrapper, outputFile);
                  }
               }
            }
         }
#else
         Debug.LogError("'Generate' is only supported in .NET4.x or .NET Standard 2.0!", this);
         yield return null;
#endif
#else
         Debug.LogError("'Generate' is not supported under WebGL!", this);
         yield return null;
#endif
      }

      public override IEnumerator SpeakNative(Crosstales.RTVoice.Model.Wrapper wrapper)
      {
#if NET_4_6 || NET_STANDARD_2_0
         yield return speak(wrapper, true);
#else
         Debug.LogError("'SpeakNative' is only supported in .NET4.x or .NET Standard 2.0!", this);
         yield return null;
#endif
      }

      public override IEnumerator Speak(Crosstales.RTVoice.Model.Wrapper wrapper)
      {
#if NET_4_6 || NET_STANDARD_2_0
         yield return speak(wrapper, false);
#else
         Debug.LogError("'Speak' is only supported in .NET4.x or .NET Standard 2.0!", this);
         yield return null;
#endif
      }

      #endregion


      #region Private methods

      private void load()
      {
         if (cachedVoices?.Count == 0)
         {
            System.Collections.Generic.List<Crosstales.RTVoice.Model.Voice> voices = new System.Collections.Generic.List<Crosstales.RTVoice.Model.Voice>
            {
               new Crosstales.RTVoice.Model.Voice("Hoda", "Microsoft Server Speech Text to Speech Voice (ar-EG, Hoda)", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "adult", "ar-EG", "Microsoft Server Speech Text to Speech Voice (ar-EG, Hoda)"),
               new Crosstales.RTVoice.Model.Voice("Naayf", "Microsoft Server Speech Text to Speech Voice (ar-SA, Naayf)", Crosstales.RTVoice.Model.Enum.Gender.MALE, "adult", "ar-SA", "Microsoft Server Speech Text to Speech Voice (ar-SA, Naayf)"),
               new Crosstales.RTVoice.Model.Voice("Ivan", "Microsoft Server Speech Text to Speech Voice (bg-BG, Ivan)", Crosstales.RTVoice.Model.Enum.Gender.MALE, "adult", "bg-BG", "Microsoft Server Speech Text to Speech Voice (bg-BG, Ivan)"),
               new Crosstales.RTVoice.Model.Voice("HerenaRUS", "Microsoft Server Speech Text to Speech Voice (ca-ES, HerenaRUS)", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "adult", "ca-ES", "Microsoft Server Speech Text to Speech Voice (ca-ES, HerenaRUS)"),
               new Crosstales.RTVoice.Model.Voice("Jakub", "Microsoft Server Speech Text to Speech Voice (cs-CZ, Jakub)", Crosstales.RTVoice.Model.Enum.Gender.MALE, "adult", "cs-CZ", "Microsoft Server Speech Text to Speech Voice (cs-CZ, Jakub)"),
               new Crosstales.RTVoice.Model.Voice("HelleRUS", "Microsoft Server Speech Text to Speech Voice (da-DK, HelleRUS)", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "adult", "da-DK", "Microsoft Server Speech Text to Speech Voice (da-DK, HelleRUS)"),
               new Crosstales.RTVoice.Model.Voice("Michael", "Microsoft Server Speech Text to Speech Voice (de-AT, Michael)", Crosstales.RTVoice.Model.Enum.Gender.MALE, "adult", "de-AT", "Microsoft Server Speech Text to Speech Voice (de-AT, Michael)"),
               new Crosstales.RTVoice.Model.Voice("Karsten", "Microsoft Server Speech Text to Speech Voice (de-CH, Karsten)", Crosstales.RTVoice.Model.Enum.Gender.MALE, "adult", "de-CH", "Microsoft Server Speech Text to Speech Voice (de-CH, Karsten)"),
               new Crosstales.RTVoice.Model.Voice("Hedda", "Microsoft Server Speech Text to Speech Voice (de-DE, Hedda)", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "adult", "de-DE", "Microsoft Server Speech Text to Speech Voice (de-DE, Hedda)"),
               new Crosstales.RTVoice.Model.Voice("HeddaRUS", "Microsoft Server Speech Text to Speech Voice (de-DE, HeddaRUS)", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "adult", "de-DE", "Microsoft Server Speech Text to Speech Voice (de-DE, HeddaRUS)"),
               new Crosstales.RTVoice.Model.Voice("Stefan-Apollo", "Microsoft Server Speech Text to Speech Voice (de-DE, Stefan, Apollo)", Crosstales.RTVoice.Model.Enum.Gender.MALE, "adult", "de-DE", "Microsoft Server Speech Text to Speech Voice (de-DE, Stefan, Apollo)"),
               new Crosstales.RTVoice.Model.Voice("Stefanos", "Microsoft Server Speech Text to Speech Voice (el-GR, Stefanos)", Crosstales.RTVoice.Model.Enum.Gender.MALE, "adult", "el-GR", "Microsoft Server Speech Text to Speech Voice (el-GR, Stefanos)"),
               new Crosstales.RTVoice.Model.Voice("Catherine", "Microsoft Server Speech Text to Speech Voice (en-AU, Catherine)", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "adult", "en-AU", "Microsoft Server Speech Text to Speech Voice (en-AU, Catherine)"),
               new Crosstales.RTVoice.Model.Voice("HayleyRUS", "Microsoft Server Speech Text to Speech Voice (en-AU, HayleyRUS)", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "adult", "en-AU", "Microsoft Server Speech Text to Speech Voice (en-AU, HayleyRUS)"),
               new Crosstales.RTVoice.Model.Voice("Linda", "Microsoft Server Speech Text to Speech Voice (en-CA, Linda)", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "adult", "en-CA", "Microsoft Server Speech Text to Speech Voice (en-CA, Linda)"),
               new Crosstales.RTVoice.Model.Voice("HeatherRUS", "Microsoft Server Speech Text to Speech Voice (en-CA, HeatherRUS)", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "adult", "en-CA", "Microsoft Server Speech Text to Speech Voice (en-CA, HeatherRUS)"),
               new Crosstales.RTVoice.Model.Voice("Susan-Apollo", "Microsoft Server Speech Text to Speech Voice (en-GB, Susan, Apollo)", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "adult", "en-GB", "Microsoft Server Speech Text to Speech Voice (en-GB, Susan, Apollo)"),
               new Crosstales.RTVoice.Model.Voice("HazelRUS", "Microsoft Server Speech Text to Speech Voice (en-GB, HazelRUS)", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "adult", "en-GB", "Microsoft Server Speech Text to Speech Voice (en-GB, HazelRUS)"),
               new Crosstales.RTVoice.Model.Voice("George-Apollo", "Microsoft Server Speech Text to Speech Voice (en-GB, George, Apollo)", Crosstales.RTVoice.Model.Enum.Gender.MALE, "adult", "en-GB", "Microsoft Server Speech Text to Speech Voice (en-GB, George, Apollo)"),
               new Crosstales.RTVoice.Model.Voice("Sean", "Microsoft Server Speech Text to Speech Voice (en-IE, Sean)", Crosstales.RTVoice.Model.Enum.Gender.MALE, "adult", "en-IE", "Microsoft Server Speech Text to Speech Voice (en-IE, Sean)"),
               new Crosstales.RTVoice.Model.Voice("Heera-Apollo", "Microsoft Server Speech Text to Speech Voice (en-IN, Heera, Apollo)", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "adult", "en-IN", "Microsoft Server Speech Text to Speech Voice (en-IN, Heera, Apollo)"),
               new Crosstales.RTVoice.Model.Voice("PriyaRUS", "Microsoft Server Speech Text to Speech Voice (en-IN, PriyaRUS)", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "adult", "en-IN", "Microsoft Server Speech Text to Speech Voice (en-IN, PriyaRUS)"),
               new Crosstales.RTVoice.Model.Voice("Ravi-Apollo", "Microsoft Server Speech Text to Speech Voice (en-IN, Ravi, Apollo)", Crosstales.RTVoice.Model.Enum.Gender.MALE, "adult", "en-IN", "Microsoft Server Speech Text to Speech Voice (en-IN, Ravi, Apollo)"),
               new Crosstales.RTVoice.Model.Voice("ZiraRUS", "Microsoft Server Speech Text to Speech Voice (en-US, ZiraRUS)", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "adult", "en-US", "Microsoft Server Speech Text to Speech Voice (en-US, ZiraRUS)"),
               new Crosstales.RTVoice.Model.Voice("JessaRUS", "Microsoft Server Speech Text to Speech Voice (en-US, JessaRUS)", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "adult", "en-US", "Microsoft Server Speech Text to Speech Voice (en-US, JessaRUS)"),
               new Crosstales.RTVoice.Model.Voice("BenjaminRUS", "Microsoft Server Speech Text to Speech Voice (en-US, BenjaminRUS)", Crosstales.RTVoice.Model.Enum.Gender.MALE, "adult", "en-US", "Microsoft Server Speech Text to Speech Voice (en-US, BenjaminRUS)"),
               new Crosstales.RTVoice.Model.Voice("Jessa24kRUS", "Microsoft Server Speech Text to Speech Voice (en-US, Jessa24kRUS)", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "adult", "en-US", "Microsoft Server Speech Text to Speech Voice (en-US, Jessa24kRUS)"),
               new Crosstales.RTVoice.Model.Voice("Guy24kRUS", "Microsoft Server Speech Text to Speech Voice (en-US, Guy24kRUS)", Crosstales.RTVoice.Model.Enum.Gender.MALE, "adult", "en-US", "Microsoft Server Speech Text to Speech Voice (en-US, Guy24kRUS)"),
               new Crosstales.RTVoice.Model.Voice("Laura-Apollo", "Microsoft Server Speech Text to Speech Voice (es-ES, Laura, Apollo)", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "adult", "es-ES", "Microsoft Server Speech Text to Speech Voice (es-ES, Laura, Apollo)"),
               new Crosstales.RTVoice.Model.Voice("HelenaRUS", "Microsoft Server Speech Text to Speech Voice (es-ES, HelenaRUS)", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "adult", "es-ES", "Microsoft Server Speech Text to Speech Voice (es-ES, HelenaRUS)"),
               new Crosstales.RTVoice.Model.Voice("Pablo-Apollo", "Microsoft Server Speech Text to Speech Voice (es-ES, Pablo, Apollo)", Crosstales.RTVoice.Model.Enum.Gender.MALE, "adult", "es-ES", "Microsoft Server Speech Text to Speech Voice (es-ES, Pablo, Apollo)"),
               new Crosstales.RTVoice.Model.Voice("HildaRUS", "Microsoft Server Speech Text to Speech Voice (es-MX, HildaRUS)", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "adult", "es-MX", "Microsoft Server Speech Text to Speech Voice (es-MX, HildaRUS)"),
               new Crosstales.RTVoice.Model.Voice("Raul-Apollo", "Microsoft Server Speech Text to Speech Voice (es-MX, Raul, Apollo)", Crosstales.RTVoice.Model.Enum.Gender.MALE, "adult", "es-MX", "Microsoft Server Speech Text to Speech Voice (es-MX, Raul, Apollo)"),
               new Crosstales.RTVoice.Model.Voice("HeidiRUS", "Microsoft Server Speech Text to Speech Voice (fi-FI, HeidiRUS)", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "adult", "fi-FI", "Microsoft Server Speech Text to Speech Voice (fi-FI, HeidiRUS)"),
               new Crosstales.RTVoice.Model.Voice("Caroline", "Microsoft Server Speech Text to Speech Voice (fr-CA, Caroline)", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "adult", "fr-CA", "Microsoft Server Speech Text to Speech Voice (fr-CA, Caroline)"),
               new Crosstales.RTVoice.Model.Voice("HarmonieRUS", "Microsoft Server Speech Text to Speech Voice (fr-CA, HarmonieRUS)", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "adult", "fr-CA", "Microsoft Server Speech Text to Speech Voice (fr-CA, HarmonieRUS)"),
               new Crosstales.RTVoice.Model.Voice("Guillaume", "Microsoft Server Speech Text to Speech Voice (fr-CH, Guillaume)", Crosstales.RTVoice.Model.Enum.Gender.MALE, "adult", "fr-CH", "Microsoft Server Speech Text to Speech Voice (fr-CH, Guillaume)"),
               new Crosstales.RTVoice.Model.Voice("Julie-Apollo", "Microsoft Server Speech Text to Speech Voice (fr-FR, Julie, Apollo)", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "adult", "fr-FR", "Microsoft Server Speech Text to Speech Voice (fr-FR, Julie, Apollo)"),
               new Crosstales.RTVoice.Model.Voice("HortenseRUS", "Microsoft Server Speech Text to Speech Voice (fr-FR, HortenseRUS)", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "adult", "fr-FR", "Microsoft Server Speech Text to Speech Voice (fr-FR, HortenseRUS)"),
               new Crosstales.RTVoice.Model.Voice("Paul-Apollo", "Microsoft Server Speech Text to Speech Voice (fr-FR, Paul, Apollo)", Crosstales.RTVoice.Model.Enum.Gender.MALE, "adult", "fr-FR", "Microsoft Server Speech Text to Speech Voice (fr-FR, Paul, Apollo)"),
               new Crosstales.RTVoice.Model.Voice("Asaf", "Microsoft Server Speech Text to Speech Voice (he-IL, Asaf)", Crosstales.RTVoice.Model.Enum.Gender.MALE, "adult", "he-IL", "Microsoft Server Speech Text to Speech Voice (he-IL, Asaf)"),
               new Crosstales.RTVoice.Model.Voice("Kalpana-Apollo", "Microsoft Server Speech Text to Speech Voice (hi-IN, Kalpana, Apollo)", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "adult", "hi-IN", "Microsoft Server Speech Text to Speech Voice (hi-IN, Kalpana, Apollo)"),
               new Crosstales.RTVoice.Model.Voice("Kalpana", "Microsoft Server Speech Text to Speech Voice (hi-IN, Kalpana)", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "adult", "hi-IN", "Microsoft Server Speech Text to Speech Voice (hi-IN, Kalpana)"),
               new Crosstales.RTVoice.Model.Voice("Hemant", "Microsoft Server Speech Text to Speech Voice (hi-IN, Hemant)", Crosstales.RTVoice.Model.Enum.Gender.MALE, "adult", "hi-IN", "Microsoft Server Speech Text to Speech Voice (hi-IN, Hemant)"),
               new Crosstales.RTVoice.Model.Voice("Matej", "Microsoft Server Speech Text to Speech Voice (hr-HR, Matej)", Crosstales.RTVoice.Model.Enum.Gender.MALE, "adult", "hr-HR", "Microsoft Server Speech Text to Speech Voice (hr-HR, Matej)"),
               new Crosstales.RTVoice.Model.Voice("Szabolcs", "Microsoft Server Speech Text to Speech Voice (hu-HU, Szabolcs)", Crosstales.RTVoice.Model.Enum.Gender.MALE, "adult", "hu-HU", "Microsoft Server Speech Text to Speech Voice (hu-HU, Szabolcs)"),
               new Crosstales.RTVoice.Model.Voice("Andika", "Microsoft Server Speech Text to Speech Voice (id-ID, Andika)", Crosstales.RTVoice.Model.Enum.Gender.MALE, "adult", "id-ID", "Microsoft Server Speech Text to Speech Voice (id-ID, Andika)"),
               new Crosstales.RTVoice.Model.Voice("Cosimo-Apollo", "Microsoft Server Speech Text to Speech Voice (it-IT, Cosimo, Apollo)", Crosstales.RTVoice.Model.Enum.Gender.MALE, "adult", "it-IT", "Microsoft Server Speech Text to Speech Voice (it-IT, Cosimo, Apollo)"),
               new Crosstales.RTVoice.Model.Voice("LuciaRUS", "Microsoft Server Speech Text to Speech Voice (it-IT, LuciaRUS)", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "adult", "it-IT", "Microsoft Server Speech Text to Speech Voice (it-IT, LuciaRUS)"),
               new Crosstales.RTVoice.Model.Voice("Ayumi-Apollo", "Microsoft Server Speech Text to Speech Voice (ja-JP, Ayumi, Apollo)", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "adult", "ja-JP", "Microsoft Server Speech Text to Speech Voice (ja-JP, Ayumi, Apollo)"),
               new Crosstales.RTVoice.Model.Voice("Ichiro-Apollo", "Microsoft Server Speech Text to Speech Voice (ja-JP, Ichiro, Apollo)", Crosstales.RTVoice.Model.Enum.Gender.MALE, "adult", "ja-JP", "Microsoft Server Speech Text to Speech Voice (ja-JP, Ichiro, Apollo)"),
               new Crosstales.RTVoice.Model.Voice("HarukaRUS", "Microsoft Server Speech Text to Speech Voice (ja-JP, HarukaRUS)", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "adult", "ja-JP", "Microsoft Server Speech Text to Speech Voice (ja-JP, HarukaRUS)"),
               new Crosstales.RTVoice.Model.Voice("HeamiRUS", "Microsoft Server Speech Text to Speech Voice (ko-KR, HeamiRUS)", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "adult", "ko-KR", "Microsoft Server Speech Text to Speech Voice (ko-KR, HeamiRUS)"),
               new Crosstales.RTVoice.Model.Voice("Rizwan", "Microsoft Server Speech Text to Speech Voice (ms-MY, Rizwan)", Crosstales.RTVoice.Model.Enum.Gender.MALE, "adult", "ms-MY", "Microsoft Server Speech Text to Speech Voice (ms-MY, Rizwan)"),
               new Crosstales.RTVoice.Model.Voice("HuldaRUS", "Microsoft Server Speech Text to Speech Voice (nb-NO, HuldaRUS)", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "adult", "nb-NO", "Microsoft Server Speech Text to Speech Voice (nb-NO, HuldaRUS)"),
               new Crosstales.RTVoice.Model.Voice("HannaRUS", "Microsoft Server Speech Text to Speech Voice (nl-NL, HannaRUS)", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "adult", "nl-NL", "Microsoft Server Speech Text to Speech Voice (nl-NL, HannaRUS)"),
               new Crosstales.RTVoice.Model.Voice("PaulinaRUS", "Microsoft Server Speech Text to Speech Voice (pl-PL, PaulinaRUS)", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "adult", "pl-PL", "Microsoft Server Speech Text to Speech Voice (pl-PL, PaulinaRUS)"),
               new Crosstales.RTVoice.Model.Voice("HeloisaRUS", "Microsoft Server Speech Text to Speech Voice (pt-BR, HeloisaRUS)", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "adult", "pt-BR", "Microsoft Server Speech Text to Speech Voice (pt-BR, HeloisaRUS)"),
               new Crosstales.RTVoice.Model.Voice("Daniel-Apollo", "Microsoft Server Speech Text to Speech Voice (pt-BR, Daniel, Apollo)", Crosstales.RTVoice.Model.Enum.Gender.MALE, "adult", "pt-BR", "Microsoft Server Speech Text to Speech Voice (pt-BR, Daniel, Apollo)"),
               new Crosstales.RTVoice.Model.Voice("HeliaRUS", "Microsoft Server Speech Text to Speech Voice (pt-PT, HeliaRUS)", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "adult", "pt-PT", "Microsoft Server Speech Text to Speech Voice (pt-PT, HeliaRUS)"),
               new Crosstales.RTVoice.Model.Voice("Andrei", "Microsoft Server Speech Text to Speech Voice (ro-RO, Andrei)", Crosstales.RTVoice.Model.Enum.Gender.MALE, "adult", "ro-RO", "Microsoft Server Speech Text to Speech Voice (ro-RO, Andrei)"),
               new Crosstales.RTVoice.Model.Voice("Irina-Apollo", "Microsoft Server Speech Text to Speech Voice (ru-RU, Irina, Apollo)", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "adult", "ru-RU", "Microsoft Server Speech Text to Speech Voice (ru-RU, Irina, Apollo)"),
               new Crosstales.RTVoice.Model.Voice("Pavel-Apollo", "Microsoft Server Speech Text to Speech Voice (ru-RU, Pavel, Apollo)", Crosstales.RTVoice.Model.Enum.Gender.MALE, "adult", "ru-RU", "Microsoft Server Speech Text to Speech Voice (ru-RU, Pavel, Apollo)"),
               new Crosstales.RTVoice.Model.Voice("EkaterinaRUS", "Microsoft Server Speech Text to Speech Voice (ru-RU, EkaterinaRUS)", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "adult", "ru-RU", "Microsoft Server Speech Text to Speech Voice (ru-RU, EkaterinaRUS)"),
               new Crosstales.RTVoice.Model.Voice("Filip", "Microsoft Server Speech Text to Speech Voice (sk-SK, Filip)", Crosstales.RTVoice.Model.Enum.Gender.MALE, "adult", "sk-SK", "Microsoft Server Speech Text to Speech Voice (sk-SK, Filip)"),
               new Crosstales.RTVoice.Model.Voice("Lado", "Microsoft Server Speech Text to Speech Voice (sl-SI, Lado)", Crosstales.RTVoice.Model.Enum.Gender.MALE, "adult", "sl-SI", "Microsoft Server Speech Text to Speech Voice (sl-SI, Lado)"),
               new Crosstales.RTVoice.Model.Voice("HedvigRUS", "Microsoft Server Speech Text to Speech Voice (sv-SE, HedvigRUS)", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "adult", "sv-SE", "Microsoft Server Speech Text to Speech Voice (sv-SE, HedvigRUS)"),
               new Crosstales.RTVoice.Model.Voice("Valluvar", "Microsoft Server Speech Text to Speech Voice (ta-IN, Valluvar)", Crosstales.RTVoice.Model.Enum.Gender.MALE, "adult", "ta-IN", "Microsoft Server Speech Text to Speech Voice (ta-IN, Valluvar)"),
               new Crosstales.RTVoice.Model.Voice("Chitra", "Microsoft Server Speech Text to Speech Voice (te-IN, Chitra)", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "adult", "te-IN", "Microsoft Server Speech Text to Speech Voice (te-IN, Chitra)"),
               new Crosstales.RTVoice.Model.Voice("Pattara", "Microsoft Server Speech Text to Speech Voice (th-TH, Pattara)", Crosstales.RTVoice.Model.Enum.Gender.MALE, "adult", "th-TH", "Microsoft Server Speech Text to Speech Voice (th-TH, Pattara)"),
               new Crosstales.RTVoice.Model.Voice("SedaRUS", "Microsoft Server Speech Text to Speech Voice (tr-TR, SedaRUS)", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "adult", "tr-TR", "Microsoft Server Speech Text to Speech Voice (tr-TR, SedaRUS)"),
               new Crosstales.RTVoice.Model.Voice("An", "Microsoft Server Speech Text to Speech Voice (vi-VN, An)", Crosstales.RTVoice.Model.Enum.Gender.MALE, "adult", "vi-VN", "Microsoft Server Speech Text to Speech Voice (vi-VN, An)"),
               new Crosstales.RTVoice.Model.Voice("HuihuiRUS", "Microsoft Server Speech Text to Speech Voice (zh-CN, HuihuiRUS)", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "adult", "zh-CN", "Microsoft Server Speech Text to Speech Voice (zh-CN, HuihuiRUS)"),
               new Crosstales.RTVoice.Model.Voice("Yaoyao-Apollo", "Microsoft Server Speech Text to Speech Voice (zh-CN, Yaoyao, Apollo)", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "adult", "zh-CN", "Microsoft Server Speech Text to Speech Voice (zh-CN, Yaoyao, Apollo)"),
               new Crosstales.RTVoice.Model.Voice("Kangkang-Apollo", "Microsoft Server Speech Text to Speech Voice (zh-CN, Kangkang, Apollo)", Crosstales.RTVoice.Model.Enum.Gender.MALE, "adult", "zh-CN", "Microsoft Server Speech Text to Speech Voice (zh-CN, Kangkang, Apollo)"),
               new Crosstales.RTVoice.Model.Voice("Tracy-Apollo", "Microsoft Server Speech Text to Speech Voice (zh-HK, Tracy, Apollo)", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "adult", "zh-HK", "Microsoft Server Speech Text to Speech Voice (zh-HK, Tracy, Apollo)"),
               new Crosstales.RTVoice.Model.Voice("TracyRUS", "Microsoft Server Speech Text to Speech Voice (zh-HK, TracyRUS)", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "adult", "zh-HK", "Microsoft Server Speech Text to Speech Voice (zh-HK, TracyRUS)"),
               new Crosstales.RTVoice.Model.Voice("Danny-Apollo", "Microsoft Server Speech Text to Speech Voice (zh-HK, Danny, Apollo)", Crosstales.RTVoice.Model.Enum.Gender.MALE, "adult", "zh-HK", "Microsoft Server Speech Text to Speech Voice (zh-HK, Danny, Apollo)"),
               new Crosstales.RTVoice.Model.Voice("Yating-Apollo", "Microsoft Server Speech Text to Speech Voice (zh-TW, Yating, Apollo)", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "adult", "zh-TW", "Microsoft Server Speech Text to Speech Voice (zh-TW, Yating, Apollo)"),
               new Crosstales.RTVoice.Model.Voice("HanHanRUS", "Microsoft Server Speech Text to Speech Voice (zh-TW, HanHanRUS)", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "adult", "zh-TW", "Microsoft Server Speech Text to Speech Voice (zh-TW, HanHanRUS)"),
               new Crosstales.RTVoice.Model.Voice("Zhiwei-Apollo", "Microsoft Server Speech Text to Speech Voice (zh-TW, Zhiwei, Apollo)", Crosstales.RTVoice.Model.Enum.Gender.MALE, "adult", "zh-TW", "Microsoft Server Speech Text to Speech Voice (zh-TW, Zhiwei, Apollo)"),
               //neural voices
               new Crosstales.RTVoice.Model.Voice("SalmaNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "adult", "ar-EG", "ar-EG-SalmaNeural"),
               new Crosstales.RTVoice.Model.Voice("ShakirNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.MALE, "adult", "ar-EG", "ar-EG-ShakirNeural"),
               new Crosstales.RTVoice.Model.Voice("ZariyahNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "adult", "ar-SA", "ar-SA-ZariyahNeural"),
               new Crosstales.RTVoice.Model.Voice("HamedNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.MALE, "adult", "ar-SA", "ar-SA-HamedNeural"),
               new Crosstales.RTVoice.Model.Voice("KalinaNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "adult", "bg-BG", "bg-BG-KalinaNeural"),
               new Crosstales.RTVoice.Model.Voice("BorislavNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.MALE, "adult", "bg-BG", "bg-BG-BorislavNeural"),
               new Crosstales.RTVoice.Model.Voice("AlbaNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "adult", "ca-ES", "ca-ES-AlbaNeural"),
               new Crosstales.RTVoice.Model.Voice("JoanaNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "adult", "ca-ES", "ca-ES-JoanaNeural"),
               new Crosstales.RTVoice.Model.Voice("EnricNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.MALE, "adult", "ca-ES", "ca-ES-EnricNeural"),
               new Crosstales.RTVoice.Model.Voice("HiuGaaiNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "adult", "zh-HK", "zh-HK-HiuGaaiNeural"),
               new Crosstales.RTVoice.Model.Voice("HiuMaanNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "adult", "zh-HK", "zh-HK-HiuMaanNeural"),
               new Crosstales.RTVoice.Model.Voice("WanLungNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.MALE, "adult", "zh-HK", "zh-HK-WanLungNeural"),
               new Crosstales.RTVoice.Model.Voice("XiaoxiaoNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "adult", "zh-CN", "zh-CN-XiaoxiaoNeural"),
               new Crosstales.RTVoice.Model.Voice("XiaoyouNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "adult", "zh-CN", "zh-CN-XiaoyouNeural"),
               new Crosstales.RTVoice.Model.Voice("XiaomoNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "adult", "zh-CN", "zh-CN-XiaomoNeural"),
               new Crosstales.RTVoice.Model.Voice("XiaoxuanNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "adult", "zh-CN", "zh-CN-XiaoxuanNeural"),
               new Crosstales.RTVoice.Model.Voice("XiaohanNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "adult", "zh-CN", "zh-CN-XiaohanNeural"),
               new Crosstales.RTVoice.Model.Voice("XiaoruiNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "adult", "zh-CN", "zh-CN-XiaoruiNeural"),
               new Crosstales.RTVoice.Model.Voice("YunyangNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.MALE, "adult", "zh-CN", "zh-CN-YunyangNeural"),
               new Crosstales.RTVoice.Model.Voice("YunyeNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.MALE, "adult", "zh-CN", "zh-CN-YunyeNeural"),
               new Crosstales.RTVoice.Model.Voice("YunxiNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.MALE, "adult", "zh-CN", "zh-CN-YunxiNeural"),
               new Crosstales.RTVoice.Model.Voice("HsiaoChenNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "adult", "zh-TW", "zh-TW-HsiaoChenNeural"),
               new Crosstales.RTVoice.Model.Voice("HsiaoYuNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "adult", "zh-TW", "zh-TW-HsiaoYuNeural"),
               new Crosstales.RTVoice.Model.Voice("YunJheNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.MALE, "adult", "zh-TW", "zh-TW-YunJheNeural"),
               new Crosstales.RTVoice.Model.Voice("GabrijelaNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "adult", "hr-HR", "hr-HR-GabrijelaNeural"),
               new Crosstales.RTVoice.Model.Voice("SreckoNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.MALE, "adult", "hr-HR", "hr-HR-SreckoNeural"),
               new Crosstales.RTVoice.Model.Voice("VlastaNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "adult", "cs-CZ", "cs-CZ-VlastaNeural"),
               new Crosstales.RTVoice.Model.Voice("AntoninNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.MALE, "adult", "cs-CZ", "cs-CZ-AntoninNeural"),
               new Crosstales.RTVoice.Model.Voice("ChristelNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "adult", "da-DK", "da-DK-ChristelNeural"),
               new Crosstales.RTVoice.Model.Voice("JeppeNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.MALE, "adult", "da-DK", "da-DK-JeppeNeural"),
               new Crosstales.RTVoice.Model.Voice("DenaNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "adult", "nl-BE", "nl-BE-DenaNeural"),
               new Crosstales.RTVoice.Model.Voice("ArnaudNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.MALE, "adult", "nl-BE", "nl-BE-ArnaudNeural"),
               new Crosstales.RTVoice.Model.Voice("ColetteNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "adult", "nl-NL", "nl-NL-ColetteNeural"),
               new Crosstales.RTVoice.Model.Voice("FennaNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "adult", "nl-NL", "nl-NL-FennaNeural"),
               new Crosstales.RTVoice.Model.Voice("MaartenNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.MALE, "adult", "nl-NL", "nl-NL-MaartenNeural"),
               new Crosstales.RTVoice.Model.Voice("NatashaNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "adult", "en-AU", "en-AU-NatashaNeural"),
               new Crosstales.RTVoice.Model.Voice("WilliamNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.MALE, "adult", "en-AU", "en-AU-WilliamNeural"),
               new Crosstales.RTVoice.Model.Voice("ClaraNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "adult", "en-CA", "en-CA-ClaraNeural"),
               new Crosstales.RTVoice.Model.Voice("LiamNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.MALE, "adult", "en-CA", "en-CA-LiamNeural"),
               new Crosstales.RTVoice.Model.Voice("YanNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "adult", "en-HK", "en-HK-YanNeural"),
               new Crosstales.RTVoice.Model.Voice("SamNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.MALE, "adult", "en-HK", "en-HK-SamNeural"),
               new Crosstales.RTVoice.Model.Voice("NeerjaNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "adult", "en-IN", "en-IN-NeerjaNeural"),
               new Crosstales.RTVoice.Model.Voice("PrabhatNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.MALE, "adult", "en-IN", "en-IN-PrabhatNeural"),
               new Crosstales.RTVoice.Model.Voice("EmilyNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "adult", "en-IE", "en-IE-EmilyNeural"),
               new Crosstales.RTVoice.Model.Voice("ConnorNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.MALE, "adult", "en-IE", "en-IE-ConnorNeural"),
               new Crosstales.RTVoice.Model.Voice("MollyNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "adult", "en-NZ", "en-NZ-MollyNeural"),
               new Crosstales.RTVoice.Model.Voice("MitchellNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.MALE, "adult", "en-NZ", "en-NZ-MitchellNeural"),
               new Crosstales.RTVoice.Model.Voice("RosaNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "adult", "en-PH", "en-PH-RosaNeural"),
               new Crosstales.RTVoice.Model.Voice("JamesNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.MALE, "adult", "en-PH", "en-PH-JamesNeural"),
               new Crosstales.RTVoice.Model.Voice("LunaNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "adult", "en-SG", "en-SG-LunaNeural"),
               new Crosstales.RTVoice.Model.Voice("WayneNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.MALE, "adult", "en-SG", "en-SG-WayneNeural"),
               new Crosstales.RTVoice.Model.Voice("LeahNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "adult", "en-ZA", "en-ZA-LeahNeural"),
               new Crosstales.RTVoice.Model.Voice("LukeNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.MALE, "adult", "en-ZA", "en-ZA-LukeNeural"),
               new Crosstales.RTVoice.Model.Voice("LibbyNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "adult", "en-GB", "en-GB-LibbyNeural"),
               new Crosstales.RTVoice.Model.Voice("SoniaNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "adult", "en-GB", "en-GB-SoniaNeural"),
               new Crosstales.RTVoice.Model.Voice("RyanNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.MALE, "adult", "en-GB", "en-GB-RyanNeural"),
               new Crosstales.RTVoice.Model.Voice("AriaNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "adult", "en-US", "en-US-AriaNeural"),
               new Crosstales.RTVoice.Model.Voice("JennyNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "adult", "en-US", "en-US-JennyNeural"),
               new Crosstales.RTVoice.Model.Voice("GuyNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.MALE, "adult", "en-US", "en-US-GuyNeural"),
               new Crosstales.RTVoice.Model.Voice("SaraNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "adult", "en-US", "en-US-SaraNeural"),
               new Crosstales.RTVoice.Model.Voice("AmberNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "adult", "en-US", "en-US-AmberNeural"),
               new Crosstales.RTVoice.Model.Voice("AshleyNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "adult", "en-US", "en-US-AshleyNeural"),
               new Crosstales.RTVoice.Model.Voice("CoraNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "adult", "en-US", "en-US-CoraNeural"),
               new Crosstales.RTVoice.Model.Voice("ElizabethNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "adult", "en-US", "en-US-ElizabethNeural"),
               new Crosstales.RTVoice.Model.Voice("MichelleNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "adult", "en-US", "en-US-MichelleNeural"),
               new Crosstales.RTVoice.Model.Voice("MonicaNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "adult", "en-US", "en-US-MonicaNeural"),
               new Crosstales.RTVoice.Model.Voice("AnaNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "child", "en-US", "en-US-AnaNeural"),
               new Crosstales.RTVoice.Model.Voice("BrandonNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.MALE, "adult", "en-US", "en-US-BrandonNeural"),
               new Crosstales.RTVoice.Model.Voice("ChristopherNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.MALE, "adult", "en-US", "en-US-ChristopherNeural"),
               new Crosstales.RTVoice.Model.Voice("JacobNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.MALE, "adult", "en-US", "en-US-JacobNeural"),
               new Crosstales.RTVoice.Model.Voice("EricNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.MALE, "adult", "en-US", "en-US-EricNeural"),
               new Crosstales.RTVoice.Model.Voice("AnuNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "adult", "et-EE", "et-EE-AnuNeural"),
               new Crosstales.RTVoice.Model.Voice("KertNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.MALE, "adult", "et-EE", "et-EE-KertNeural"),
               new Crosstales.RTVoice.Model.Voice("NooraNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "adult", "fi-FI", "fi-FI-NooraNeural"),
               new Crosstales.RTVoice.Model.Voice("SelmaNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "adult", "fi-FI", "fi-FI-SelmaNeural"),
               new Crosstales.RTVoice.Model.Voice("HarriNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.MALE, "adult", "fi-FI", "fi-FI-HarriNeural"),
               new Crosstales.RTVoice.Model.Voice("CharlineNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "adult", "fr-BE", "fr-BE-CharlineNeural"),
               new Crosstales.RTVoice.Model.Voice("GerardNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.MALE, "adult", "fr-BE", "fr-BE-GerardNeural"),
               new Crosstales.RTVoice.Model.Voice("SylvieNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "adult", "fr-CA", "fr-CA-SylvieNeural"),
               new Crosstales.RTVoice.Model.Voice("AntoineNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.MALE, "adult", "fr-CA", "fr-CA-AntoineNeural"),
               new Crosstales.RTVoice.Model.Voice("JeanNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.MALE, "adult", "fr-CA", "fr-CA-JeanNeural"),
               new Crosstales.RTVoice.Model.Voice("DeniseNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "adult", "fr-FR", "fr-FR-DeniseNeural"),
               new Crosstales.RTVoice.Model.Voice("HenriNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.MALE, "adult", "fr-FR", "fr-FR-HenriNeural"),
               new Crosstales.RTVoice.Model.Voice("ArianeNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "adult", "fr-CH", "fr-CH-ArianeNeural"),
               new Crosstales.RTVoice.Model.Voice("FabriceNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.MALE, "adult", "fr-CH", "fr-CH-FabriceNeural"),
               new Crosstales.RTVoice.Model.Voice("IngridNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "adult", "de-AT", "de-AT-IngridNeural"),
               new Crosstales.RTVoice.Model.Voice("JonasNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.MALE, "adult", "de-AT", "de-AT-JonasNeural"),
               new Crosstales.RTVoice.Model.Voice("KatjaNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "adult", "de-DE", "de-DE-KatjaNeural"),
               new Crosstales.RTVoice.Model.Voice("ConradNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.MALE, "adult", "de-DE", "de-DE-ConradNeural"),
               new Crosstales.RTVoice.Model.Voice("LeniNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "adult", "de-CH", "de-CH-LeniNeural"),
               new Crosstales.RTVoice.Model.Voice("JanNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.MALE, "adult", "de-CH", "de-CH-JanNeural"),
               new Crosstales.RTVoice.Model.Voice("AthinaNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "adult", "el-GR", "el-GR-AthinaNeural"),
               new Crosstales.RTVoice.Model.Voice("NestorasNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.MALE, "adult", "el-GR", "el-GR-NestorasNeural"),
               new Crosstales.RTVoice.Model.Voice("DhwaniNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "adult", "gu-IN", "gu-IN-DhwaniNeural"),
               new Crosstales.RTVoice.Model.Voice("NiranjanNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.MALE, "adult", "gu-IN", "gu-IN-NiranjanNeural"),
               new Crosstales.RTVoice.Model.Voice("HilaNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "adult", "he-IL", "he-IL-HilaNeural"),
               new Crosstales.RTVoice.Model.Voice("AvriNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.MALE, "adult", "he-IL", "he-IL-AvriNeural"),
               new Crosstales.RTVoice.Model.Voice("SwaraNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "adult", "hi-IN", "hi-IN-SwaraNeural"),
               new Crosstales.RTVoice.Model.Voice("MadhurNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.MALE, "adult", "hi-IN", "hi-IN-MadhurNeural"),
               new Crosstales.RTVoice.Model.Voice("NoemiNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "adult", "hu-HU", "hu-HU-NoemiNeural"),
               new Crosstales.RTVoice.Model.Voice("TamasNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.MALE, "adult", "hu-HU", "hu-HU-TamasNeural"),
               new Crosstales.RTVoice.Model.Voice("GadisNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "adult", "id-ID", "id-ID-GadisNeural"),
               new Crosstales.RTVoice.Model.Voice("ArdiNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.MALE, "adult", "id-ID", "id-ID-ArdiNeural"),
               new Crosstales.RTVoice.Model.Voice("OrlaNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "adult", "ga-IE", "ga-IE-OrlaNeural"),
               new Crosstales.RTVoice.Model.Voice("ColmNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.MALE, "adult", "ga-IE", "ga-IE-ColmNeural"),
               new Crosstales.RTVoice.Model.Voice("ElsaNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "adult", "it-IT", "it-IT-ElsaNeural"),
               new Crosstales.RTVoice.Model.Voice("IsabellaNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "adult", "it-IT", "it-IT-IsabellaNeural"),
               new Crosstales.RTVoice.Model.Voice("DiegoNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.MALE, "adult", "it-IT", "it-IT-DiegoNeural"),
               new Crosstales.RTVoice.Model.Voice("NanamiNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "adult", "ja-JP", "ja-JP-NanamiNeural"),
               new Crosstales.RTVoice.Model.Voice("KeitaNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.MALE, "adult", "ja-JP", "ja-JP-KeitaNeural"),
               new Crosstales.RTVoice.Model.Voice("SunHiNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "adult", "ko-KR", "ko-KR-SunHiNeural"),
               new Crosstales.RTVoice.Model.Voice("InJoonNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.MALE, "adult", "ko-KR", "ko-KR-InJoonNeural"),
               new Crosstales.RTVoice.Model.Voice("EveritaNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "adult", "lv-LV", "lv-LV-EveritaNeural"),
               new Crosstales.RTVoice.Model.Voice("NilsNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.MALE, "adult", "lv-LV", "lv-LV-NilsNeural"),
               new Crosstales.RTVoice.Model.Voice("OnaNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "adult", "lt-LT", "lt-LT-OnaNeural"),
               new Crosstales.RTVoice.Model.Voice("LeonasNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.MALE, "adult", "lt-LT", "lt-LT-LeonasNeural"),
               new Crosstales.RTVoice.Model.Voice("YasminNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "adult", "ms-MY", "ms-MY-YasminNeural"),
               new Crosstales.RTVoice.Model.Voice("OsmanNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.MALE, "adult", "ms-MY", "ms-MY-OsmanNeural"),
               new Crosstales.RTVoice.Model.Voice("GraceNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "adult", "mt-MT", "mt-MT-GraceNeural"),
               new Crosstales.RTVoice.Model.Voice("JosephNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.MALE, "adult", "mt-MT", "mt-MT-JosephNeural"),
               new Crosstales.RTVoice.Model.Voice("AarohiNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "adult", "mr-IN", "mr-IN-AarohiNeural"),
               new Crosstales.RTVoice.Model.Voice("ManoharNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.MALE, "adult", "mr-IN", "mr-IN-ManoharNeural"),
               new Crosstales.RTVoice.Model.Voice("IselinNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "adult", "nb-NO", "nb-NO-IselinNeural"),
               new Crosstales.RTVoice.Model.Voice("PernilleNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "adult", "nb-NO", "nb-NO-PernilleNeural"),
               new Crosstales.RTVoice.Model.Voice("FinnNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.MALE, "adult", "nb-NO", "nb-NO-FinnNeural"),
               new Crosstales.RTVoice.Model.Voice("AgnieszkaNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "adult", "pl-PL", "pl-PL-AgnieszkaNeural"),
               new Crosstales.RTVoice.Model.Voice("ZofiaNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "adult", "pl-PL", "pl-PL-ZofiaNeural"),
               new Crosstales.RTVoice.Model.Voice("MarekNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.MALE, "adult", "pl-PL", "pl-PL-MarekNeural"),
               new Crosstales.RTVoice.Model.Voice("FranciscaNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "adult", "pt-BR", "pt-BR-FranciscaNeural"),
               new Crosstales.RTVoice.Model.Voice("AntonioNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.MALE, "adult", "pt-BR", "pt-BR-AntonioNeural"),
               new Crosstales.RTVoice.Model.Voice("FernandaNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "adult", "pt-PT", "pt-PT-FernandaNeural"),
               new Crosstales.RTVoice.Model.Voice("RaquelNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "adult", "pt-PT", "pt-PT-RaquelNeural"),
               new Crosstales.RTVoice.Model.Voice("DuarteNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.MALE, "adult", "pt-PT", "pt-PT-DuarteNeural"),
               new Crosstales.RTVoice.Model.Voice("AlinaNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "adult", "ro-RO", "ro-RO-AlinaNeural"),
               new Crosstales.RTVoice.Model.Voice("EmilNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.MALE, "adult", "ro-RO", "ro-RO-EmilNeural"),
               new Crosstales.RTVoice.Model.Voice("DariyaNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "adult", "ru-RU", "ru-RU-DariyaNeural"),
               new Crosstales.RTVoice.Model.Voice("SvetlanaNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "adult", "ru-RU", "ru-RU-SvetlanaNeural"),
               new Crosstales.RTVoice.Model.Voice("DmitryNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.MALE, "adult", "ru-RU", "ru-RU-DmitryNeural"),
               new Crosstales.RTVoice.Model.Voice("ViktoriaNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "adult", "sk-SK", "sk-SK-ViktoriaNeural"),
               new Crosstales.RTVoice.Model.Voice("LukasNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.MALE, "adult", "sk-SK", "sk-SK-LukasNeural"),
               new Crosstales.RTVoice.Model.Voice("PetraNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "adult", "sl-SI", "sl-SI-PetraNeural"),
               new Crosstales.RTVoice.Model.Voice("RokNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.MALE, "adult", "sl-SI", "sl-SI-RokNeural"),
               new Crosstales.RTVoice.Model.Voice("ElenaNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "adult", "es-AR", "es-AR-ElenaNeural"),
               new Crosstales.RTVoice.Model.Voice("TomasNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.MALE, "adult", "es-AR", "es-AR-TomasNeural"),
               new Crosstales.RTVoice.Model.Voice("SalomeNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "adult", "es-CO", "es-CO-SalomeNeural"),
               new Crosstales.RTVoice.Model.Voice("GonzaloNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.MALE, "adult", "es-CO", "es-CO-GonzaloNeural"),
               new Crosstales.RTVoice.Model.Voice("DaliaNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "adult", "es-MX", "es-MX-DaliaNeural"),
               new Crosstales.RTVoice.Model.Voice("JorgeNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.MALE, "adult", "es-MX", "es-MX-JorgeNeural"),
               new Crosstales.RTVoice.Model.Voice("ElviraNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "adult", "es-ES", "es-ES-ElviraNeural"),
               new Crosstales.RTVoice.Model.Voice("AlvaroNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.MALE, "adult", "es-ES", "es-ES-AlvaroNeural"),
               new Crosstales.RTVoice.Model.Voice("PalomaNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "adult", "es-US", "es-US-PalomaNeural"),
               new Crosstales.RTVoice.Model.Voice("AlonsoNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.MALE, "adult", "es-US", "es-US-AlonsoNeural"),
               new Crosstales.RTVoice.Model.Voice("ZuriNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "adult", "sw-KE", "sw-KE-ZuriNeural"),
               new Crosstales.RTVoice.Model.Voice("RafikiNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.MALE, "adult", "sw-KE", "sw-KE-RafikiNeural"),
               new Crosstales.RTVoice.Model.Voice("HilleviNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "adult", "sv-SE", "sv-SE-HilleviNeural"),
               new Crosstales.RTVoice.Model.Voice("SofieNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "adult", "sv-SE", "sv-SE-SofieNeural"),
               new Crosstales.RTVoice.Model.Voice("MattiasNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.MALE, "adult", "sv-SE", "sv-SE-MattiasNeural"),
               new Crosstales.RTVoice.Model.Voice("PallaviNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "adult", "ta-IN", "ta-IN-PallaviNeural"),
               new Crosstales.RTVoice.Model.Voice("ValluvarNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.MALE, "adult", "ta-IN", "ta-IN-ValluvarNeural"),
               new Crosstales.RTVoice.Model.Voice("ShrutiNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "adult", "te-IN", "te-IN-ShrutiNeural"),
               new Crosstales.RTVoice.Model.Voice("MohanNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.MALE, "adult", "te-IN", "te-IN-MohanNeural"),
               new Crosstales.RTVoice.Model.Voice("AcharaNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "adult", "th-TH", "th-TH-AcharaNeural"),
               new Crosstales.RTVoice.Model.Voice("PremwadeeNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "adult", "th-TH", "th-TH-PremwadeeNeural"),
               new Crosstales.RTVoice.Model.Voice("NiwatNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.MALE, "adult", "th-TH", "th-TH-NiwatNeural"),
               new Crosstales.RTVoice.Model.Voice("EmelNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "adult", "tr-TR", "tr-TR-EmelNeural"),
               new Crosstales.RTVoice.Model.Voice("AhmetNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.MALE, "adult", "tr-TR", "tr-TR-AhmetNeural"),
               new Crosstales.RTVoice.Model.Voice("PolinaNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "adult", "uk-UA", "uk-UA-PolinaNeural"),
               new Crosstales.RTVoice.Model.Voice("OstapNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.MALE, "adult", "uk-UA", "uk-UA-OstapNeural"),
               new Crosstales.RTVoice.Model.Voice("UzmaNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "adult", "ur-PK", "ur-PK-UzmaNeural"),
               new Crosstales.RTVoice.Model.Voice("AsadNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.MALE, "adult", "ur-PK", "ur-PK-AsadNeural"),
               new Crosstales.RTVoice.Model.Voice("HoaiMyNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "adult", "vi-VN", "vi-VN-HoaiMyNeural"),
               new Crosstales.RTVoice.Model.Voice("NamMinhNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.MALE, "adult", "vi-VN", "vi-VN-NamMinhNeural"),
               new Crosstales.RTVoice.Model.Voice("NiaNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "adult", "cy-GB", "cy-GB-NiaNeural"),
               new Crosstales.RTVoice.Model.Voice("AledNeural", "Microsoft Azure neural voice", Crosstales.RTVoice.Model.Enum.Gender.MALE, "adult", "cy-GB", "cy-GB-AledNeural"),
               new Crosstales.RTVoice.Model.Voice("JennyMultilingualNeural", "Microsoft Azure neural voice (preview)", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "adult", "en-US", "en-US-JennyMultilingualNeural"),
               new Crosstales.RTVoice.Model.Voice("XiaochenNeural", "Microsoft Azure neural voice (preview)", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "adult", "zh-CN", "zh-CN-XiaochenNeural"),
               new Crosstales.RTVoice.Model.Voice("XiaoyanNeural", "Microsoft Azure neural voice (preview)", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "adult", "zh-CN", "zh-CN-XiaoyanNeural"),
               new Crosstales.RTVoice.Model.Voice("XiaoshuangNeural", "Microsoft Azure neural voice (preview)", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "adult", "zh-CN", "zh-CN-XiaoshuangNeural"),
               new Crosstales.RTVoice.Model.Voice("XiaoqiuNeural", "Microsoft Azure neural voice (preview)", Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "adult", "zh-CN", "zh-CN-XiaoqiuNeural"),
            };

#if NET_4_6 || NET_STANDARD_2_0
            isReady = false;
#endif

            cachedVoices = voices.OrderBy(s => s.Name).ToList();
         }

         onVoicesReady();

         isLoading = false;
      }

#if NET_4_6 || NET_STANDARD_2_0
      private IEnumerator resetConnection()
      {
         //refresh the connection every 9.5min
         WaitForSeconds wfs = new WaitForSeconds(60 * 9.5f);

         while (true)
         {
            yield return wfs;
            //Debug.LogWarning("resetConnection", this);
            isReady = false;
         }
      }

      private IEnumerator connect(Crosstales.RTVoice.Model.Wrapper wrapper)
      {
         //Debug.LogWarning("connect", this);

         isReady = false;

         if (!isValidAPIKey)
         {
            const string errorMessage = "Please add a valid 'API Key' to access Azure!";
            Debug.LogError(errorMessage, this);
            onErrorInfo(wrapper, errorMessage);
         }
         else if (!isValidRequestUri)
         {
            const string errorMessage = "Please add a valid 'Request URI' to access Azure!";
            Debug.LogError(errorMessage, this);
            onErrorInfo(wrapper, errorMessage);
         }
         else if (!isValidEndpoint)
         {
            const string errorMessage = "Please add a valid 'Endpoint' to access Azure!";
            Debug.LogError(errorMessage, this);
            onErrorInfo(wrapper, errorMessage);
         }
         else
         {
            if (!Crosstales.Common.Util.NetworkHelper.isInternetAvailable)
            {
               const string errorMessage = "Internet is not available - can't use Azure right now!";
               Debug.LogError(errorMessage, this);
               onErrorInfo(wrapper, errorMessage);
            }
            else
            {
#if (!UNITY_WSA && !UNITY_XBOXONE) || UNITY_EDITOR
               System.Net.ServicePointManager.ServerCertificateValidationCallback = Crosstales.Common.Util.NetworkHelper.RemoteCertificateValidationCallback;
#endif
               Crosstales.RTVoice.Azure.Authentication auth = new Crosstales.RTVoice.Azure.Authentication();
               System.Threading.Tasks.Task<string> authenticating = auth.Authenticate(endpoint, APIKey);

               yield return authenticateSpeechService(authenticating);
            }
         }
      }

      private IEnumerator authenticateSpeechService(System.Threading.Tasks.Task<string> authenticating)
      {
         // Yield control back to the main thread as long as the task is still running
         while (!authenticating.IsCompleted)
         {
            yield return null;
         }

         try
         {
            accessToken = authenticating.Result;

            isReady = true;

            if (string.IsNullOrEmpty(accessToken))
            {
               isReady = false;
               Debug.LogError("No valid token received; are the settings for Azure correct?", this);
            }
            else if (accessToken.Contains("error"))
            {
               isReady = false;
               Debug.LogError("No valid token received: " + accessToken, this);
            }

            if (Crosstales.RTVoice.Util.Config.DEBUG)
               Debug.Log("Token: " + accessToken, this);
         }
         catch (System.Exception ex)
         {
            Debug.LogError("Failed authentication: " + ex, this);
         }
      }

      private IEnumerator speak(Crosstales.RTVoice.Model.Wrapper wrapper, bool isNative)
      {
         if (!isReady)
            yield return connect(wrapper);

         if (!isReady)
         {
            Debug.LogWarning("Not connected to Azure! Did you enter the correct API-key?", this);
         }
         else
         {
            if (wrapper == null)
            {
               Debug.LogWarning("'wrapper' is null!", this);
            }
            else
            {
               if (string.IsNullOrEmpty(wrapper.Text))
               {
                  Debug.LogWarning("'wrapper.Text' is null or empty!", this);
               }
               else
               {
                  if (!Crosstales.Common.Util.NetworkHelper.isInternetAvailable)
                  {
                     const string errorMessage = "Internet is not available - can't use Azure right now!";
                     Debug.LogError(errorMessage, this);
                     onErrorInfo(wrapper, errorMessage);
                  }
                  else
                  {
                     yield return null; //return to the main process (uid)
                     silence = false;
                     bool success = false;

                     if (!isNative)
                        onSpeakAudioGenerationStart(wrapper);

                     Crosstales.RTVoice.Azure.Synthesize synthesizer = new Crosstales.RTVoice.Azure.Synthesize();
                     string outputFile = getOutputFile(wrapper.Uid, Crosstales.RTVoice.Util.Helper.isWebPlatform);

                     System.Threading.Tasks.Task<System.IO.Stream> speakTask = synthesizer.Speak(System.Threading.CancellationToken.None, new Crosstales.RTVoice.Azure.Synthesize.InputOptions
                     {
                        RequestUri = new System.Uri(requestUri),

                        Text = prepareText(wrapper),
                        VoiceType = getVoiceGender(wrapper),
                        Locale = getVoiceCulture(wrapper),
                        VoiceName = getVoiceID(wrapper),
                        OutputFormat = sampleRate == SampleRate._16000Hz ? Crosstales.RTVoice.Azure.AudioOutputFormat.Riff16Khz16BitMonoPcm : Crosstales.RTVoice.Azure.AudioOutputFormat.Riff24Khz16BitMonoPcm,
                        AuthorizationToken = "Bearer " + accessToken
                     });

                     do
                     {
                        yield return null;
                     } while (!speakTask.IsCompleted);
#if UNITY_WEBGL
                     AudioClip ac = Crosstales.Common.Audio.WavMaster.ToAudioClip(speakTask.Result.CTReadFully());
                     yield return playAudioFile(wrapper, ac, isNative);
#else
                     try
                     {
                        System.IO.File.WriteAllBytes(outputFile, speakTask.Result.CTReadFully());
                        success = true;
                     }
                     catch (System.Exception ex)
                     {
                        string errorMessage = "Could not create output file: " + outputFile + System.Environment.NewLine + "Error: " + ex;
                        Debug.LogError(errorMessage, this);
                        onErrorInfo(wrapper, errorMessage);
                     }

                     if (success)
                        yield return playAudioFile(wrapper, Crosstales.Common.Util.NetworkHelper.ValidURLFromFilePath(outputFile), outputFile, AudioFileType, isNative);
#endif
                  }
               }
            }
         }
      }

      private string getVoiceID(Crosstales.RTVoice.Model.Wrapper wrapper)
      {
         if (wrapper != null && string.IsNullOrEmpty(wrapper.Voice?.Identifier))
         {
            if (Crosstales.RTVoice.Util.Config.DEBUG)
               Debug.LogWarning("'wrapper.Voice' or 'wrapper.Voice.Identifier' is null! Using the OS 'default' voice.", this);

            return Crosstales.RTVoice.Speaker.Instance.VoiceForName(DefaultVoiceName).Identifier;
         }

         return wrapper != null ? wrapper.Voice?.Identifier : Crosstales.RTVoice.Speaker.Instance.VoiceForName(DefaultVoiceName).Identifier;
      }

      private string getVoiceCulture(Crosstales.RTVoice.Model.Wrapper wrapper)
      {
         if (string.IsNullOrEmpty(wrapper.Voice?.Culture))
         {
            if (Crosstales.RTVoice.Util.Config.DEBUG)
               Debug.LogWarning("'wrapper.Voice' or 'wrapper.Voice.Culture' is null! Using the 'default' English voice.", this);

            //always use English as fallback
            return "en-US";
         }

         return wrapper.Voice?.Culture;
      }

      private Crosstales.RTVoice.Model.Enum.Gender getVoiceGender(Crosstales.RTVoice.Model.Wrapper wrapper)
      {
         if (wrapper.Voice == null)
         {
            if (Crosstales.RTVoice.Util.Config.DEBUG)
               Debug.LogWarning("'wrapper.Voice' is null! Using the 'default' Female voice.", this);

            //always use a Female voice as fallback
            return Crosstales.RTVoice.Model.Enum.Gender.FEMALE;
         }

         return wrapper.Voice.Gender;
      }

      private static string prepareText(Crosstales.RTVoice.Model.Wrapper wrapper)
      {
         System.Text.StringBuilder sbXML = new System.Text.StringBuilder();

         if (Mathf.Abs(wrapper.Rate - 1f) > Crosstales.Common.Util.BaseConstants.FLOAT_TOLERANCE || Mathf.Abs(wrapper.Pitch - 1f) > Crosstales.Common.Util.BaseConstants.FLOAT_TOLERANCE || Mathf.Abs(wrapper.Volume - 1f) > Crosstales.Common.Util.BaseConstants.FLOAT_TOLERANCE)
         {
            sbXML.Append("<prosody");

            if (Mathf.Abs(wrapper.Rate - 1f) > Crosstales.Common.Util.BaseConstants.FLOAT_TOLERANCE)
            {
               float _rate = wrapper.Rate > 1 ? (wrapper.Rate - 1f) * 0.5f : wrapper.Rate - 1f;

               sbXML.Append(" rate=\"");
               sbXML.Append(_rate >= 0f
                  ? _rate.ToString("+#0%", Crosstales.RTVoice.Util.Helper.BaseCulture)
                  : _rate.ToString("#0%", Crosstales.RTVoice.Util.Helper.BaseCulture));

               sbXML.Append("\"");
            }

            if (Mathf.Abs(wrapper.Pitch - 1f) > Crosstales.Common.Util.BaseConstants.FLOAT_TOLERANCE)
            {
               float _pitch = wrapper.Pitch - 1f;

               sbXML.Append(" pitch=\"");
               sbXML.Append(_pitch >= 0f
                  ? _pitch.ToString("+#0%", Crosstales.RTVoice.Util.Helper.BaseCulture)
                  : _pitch.ToString("#0%", Crosstales.RTVoice.Util.Helper.BaseCulture));

               sbXML.Append("\"");
            }

            if (Mathf.Abs(wrapper.Volume - 1f) > Crosstales.Common.Util.BaseConstants.FLOAT_TOLERANCE)
            {
               sbXML.Append(" volume=\"");
               sbXML.Append((100 * wrapper.Volume).ToString("#0", Crosstales.RTVoice.Util.Helper.BaseCulture));

               sbXML.Append("\"");
            }

            sbXML.Append(">");

            sbXML.Append(wrapper.Text);

            sbXML.Append("</prosody>");
         }
         else
         {
            sbXML.Append(getValidXML(wrapper.Text));
         }

         //Debug.Log(sbXML);

         return sbXML.ToString();
      }
#endif

      #endregion


      #region Editor-only methods

#if UNITY_EDITOR
      public override void GenerateInEditor(Crosstales.RTVoice.Model.Wrapper wrapper)
      {
         Debug.LogError("'GenerateInEditor' is not supported for Azure!", this);
      }

      public override void SpeakNativeInEditor(Crosstales.RTVoice.Model.Wrapper wrapper)
      {
         Debug.LogError("'SpeakNativeInEditor' is not supported for Azure!", this);
      }
#endif

      #endregion
   }
}
// © 2019-2023 crosstales LLC (https://www.crosstales.com)