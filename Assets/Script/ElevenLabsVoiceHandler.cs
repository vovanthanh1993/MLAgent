using UnityEngine;
using ReadyPlayerMe.Core;

public class ElevenLabsVoiceHandler : MonoBehaviour
{
    [Header("Components")]
    public ElevenLabsTTS elevenLabsTTS;
    public VoiceHandler voiceHandler;
    
    [Header("Settings")]
    public bool autoConnect = true;
    public bool enableLipSync = true;
    
    [Header("Audio Settings")]
    public AudioSource voiceSource;
    public float volumeThreshold = 0.01f;
    
    private AudioClip currentAudioClip;
    private bool isPlaying = false;
    
    void Start()
    {
        // Tìm components nếu chưa gán
        if (elevenLabsTTS == null)
            elevenLabsTTS = FindObjectOfType<ElevenLabsTTS>();
            
        if (voiceHandler == null)
            voiceHandler = GetComponent<VoiceHandler>();
            
        if (voiceSource == null)
            voiceSource = AudioManager.Instance?.voiceSource;
            
        // Kết nối tự động
        if (autoConnect)
        {
            ConnectElevenLabsToVoiceHandler();
        }
    }
    
    void ConnectElevenLabsToVoiceHandler()
    {
        if (elevenLabsTTS != null && voiceHandler != null)
        {
            // Override ElevenLabsTTS Speak method để kết nối với VoiceHandler
            elevenLabsTTS.OnAudioGenerated += OnElevenLabsAudioGenerated;
            
            Debug.Log("ElevenLabsTTS connected to VoiceHandler successfully!");
        }
        else
        {
            Debug.LogError("ElevenLabsTTS or VoiceHandler not found!");
        }
    }
    
    // Callback khi ElevenLabsTTS tạo audio
    public void OnElevenLabsAudioGenerated(AudioClip audioClip)
    {
        if (audioClip != null && voiceHandler != null)
        {
            currentAudioClip = audioClip;
            
            // Cấu hình VoiceHandler để sử dụng AudioClip
            voiceHandler.AudioProvider = AudioProviderType.AudioClip;
            voiceHandler.AudioClip = audioClip;
            voiceHandler.AudioSource = voiceSource;
            
            // Bắt đầu phát và lip sync
            StartLipSync();
            
            Debug.Log($"ElevenLabs audio connected to VoiceHandler: {audioClip.name}");
        }
    }
    
    void StartLipSync()
    {
        if (voiceHandler != null && currentAudioClip != null)
        {
            // Cấu hình VoiceHandler
            voiceHandler.InitializeAudio();
            
            // Bắt đầu phát audio
            voiceHandler.PlayAudioClip(currentAudioClip);
            
            isPlaying = true;
            
            // Theo dõi khi audio kết thúc
            StartCoroutine(MonitorAudioPlayback());
            
            Debug.Log("Lip sync started with ElevenLabs audio");
        }
    }
    
    System.Collections.IEnumerator MonitorAudioPlayback()
    {
        while (isPlaying && voiceSource != null && voiceSource.isPlaying)
        {
            yield return new WaitForSeconds(0.1f);
        }
        
        // Audio đã kết thúc
        isPlaying = false;
        Debug.Log("ElevenLabs audio playback finished");
    }
    
    // Public methods để điều khiển từ bên ngoài
    public void SpeakWithLipSync(string text)
    {
        if (elevenLabsTTS != null)
        {
            // Gọi ElevenLabsTTS để tạo audio
            elevenLabsTTS.Speak(text);
        }
    }
    
    public void StopLipSync()
    {
        if (voiceHandler != null && voiceSource != null)
        {
            voiceSource.Stop();
            isPlaying = false;
        }
    }
    
    public void SetVoiceHandler(VoiceHandler handler)
    {
        voiceHandler = handler;
        if (autoConnect)
        {
            ConnectElevenLabsToVoiceHandler();
        }
    }
    
    public void SetElevenLabsTTS(ElevenLabsTTS tts)
    {
        elevenLabsTTS = tts;
        if (autoConnect)
        {
            ConnectElevenLabsToVoiceHandler();
        }
    }
    
    // Debug methods
    public bool IsLipSyncPlaying()
    {
        return isPlaying;
    }
    
    public AudioClip GetCurrentAudioClip()
    {
        return currentAudioClip;
    }
    
    void OnDestroy()
    {
        if (elevenLabsTTS != null)
        {
            elevenLabsTTS.OnAudioGenerated -= OnElevenLabsAudioGenerated;
        }
    }
} 