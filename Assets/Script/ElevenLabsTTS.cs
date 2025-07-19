using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json; // Thêm ở đầu file

public class ElevenLabsTTS : MonoBehaviour
{
    [Header("ElevenLabs Settings")]
    public string apiKey = "YOUR_ELEVENLABS_API_KEY";
    public string voiceId = "BUPPIXeDaJWBz696iXRS";
    public string modelId = "eleven_flash_v2_5";
    
    private string apiUrl = "https://api.elevenlabs.io/v1/text-to-speech/";
    
    // Event để thông báo khi audio được tạo
    public System.Action<AudioClip> OnAudioGenerated;
    
    public void Speak(string text)
    {
        StartCoroutine(GenerateSpeech(text));
    }
    
    IEnumerator GenerateSpeech(string text)
    {
        // Tạo JSON request đúng format cho ElevenLabs
        var requestData = new {
            text = text,
            model_id = modelId
        };
        string jsonData = JsonConvert.SerializeObject(requestData);
        
        // Tạo request
        using (UnityWebRequest request = new UnityWebRequest(apiUrl + voiceId, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("xi-api-key", apiKey);
            
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                // Chuyển đổi audio data thành AudioClip
                AudioClip clip = CreateAudioClipFromBytes(request.downloadHandler.data);
                if (clip != null)
                {
                    // Trigger event trước khi phát audio
                    OnAudioGenerated?.Invoke(clip);
                    
                    // Sử dụng AudioManager thay vì AudioSource riêng
                    if (AudioManager.Instance != null)
                    {
                        AudioManager.Instance.PlayVoice(clip);
                        Debug.Log("ElevenLabs TTS: Phát âm thanh thành công qua AudioManager!");
                    }
                    else
                    {
                        Debug.LogError("AudioManager not found! Make sure AudioManager is in the scene.");
                    }
                }
            }
            else
            {
                Debug.LogError($"ElevenLabs TTS Error: {request.error}");
                Debug.LogError($"Response Code: {request.responseCode}");
                Debug.LogError($"Response: {request.downloadHandler.text}");
                Debug.LogError($"Request URL: {request.url}");
                Debug.LogError($"Request Data: {jsonData}");
            }
        }
    }
    
    AudioClip CreateAudioClipFromBytes(byte[] audioData)
    {
        // ElevenLabs trả về MP3, cần chuyển đổi
        // Đơn giản nhất là lưu file tạm và load
        string tempPath = Application.temporaryCachePath + "/tts_audio.mp3";
        System.IO.File.WriteAllBytes(tempPath, audioData);
        
        // Load audio file
        WWW www = new WWW("file://" + tempPath);
        while (!www.isDone) { }
        
        if (www.error == null)
        {
            AudioClip clip = www.GetAudioClip(false, false);
            return clip;
        }
        else
        {
            Debug.LogError($"Error loading audio: {www.error}");
            return null;
        }
    }
    
    // Singleton pattern
    public static ElevenLabsTTS Instance;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
} 