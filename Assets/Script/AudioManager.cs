using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    [Header("Audio Sources")]
    public AudioSource musicSource; // Âm nhạc nền
    public AudioSource sfxSource; // Hiệu ứng âm thanh
    public AudioSource voiceSource; // Giọng nói/TTS
    
    [Header("Audio Settings")]
    [Range(0f, 1f)]
    public float masterVolume = 1f;
    [Range(0f, 1f)]
    public float musicVolume = 0.2f;
    [Range(0f, 1f)]
    public float sfxVolume = 0.8f;
    [Range(0f, 1f)]
    public float voiceVolume = 1f;
    
    [Header("Audio Clips")]
    public AudioClip[] backgroundMusic;
    public AudioClip[] sfxClips;
    
    [Header("Voice Settings")]
    public bool enableVoiceAudio = true;
    public bool enableMusic = true;
    public bool enableSFX = true;
    
    // Singleton pattern
    public static AudioManager Instance { get; private set; }
    
    // Audio clip dictionary để truy cập nhanh
    private Dictionary<string, AudioClip> sfxDictionary = new Dictionary<string, AudioClip>();
    private Dictionary<string, AudioClip> musicDictionary = new Dictionary<string, AudioClip>();
    
    // Current playing info
    private string currentMusicName = "";
    private Coroutine musicFadeCoroutine;
    
    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            InitializeAudioSources();
            InitializeAudioDictionaries();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        // Load settings từ PlayerPrefs
        //LoadAudioSettings();
        
        // Bắt đầu phát nhạc nền nếu có
        if (enableMusic && backgroundMusic.Length > 0)
        {
            PlayBackgroundMusic(0);
        }
    }
    
    private void InitializeAudioSources()
    {
        // Tạo AudioSource cho music nếu chưa có
        if (musicSource == null)
        {
            GameObject musicObj = new GameObject("MusicSource");
            musicObj.transform.SetParent(transform);
            musicSource = musicObj.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.playOnAwake = false;
        }
        
        // Tạo AudioSource cho SFX nếu chưa có
        if (sfxSource == null)
        {
            GameObject sfxObj = new GameObject("SFXSource");
            sfxObj.transform.SetParent(transform);
            sfxSource = sfxObj.AddComponent<AudioSource>();
            sfxSource.loop = false;
            sfxSource.playOnAwake = false;
        }
        
        // Tạo AudioSource cho voice nếu chưa có
        if (voiceSource == null)
        {
            GameObject voiceObj = new GameObject("VoiceSource");
            voiceObj.transform.SetParent(transform);
            voiceSource = voiceObj.AddComponent<AudioSource>();
            voiceSource.loop = false;
            voiceSource.playOnAwake = false;
        }
        
        // Cập nhật volume
        UpdateAllVolumes();
    }
    
    private void InitializeAudioDictionaries()
    {
        // Thêm SFX clips vào dictionary
        for (int i = 0; i < sfxClips.Length; i++)
        {
            if (sfxClips[i] != null)
            {
                sfxDictionary[sfxClips[i].name] = sfxClips[i];
            }
        }
        
        // Thêm music clips vào dictionary
        for (int i = 0; i < backgroundMusic.Length; i++)
        {
            if (backgroundMusic[i] != null)
            {
                musicDictionary[backgroundMusic[i].name] = backgroundMusic[i];
            }
        }
    }
    
    // === MUSIC METHODS ===
    public void PlayBackgroundMusic(int index)
    {
        if (!enableMusic || index < 0 || index >= backgroundMusic.Length)
            return;
            
        AudioClip clip = backgroundMusic[index];
        if (clip != null)
        {
            PlayBackgroundMusic(clip.name);
        }
    }
    
    public void PlayBackgroundMusic(string musicName)
    {
        if (!enableMusic || !musicDictionary.ContainsKey(musicName))
            return;
            
        if (currentMusicName != musicName)
        {
            currentMusicName = musicName;
            musicSource.clip = musicDictionary[musicName];
            musicSource.Play();
            Debug.Log($"Playing background music: {musicName}");
        }
    }
    
    public void StopBackgroundMusic()
    {
        if (musicSource != null && musicSource.isPlaying)
        {
            musicSource.Stop();
            currentMusicName = "";
            Debug.Log("Background music stopped");
        }
    }
    
    public void PauseBackgroundMusic()
    {
        if (musicSource != null && musicSource.isPlaying)
        {
            musicSource.Pause();
        }
    }
    
    public void ResumeBackgroundMusic()
    {
        if (musicSource != null && !musicSource.isPlaying && musicSource.clip != null)
        {
            musicSource.UnPause();
        }
    }
    
    public void FadeInMusic(float duration = 2f)
    {
        if (musicFadeCoroutine != null)
            StopCoroutine(musicFadeCoroutine);
        musicFadeCoroutine = StartCoroutine(FadeMusic(0f, musicVolume, duration));
    }
    
    public void FadeOutMusic(float duration = 2f)
    {
        if (musicFadeCoroutine != null)
            StopCoroutine(musicFadeCoroutine);
        musicFadeCoroutine = StartCoroutine(FadeMusic(musicVolume, 0f, duration));
    }
    
    private IEnumerator FadeMusic(float startVolume, float endVolume, float duration)
    {
        float currentTime = 0f;
        float startVol = startVolume;
        
        while (currentTime < duration)
        {
            currentTime += Time.deltaTime;
            float newVolume = Mathf.Lerp(startVol, endVolume, currentTime / duration);
            musicSource.volume = newVolume * masterVolume;
            yield return null;
        }
        
        musicSource.volume = endVolume * masterVolume;
    }
    
    // === SFX METHODS ===
    public void PlaySFX(int index)
    {
        if (!enableSFX || index < 0 || index >= sfxClips.Length)
            return;
            
        AudioClip clip = sfxClips[index];
        if (clip != null)
        {
            PlaySFX(clip.name);
        }
    }
    
    public void PlaySFX(string sfxName)
    {
        if (!enableSFX || !sfxDictionary.ContainsKey(sfxName))
            return;
            
        sfxSource.PlayOneShot(sfxDictionary[sfxName], sfxVolume * masterVolume);
        Debug.Log($"Playing SFX: {sfxName}");
    }
    
    public void PlaySFX(AudioClip clip)
    {
        if (!enableSFX || clip == null)
            return;
            
        sfxSource.PlayOneShot(clip, sfxVolume * masterVolume);
    }
    
    // === VOICE METHODS ===
    public void PlayVoice(AudioClip clip)
    {
        if (!enableVoiceAudio || clip == null)
            return;
            
        // Dừng voice hiện tại nếu đang phát
        if (voiceSource.isPlaying)
        {
            voiceSource.Stop();
        }
        
        voiceSource.clip = clip;
        voiceSource.volume = voiceVolume * masterVolume;
        voiceSource.Play();
        Debug.Log($"Playing voice: {clip.name}");
    }
    
    public void StopVoice()
    {
        if (voiceSource != null && voiceSource.isPlaying)
        {
            voiceSource.Stop();
        }
    }
    
    public bool IsVoicePlaying()
    {
        return voiceSource != null && voiceSource.isPlaying;
    }
    
    // === VOLUME CONTROL ===
    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        UpdateAllVolumes();
        SaveAudioSettings();
    }
    
    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        if (musicSource != null)
        {
            musicSource.volume = musicVolume * masterVolume;
        }
        SaveAudioSettings();
    }
    
    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        SaveAudioSettings();
    }
    
    public void SetVoiceVolume(float volume)
    {
        voiceVolume = Mathf.Clamp01(volume);
        if (voiceSource != null)
        {
            voiceSource.volume = voiceVolume * masterVolume;
        }
        SaveAudioSettings();
    }
    
    private void UpdateAllVolumes()
    {
        if (musicSource != null)
            musicSource.volume = musicVolume * masterVolume;
        if (voiceSource != null)
            voiceSource.volume = voiceVolume * masterVolume;
    }
    
    // === SETTINGS PERSISTENCE ===
    private void SaveAudioSettings()
    {
        PlayerPrefs.SetFloat("MasterVolume", masterVolume);
        PlayerPrefs.SetFloat("MusicVolume", musicVolume);
        PlayerPrefs.SetFloat("SFXVolume", sfxVolume);
        PlayerPrefs.SetFloat("VoiceVolume", voiceVolume);
        PlayerPrefs.SetInt("EnableMusic", enableMusic ? 1 : 0);
        PlayerPrefs.SetInt("EnableSFX", enableSFX ? 1 : 0);
        PlayerPrefs.SetInt("EnableVoice", enableVoiceAudio ? 1 : 0);
        PlayerPrefs.Save();
    }
    
    private void LoadAudioSettings()
    {
        masterVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
        musicVolume = PlayerPrefs.GetFloat("MusicVolume", 0.7f);
        sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 0.8f);
        voiceVolume = PlayerPrefs.GetFloat("VoiceVolume", 0.9f);
        enableMusic = PlayerPrefs.GetInt("EnableMusic", 1) == 1;
        enableSFX = PlayerPrefs.GetInt("EnableSFX", 1) == 1;
        enableVoiceAudio = PlayerPrefs.GetInt("EnableVoice", 1) == 1;
        
        UpdateAllVolumes();
    }
    
    // === UTILITY METHODS ===
    public void MuteAll()
    {
        float tempMaster = masterVolume;
        SetMasterVolume(0f);
        StartCoroutine(UnmuteAfterDelay(tempMaster, 2f));
    }
    
    private IEnumerator UnmuteAfterDelay(float originalVolume, float delay)
    {
        yield return new WaitForSeconds(delay);
        SetMasterVolume(originalVolume);
    }
    
    public void ToggleMusic()
    {
        enableMusic = !enableMusic;
        if (!enableMusic)
        {
            StopBackgroundMusic();
        }
        else if (backgroundMusic.Length > 0)
        {
            PlayBackgroundMusic(0);
        }
        SaveAudioSettings();
    }
    
    public void ToggleSFX()
    {
        enableSFX = !enableSFX;
        SaveAudioSettings();
    }
    
    public void ToggleVoice()
    {
        enableVoiceAudio = !enableVoiceAudio;
        if (!enableVoiceAudio)
        {
            StopVoice();
        }
        SaveAudioSettings();
    }
    
    // === DEBUG METHODS ===
    public void PrintAudioStatus()
    {
        Debug.Log($"AudioManager Status:");
        Debug.Log($"- Master Volume: {masterVolume}");
        Debug.Log($"- Music Volume: {musicVolume} (Enabled: {enableMusic})");
        Debug.Log($"- SFX Volume: {sfxVolume} (Enabled: {enableSFX})");
        Debug.Log($"- Voice Volume: {voiceVolume} (Enabled: {enableVoiceAudio})");
        Debug.Log($"- Current Music: {currentMusicName}");
        Debug.Log($"- Voice Playing: {IsVoicePlaying()}");
    }
    
    void OnDestroy()
    {
        SaveAudioSettings();
    }
} 