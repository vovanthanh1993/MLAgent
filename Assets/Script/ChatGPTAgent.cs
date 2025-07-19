using UnityEngine;
using TMPro;
using OpenAI;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Collections;

public class ChatGPTAgent : MonoBehaviour
{
    public string apiKey = "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";
    public TextMeshProUGUI commentText;

    private OpenAIApi openAI;
    private List<ChatMessage> messages = new List<ChatMessage>();
    
    // Singleton pattern
    public static ChatGPTAgent Instance { get; private set; }

    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            //DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        openAI = new OpenAIApi(apiKey);
        GameManager.Instance.OnScoreChanged += CommentScore;
        GenerateWelcomeMessage();
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnScoreChanged -= CommentScore;
    }

    public void AskChatGPT(string newText)
    {
        ChatMessage newMessage = new ChatMessage();
        newMessage.Content = newText;
        newMessage.Role = "user";
        messages.Add(newMessage);

        CreateChatCompletionRequest request = new CreateChatCompletionRequest();
        request.Messages = messages;
        request.Model = "gpt-3.5-turbo";
        _ = SendRequest(request);
    }

    async Task SendRequest(CreateChatCompletionRequest request)
    {
        try
        {
            var response = await openAI.CreateChatCompletion(request);
            if (response.Choices != null && response.Choices.Count > 0)
            {
                string reply = response.Choices[0].Message.Content.Trim();
                UIManager.Instance.SetComment(reply);
                Debug.Log("ChatGPT bình luận: " + reply);

                // Phát tiếng bằng PollyTTS
                //PollyTTS.Instance.Speak(reply);

                // Phát tiếng bằng ElevenLabs TTS
                ElevenLabsTTS.Instance.Speak(reply);
            }
            else
            {
                UIManager.Instance.SetComment("Không nhận được bình luận từ AI.");
            }
        }
        catch (System.Exception ex)
        {
            UIManager.Instance.SetComment("Lỗi: " + ex.Message);
        }
    }

    public void CommentScore(int playerScore, int opponentScore)
    {   string prompt;
        if(playerScore == GameManager.Instance.maxScore) 
        prompt = $"Hãy đóng vai một bình luận viên bóng đá chuyên nghiệp, đang tường thuật trực tiếp trận đấu giữa 1 AI và 1 Người chơi trong một game bóng đá tương tác. Hãy chúc mừng AI đã chiến thắng người chơi với tỉ số ({playerScore} - {opponentScore}). Bạn tên là Việt Dũng , phải nói Việt Dũng xin chào và hẹn gặp lại.";   
        else if(opponentScore == GameManager.Instance.maxScore) 
        prompt = $"Hãy đóng vai một bình luận viên bóng đá chuyên nghiệp, đang tường thuật trực tiếp trận đấu giữa 1 AI và 1 Người chơi trong một game bóng đá tương tác. Hãy chúc mừng người chơi đã chiến thắng AI với tỉ số ({opponentScore} - {playerScore}).Bạn tên là Việt Dũng , phải nói Việt Dũng xin chào và hẹn gặp lại.";   
        else prompt = $"Hãy đóng vai một bình luận viên bóng đá chuyên nghiệp, đang tường thuật trực tiếp trận đấu giữa 1 AI và 1 Người chơi trong một game bóng đá tương tác, bên nào đạt {GameManager.Instance.maxScore} sẽ chiến thắng, không có hiệp đấu nên đừng bình luận hiệp đấu.Viết bình luận bóng đá chuyên nghiệp, cảm xúc và sinh động về trận đấu giữa AI và người chơi dựa trên tỉ số hiện tại (AI {playerScore} - Người chơi {opponentScore}), không nhắc đến thời gian. Bình luận bằng tiếng Việt. Ngắn gọn trong 1 câu nói";
        if(!FootballAgent.Instance.isTraining) AskChatGPT(prompt);
    }

    public void GenerateWelcomeMessage()
    {
        string welcomePrompt = "Bắt buộc giới thiệu tên của bạn là Việt Dũng và đóng vai một bình luận viên bóng đá chuyên nghiệp, tạo ra một câu chào mừng ngắn gọn và thú vị cho người chơi khi bắt đầu trận đấu bóng đá AI. Câu chào mừng nên thể hiện sự hào hứng và tạo không khí vui vẻ. Viết bằng tiếng Việt, ngắn gọn trong 1 câu.";
        if(!FootballAgent.Instance.isTraining) AskChatGPT(welcomePrompt);
    }
} 