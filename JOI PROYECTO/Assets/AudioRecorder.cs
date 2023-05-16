/* using UnityEngine;
using System.IO;
using System.Collections;

public class AudioRecorder : MonoBehaviour {

    public AudioClip audioClipx;
    public string recordingPath;
    public string audioFilePath; // The file path to your audio clip
    AudioSource audioSource;
    AudioClip audioClip;
    public string filePath;
 
 
    public float sensitivity = 100.0f;
    public string micDeviceName = null;
    public AudioSource audioSourcexx;

    private AudioClip clipRecord = null;
    private int recordTime = 5;
    private bool isRecording = false;
    private int audioIndex = 1;

 void Start()
    {
        clipRecord = Microphone.Start(null, true, recordTime, AudioSettings.outputSampleRate);
        recordingPath = GetAudioFilePath();
        
    }

void Update() {
     if (Input.GetKeyDown(KeyCode.P)) {
        StartRecording();
    } 

    if (Input.GetKeyDown(KeyCode.Q)) {
        StopRecording();
    }
    
    if (Input.GetKeyDown(KeyCode.X)) {
        PlayAudio();
    }

 /* float[] waveData = new float[clipRecord.samples * clipRecord.channels];
        clipRecord.GetData(waveData, 0);

        float rms = 0;
        foreach (float sample in waveData)
        {
            rms += sample * sample;
        }

        rms = Mathf.Sqrt(rms / (clipRecord.samples * clipRecord.channels));

        if (rms * sensitivity > 1)
        {
           
        } 
    }

   
    public void ToggleRecording()
    {
        if (isRecording)
        {
            StopRecording();
            Debug.Log("StopRecording");

        }
        else
        {
            StartRecording();
            Debug.Log("StartRecording");

        }

        isRecording = !isRecording;
    }



    public void StartRecording() {
        // Start recording audio
       // audioClipx = Microphone.Start(null, true, 10, AudioSettings.outputSampleRate);

        // Create file path for recording
      //  recordingPath = Path.Combine(Application.dataPath + "/Resources"+"/grabacion.wav");
       recordingPath = GetAudioFilePath();

    }

    public void StopRecording() {
        // Stop recording audio
        Microphone.End(null);
        SavWav.Save(recordingPath, clipRecord);
    }
    

          private string GetAudioFilePath() {
        // Agregar el número o timestamp al nombre del archivo
        string fileName = "grabacion_" + audioIndex +  ".wav"; // Puedes utilizar un número o DateTime.Now.ToString() para el timestamp
        audioIndex++; // Incrementar el índice para el próximo archivo

        return Path.Combine(Application.dataPath, "Resources", fileName);
    }

    // Resto del código...


 IEnumerator LoadAudio(string path)
    {
        string audioURL = "file://" + path;
        WWW www = new WWW(audioURL);

        while (!www.isDone)
        {
            yield return null;
        }

        if (!string.IsNullOrEmpty(www.error))
        {
            Debug.LogError("Error loading audio file: " + www.error);
            yield break;
        }

     if (www != null && www.GetAudioClip(false, false, AudioType.WAV) != null)
    {
        audioSource.clip = www.GetAudioClip(false, false, AudioType.WAV);
        audioSource.Play();
    }
    else
    {
        Debug.LogError("Failed to load audio clip from path: " + filePath);
    }
        }


    public void PlayAudio()
    {
        audioSource =GetComponent<AudioSource>();
  if (!System.IO.File.Exists(recordingPath))
        {
            Debug.LogError("Audio file not found at path: " + recordingPath);
            return;
        }

        StartCoroutine(LoadAudio(recordingPath));

    }
    

void OnAudioFilterRead(float[] data, int channels)
    {
        float sum = 0f;
        
        // sum up the audio samples
        for (int i = 0; i < data.Length; i++)
        {
            sum += data[i] * data[i];
        }
        
        // calculate the root mean square of the audio samples
        float rms = Mathf.Sqrt(sum / data.Length);
        
        // if the RMS value is greater than the sensitivity threshold, execute a method
        if (rms > sensitivity)
        {
            StartRecording();
        }
    }
} */

using UnityEngine;
using System.IO;
using System.Collections;

public class AudioRecorder : MonoBehaviour {

    public AudioClip audioClipx;
    public string recordingPath;
    public AudioSource audioSource;
    private bool isRecording = false;
 
    public string audioFilePath; 
    AudioClip audioClip;
    public string filePath;  
    public string micDeviceName = null; 
    public AudioSource audioSourcexx;
    private AudioClip clipRecord = null;
    private float startTime = 0f;
    private int audioIndex = 1;

    void Start()
    {
        recordingPath = GetAudioFilePath();
        audioSource = GameObject.Find("AS").GetComponent<AudioSource>();

    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            if (!isRecording)
                StartRecording();
        }

        if (Input.GetKeyDown(KeyCode.Q))
        {
            if (isRecording)
                StopRecording();
        }

        if (Input.GetKeyDown(KeyCode.X))
        {
            PlayAudio();
        }
    }

    public void StartRecording()
    {
        // Start recording audio
        clipRecord = Microphone.Start(null, true, 10, AudioSettings.outputSampleRate);
        startTime = Time.time;
        isRecording = true;
    }

    public void StopRecording()
    {
        if (isRecording)
        {
            // Stop recording audio
            isRecording = false;
            Microphone.End(null);
            float clipLength = Time.time - startTime;
            AudioClip recordedClip = AudioClip.Create("recordedClip", Mathf.CeilToInt(clipLength * AudioSettings.outputSampleRate), 1, AudioSettings.outputSampleRate, false);
            float[] data = new float[Mathf.CeilToInt(clipLength * AudioSettings.outputSampleRate)];
            clipRecord.GetData(data, 0);
            recordedClip.SetData(data, 0);
            SavWav.Save(recordingPath, recordedClip);
        }
    }

public void ToggleRecording()
{
    if (isRecording)
    {
        StopRecording();
        Debug.Log("StopRecording");
    }
    else
    {
        StartRecording();
        Debug.Log("StartRecording");
    }
}


    private string GetAudioFilePath()
    {
        // Agregar el número o timestamp al nombre del archivo
        string fileName = "grabacion_" + audioIndex + ".wav"; // Puedes utilizar un número o DateTime.Now.ToString() para el timestamp
        audioIndex++; // Incrementar el índice para el próximo archivo

        return Path.Combine(Application.persistentDataPath, fileName);
    }



 IEnumerator LoadAudio(string path)
    {
            string audioURL = "file://" + path;
            WWW www = new WWW(audioURL);

            while (!www.isDone)
            {
                yield return null;
            }

            if (!string.IsNullOrEmpty(www.error))
            {
                Debug.LogError("Error loading audio file: " + www.error);
                yield break;
            }

            if (www != null && www.GetAudioClip(false, false, AudioType.WAV) != null)
            {
                audioSource.clip = www.GetAudioClip(false, false, AudioType.WAV);
                audioSource.Play();
            }
            else
            {
                Debug.LogError("Failed to load audio clip from path: " + filePath);
            }
 }

public void PlayAudio()
    {
        if (!File.Exists(recordingPath))
        {
            Debug.LogError("Audio file not found at path: " + recordingPath);
            return;
        }

        StartCoroutine(LoadAudio(recordingPath));
    }
}

