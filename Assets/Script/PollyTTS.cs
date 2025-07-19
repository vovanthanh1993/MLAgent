using UnityEngine;
using Amazon.Polly;
using Amazon.Polly.Model;
using Amazon.Runtime;
using System.IO;

public class PollyTTS : MonoBehaviour
{
    public static PollyTTS Instance { get; private set; }

    [Header("AWS Credentials")]
    public string awsAccessKeyId = "YOUR_ACCESS_KEY";
    public string awsSecretAccessKey = "YOUR_SECRET_KEY";
    public string awsRegion = "ap-southeast-1"; // hoặc region bạn chọn

    private AmazonPollyClient pollyClient;

    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        var credentials = new BasicAWSCredentials(awsAccessKeyId, awsSecretAccessKey);
        var config = new AmazonPollyConfig { RegionEndpoint = Amazon.RegionEndpoint.APSoutheast1 }; // Chỉnh region nếu cần
        pollyClient = new AmazonPollyClient(credentials, config);
    }

    public async void Speak(string text)
    {
        var request = new SynthesizeSpeechRequest
        {
            Text = text,
            OutputFormat = OutputFormat.Mp3,
            VoiceId = "Joanna"
        };

        var response = await pollyClient.SynthesizeSpeechAsync(request);

        // Tạo file tạm duy nhất
        string filePath = Path.Combine(Application.persistentDataPath, "polly_tts_" + System.Guid.NewGuid() + ".mp3");
        using (var fileStream = File.Create(filePath))
        {
            response.AudioStream.CopyTo(fileStream);
        }

        // Sử dụng AudioManager thay vì AudioSource riêng
        if (AudioManager.Instance != null && AudioManager.Instance.IsVoicePlaying())
        {
            AudioManager.Instance.StopVoice();
        }

        StartCoroutine(PlayAudioAndDelete(filePath));
    }

    private System.Collections.IEnumerator PlayAudioAndDelete(string filePath)
    {
        using (var www = new WWW("file://" + filePath))
        {
            yield return www;
            
            if (AudioManager.Instance != null)
            {
                AudioClip clip = www.GetAudioClip(false, false, AudioType.MPEG);
                AudioManager.Instance.PlayVoice(clip);
                Debug.Log("Polly TTS: Phát âm thanh thành công qua AudioManager!");
            }
            else
            {
                Debug.LogError("AudioManager not found! Make sure AudioManager is in the scene.");
            }
        }
        // Xóa file tạm sau khi phát xong
        if (System.IO.File.Exists(filePath))
            System.IO.File.Delete(filePath);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
