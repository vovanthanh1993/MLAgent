using OpenAI;
using UnityEngine;
using System.Collections.Generic;

public class WhisperVoiceController : MonoBehaviour
{
    [Header("Voice Controller")]
    public GameVoiceController gameVoiceController;
    
    [Header("UI Display")]
    public TMPro.TextMeshProUGUI voiceTextDisplay; // Hiển thị text thu được

    [Header("Cài đặt ghi âm liên tục - Voice Chat Mode")]
    public bool continuousRecording = true; // Bật/tắt ghi âm liên tục
    public float recordingInterval = 0.5f; // Thời gian ghi âm mỗi lần (giây) - giảm xuống để phản hồi nhanh hơn
    public float silenceThreshold = 0.02f; // Ngưỡng âm thanh để phát hiện nói - giảm xuống để nhạy hơn
    public float minRecordingLength = 0.2f; // Thời gian tối thiểu để xử lý (giây) - giảm xuống để nhanh hơn
    public float maxRecordingLength = 2f; // Thời gian tối đa ghi âm (giây) - giảm xuống để nhanh hơn
    public float silenceTimeout = 0.2f; // Thời gian im lặng để kết thúc ghi âm (giây)
    public bool enableRealTimeProcessing = true; // Xử lý real-time
    public float voiceDetectionSensitivity = 1.5f; // Độ nhạy phát hiện giọng nói

    [Header("Debug & Recovery")]
    public bool enableDebugLogs = true; // Bật log debug
    public float healthCheckInterval = 5f; // Kiểm tra sức khỏe hệ thống mỗi 5 giây
    public int maxQueueSize = 10; // Giới hạn kích thước queue

    private readonly string fileName = "output.wav";
    private AudioClip clip;
    private bool isRecording = false;
    private float time = 0f;
    private OpenAIApi openai = new OpenAIApi();
    private float lastRecordingTime = 0f;
    private float silenceTimer = 0f; // Thời gian im lặng
    private bool hasDetectedSpeech = false; // Đã phát hiện lời nói
    private float lastSpeechTime = 0f; // Thời gian lời nói cuối cùng
    private string lastCommand = ""; // Lệnh cuối cùng để tránh lặp lại
    private float lastCommandTime = 0f; // Thời gian lệnh cuối cùng
    private float commandCooldown = 0.3f; // Thời gian chờ giữa các lệnh - giảm xuống để nhanh hơn
    private bool isProcessingCommand = false; // Đang xử lý lệnh
    private Queue<string> commandQueue = new Queue<string>(); // Queue để xử lý lệnh
    private float lastVolumeCheck = 0f; // Thời gian kiểm tra âm lượng cuối
    
    // Thêm biến để theo dõi sức khỏe hệ thống
    private float lastHealthCheck = 0f;
    private int consecutiveFailures = 0;
    private float lastSuccessfulRecording = 0f;
    private bool isSystemHealthy = true;

    void Start()
    {
        Debug.Log("Voice recognition system initialized.");
        lastHealthCheck = Time.time;
        lastSuccessfulRecording = Time.time;
        
        // Kiểm tra microphone có sẵn
        if (Microphone.devices.Length == 0)
        {
            Debug.LogError("No microphone devices found!");
            isSystemHealthy = false;
        }
        else
        {
            Debug.Log($"Found {Microphone.devices.Length} microphone devices");
        }
    }

    void Update()
    {
        // Kiểm tra sức khỏe hệ thống định kỳ
        if (Time.time - lastHealthCheck > healthCheckInterval)
        {
            PerformHealthCheck();
            lastHealthCheck = Time.time;
        }
        
        // Xử lý queue lệnh
        ProcessCommandQueue();
        
        if (continuousRecording && !isRecording && !isProcessingCommand && isSystemHealthy)
        {
            // Tự động bắt đầu ghi âm mới sau mỗi khoảng thời gian
            if (Time.time - lastRecordingTime >= recordingInterval)
            {
                StartRecording();
            }
        }

        if (isRecording)
        {
            time += Time.deltaTime;
            
            // Kiểm tra âm thanh để phát hiện nói - tối ưu cho voice chat
            if (Microphone.IsRecording(null) && clip != null && Time.time - lastVolumeCheck > 0.05f)
            {
                lastVolumeCheck = Time.time;
                float[] samples = new float[clip.samples];
                clip.GetData(samples, 0);
                
                // Tính mức âm thanh trung bình - tối ưu hóa
                float averageVolume = 0f;
                int sampleStep = Mathf.Max(1, samples.Length / 100); // Chỉ lấy 100 mẫu để tăng tốc
                for (int i = 0; i < samples.Length; i += sampleStep)
                {
                    averageVolume += Mathf.Abs(samples[i]);
                }
                averageVolume /= (samples.Length / sampleStep);

                // Phát hiện lời nói với độ nhạy cao hơn
                if (averageVolume > silenceThreshold * voiceDetectionSensitivity)
                {
                    hasDetectedSpeech = true;
                    lastSpeechTime = Time.time;
                    silenceTimer = 0f;
                    time = 0f; // Reset thời gian nếu có người nói
                }
                else
                {
                    silenceTimer += Time.deltaTime;
                }

                // Kết thúc ghi âm nhanh hơn cho voice chat
                bool shouldEndRecording = false;
                
                if (hasDetectedSpeech)
                {
                    if (silenceTimer > silenceTimeout) // Im lặng sau khi có lời nói
                    {
                        shouldEndRecording = true;
                    }
                    else if (time >= minRecordingLength && silenceTimer > 0.1f) // Giảm thời gian im lặng
                    {
                        shouldEndRecording = true;
                    }
                }
                
                if (time >= maxRecordingLength) // Ghi âm quá lâu
                {
                    shouldEndRecording = true;
                }

                if (shouldEndRecording)
                {
                    isRecording = false;
                    time = 0f;
                    silenceTimer = 0f;
                    hasDetectedSpeech = false;
                    EndRecording();
                }
            }
        }
    }

    private void PerformHealthCheck()
    {
        if (enableDebugLogs)
        {
            Debug.Log($"Health Check - Queue Size: {commandQueue.Count}, IsRecording: {isRecording}, IsProcessing: {isProcessingCommand}");
        }
        
        // Kiểm tra nếu hệ thống bị stuck
        if (isProcessingCommand && Time.time - lastCommandTime > 10f)
        {
            Debug.LogWarning("System stuck in processing state, resetting...");
            isProcessingCommand = false;
            consecutiveFailures++;
        }
        
        // Kiểm tra nếu không có recording thành công trong thời gian dài
        if (Time.time - lastSuccessfulRecording > 30f && continuousRecording)
        {
            Debug.LogWarning("No successful recordings for 30 seconds, restarting system...");
            RestartRecordingSystem();
        }
        
        // Giới hạn queue size
        while (commandQueue.Count > maxQueueSize)
        {
            commandQueue.Dequeue();
            Debug.LogWarning("Queue overflow, clearing old commands");
        }
        
        // Reset failure counter nếu hệ thống hoạt động tốt
        if (Time.time - lastSuccessfulRecording < 10f)
        {
            consecutiveFailures = 0;
            isSystemHealthy = true;
        }
    }
    
    private void RestartRecordingSystem()
    {
        Debug.Log("Restarting recording system...");
        
        // Dừng recording hiện tại nếu có
        if (isRecording)
        {
            #if !UNITY_WEBGL
            Microphone.End(null);
            #endif
            isRecording = false;
        }
        
        // Reset các biến trạng thái
        isProcessingCommand = false;
        time = 0f;
        silenceTimer = 0f;
        hasDetectedSpeech = false;
        lastRecordingTime = Time.time;
        
        // Xóa queue cũ
        commandQueue.Clear();
        
        // Kiểm tra microphone
        if (Microphone.devices.Length > 0)
        {
            isSystemHealthy = true;
            Debug.Log("Recording system restarted successfully");
        }
        else
        {
            isSystemHealthy = false;
            Debug.LogError("Cannot restart - no microphone available");
        }
    }

    private void StartRecording()
    {
        // Kiểm tra microphone trước khi bắt đầu
        if (Microphone.devices.Length == 0)
        {
            Debug.LogError("No microphone available for recording");
            isSystemHealthy = false;
            return;
        }
        
        isRecording = true;
        time = 0f;
        silenceTimer = 0f;
        hasDetectedSpeech = false;
        lastRecordingTime = Time.time;
        
        if (enableDebugLogs)
        {
            Debug.Log("Recording started...");
        }
        
        #if !UNITY_WEBGL
        clip = Microphone.Start(null, false, (int)maxRecordingLength, 44100);
        #endif
    }

    private async void EndRecording()
    {
        if (enableDebugLogs)
        {
            Debug.Log("Processing voice recognition...");
        }
        
        #if !UNITY_WEBGL
        Microphone.End(null);
        #endif
        
        // Chỉ xử lý nếu có âm thanh
        if (clip != null && clip.length > minRecordingLength) // Ít nhất minRecordingLength giây
        {
            try
            {
                byte[] data = Samples.Whisper.SaveWav.Save(fileName, clip);
                var req = new CreateAudioTranscriptionsRequest
                {
                    FileData = new FileData() { Data = data, Name = "audio.wav" },
                    Model = "whisper-1",
                    Language = "en"
                };
                var res = await openai.CreateAudioTranscription(req);
                
                if (enableDebugLogs)
                {
                    Debug.Log($"Transcribed: {res.Text}");
                }
                
                // Xử lý lệnh ngay lập tức
                if (!string.IsNullOrEmpty(res.Text))
                {
                    string currentCommand = res.Text.Trim().ToLower();
                    
                    // Thêm vào queue để xử lý không đồng bộ
                    if (currentCommand != lastCommand || Time.time - lastCommandTime > commandCooldown)
                    {
                        if (commandQueue.Count < maxQueueSize)
                        {
                            commandQueue.Enqueue(currentCommand);
                            lastCommand = currentCommand;
                            lastCommandTime = Time.time;
                            
                            if (enableDebugLogs)
                            {
                                Debug.Log($"⚡ Voice command: {currentCommand}");
                            }
                        }
                        else
                        {
                            Debug.LogWarning("Command queue full, skipping command");
                        }
                    }
                }
                
                // Đánh dấu recording thành công
                lastSuccessfulRecording = Time.time;
                consecutiveFailures = 0;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error processing audio: {e.Message}");
                consecutiveFailures++;
                
                if (consecutiveFailures > 5)
                {
                    Debug.LogError("Too many consecutive failures, restarting system...");
                    RestartRecordingSystem();
                }
            }
        }
        else
        {
            if (enableDebugLogs)
            {
                Debug.Log("No audio detected, continuing recording...");
            }
        }
    }
    
    // Xử lý queue lệnh để tránh blocking
    private void ProcessCommandQueue()
    {
        if (commandQueue.Count > 0 && !isProcessingCommand)
        {
            isProcessingCommand = true;
            string command = commandQueue.Dequeue();
            
            // Truyền lệnh qua GameVoiceController
            if (gameVoiceController != null)
            {
                gameVoiceController.SetVoiceCommand(command);
                
                if (enableDebugLogs)
                {
                    Debug.Log($"⚡ Command sent to GameVoiceController: {command}");
                }
                
                // Hiển thị lệnh đã xử lý
                if (voiceTextDisplay != null)
                {
                    voiceTextDisplay.text = $"Voice command: {command}";
                }
            }
            else
            {
                Debug.LogWarning("GameVoiceController is not assigned!");
            }
            
            // Reset sau một frame
            StartCoroutine(ResetProcessingFlag());
        }
    }
    
    private System.Collections.IEnumerator ResetProcessingFlag()
    {
        yield return new WaitForEndOfFrame();
        isProcessingCommand = false;
    }
    
    // Thêm method để debug
    public void ForceRestart()
    {
        Debug.Log("Force restarting voice system...");
        RestartRecordingSystem();
    }
    
    void OnDestroy()
    {
        // Cleanup khi destroy
        if (isRecording)
        {
            #if !UNITY_WEBGL
            Microphone.End(null);
            #endif
        }
    }
} 