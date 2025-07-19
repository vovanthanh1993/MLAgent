using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    [Header("Score Settings")]
    public int playerScore = 0;
    public int opponentScore = 0;
    public int maxScore = 20; // Tỉ số giới hạn để dừng trận đấu
    
    [Header("3D UI References")]
    public TextMeshPro scoreText; // 3D TextMeshPro cho Scoreboard
    
    [Header("Match Settings")]
    public float matchTime = 0f;
    public bool isMatchEnded = false; // Trạng thái kết thúc trận đấu

    public static GameManager Instance;

    public delegate void ScoreChanged(int playerScore, int opponentScore);
    public event ScoreChanged OnScoreChanged;
    
    public delegate void MatchEnded(int playerScore, int opponentScore);
    public event MatchEnded OnMatchEnded;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    void Update()
    {
        // Chỉ cập nhật thời gian nếu trận đấu chưa kết thúc
        if (!isMatchEnded && matchTime < 300f)
        {
            matchTime += Time.deltaTime;
            UIManager.Instance.SetMatchTimer(matchTime);
        }
    }

    public void UpdateScore(bool isOpponentGoal)
    {
        // Kiểm tra nếu trận đấu đã kết thúc thì không cập nhật score
        if (isMatchEnded)
        {
            Debug.Log("Match already ended! Cannot update score.");
            return;
        }
        
        if (isOpponentGoal)
        {
            playerScore++;
        }
        else
        {
            opponentScore++;
        }

        // Cập nhật 3D Scoreboard
        UpdateScoreUI();

        // Gọi sự kiện score changed
        if (OnScoreChanged != null)
            OnScoreChanged(playerScore, opponentScore);
            
        // Kiểm tra xem có đạt tỉ số giới hạn không
        CheckScoreLimit();
    }
    
    public void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            // Cập nhật 3D TextMeshPro
            scoreText.text = $"{playerScore}:{opponentScore}";
            
            // Có thể thêm hiệu ứng cho 3D text
            StartCoroutine(ScoreUpdateEffect());
        }
        else
        {
            Debug.LogWarning("3D ScoreText not assigned in GameManager!");
        }
    }
    
    // Hiệu ứng khi update score cho 3D text
    private System.Collections.IEnumerator ScoreUpdateEffect()
    {
        if (scoreText != null)
        {
            // Lưu scale gốc
            Vector3 originalScale = scoreText.transform.localScale;
            
            // Scale up
            scoreText.transform.localScale = originalScale * 1.2f;
            
            // Đợi 0.1 giây
            yield return new WaitForSeconds(0.1f);
            
            // Scale về bình thường
            scoreText.transform.localScale = originalScale;
        }
    }
    
    private void CheckScoreLimit()
    {
        // Kiểm tra nếu tổng số bàn thắng đạt giới hạn
        if (playerScore >= maxScore || opponentScore >= maxScore)
        {
            EndMatch();
        }
    }
    
    public void EndMatch()
    {
        if (isMatchEnded)
        {
            Debug.Log("Match already ended!");
            return;
        }
        
        isMatchEnded = true;
        Debug.Log($"Match ended! Final score: Player {playerScore} - Opponent {opponentScore}");
        
        // Dừng mọi hoạt động của player và agent
        StopAllGameActivities();
        
        // Gọi sự kiện match ended
        if (OnMatchEnded != null)
            OnMatchEnded(playerScore, opponentScore);
            
        // Hiển thị kết quả cuối cùng
        ShowMatchResult();
    }
    
    private void StopAllGameActivities()
    {
        if(FootballAgent.Instance.isTraining) return;
        // Dung player
        LightningPoly.FootballEssentials3D.Player playerComponent = FootballAgent.Instance.player.GetComponent<LightningPoly.FootballEssentials3D.Player>();
        if (playerComponent != null)
        {
            playerComponent.enabled = false;
            Debug.Log("Player movement disabled.");
        }
        
        // Dừng AI agent
        if (FootballAgent.Instance != null)
        {
            FootballAgent.Instance.enabled = false;
            Debug.Log("AI agent disabled.");
        }
        
        // Dừng bóng
        if (FootballAgent.Instance != null && FootballAgent.Instance.ball != null)
        {
            Rigidbody ballRb = FootballAgent.Instance.ball.GetComponent<Rigidbody>();
            if (ballRb != null)
            {
                ballRb.linearVelocity = Vector3.zero;
                ballRb.angularVelocity = Vector3.zero;
                ballRb.isKinematic = true; // Dừng vật lý bóng
                Debug.Log("Ball physics disabled.");
            }
        }
        
        // Dừng tất cả coroutines
        if (FootballAgent.Instance != null)
        {
            FootballAgent.Instance.StopAllCoroutines();
        }
        
        Debug.Log("All game activities stopped.");
    }
    
    private void ShowMatchResult()
    {
        string resultMessage = "";
        
        if (playerScore > opponentScore)
        {
            resultMessage = $"AI chiến thắng {playerScore}-{opponentScore}";
        }
        else if (opponentScore > playerScore)
        {
            resultMessage = $"Người chơi chiến thắng {opponentScore}-{playerScore}";
        }
        
        // Hiển thị kết quả bằng SetCountdown
        if (UIManager.Instance != null)
        {
            UIManager.Instance.SetCountdown(resultMessage);
        }
        
        Debug.Log(resultMessage);
    }
    
    public void ResetMatch()
    {
        playerScore = 0;
        opponentScore = 0;
        matchTime = 0f;
        isMatchEnded = false;
        
        // Cập nhật 3D Scoreboard
        UpdateScoreUI();
        
        // Kích hoạt lại các hoạt động game
        ResumeGameActivities();
        
        Debug.Log("Match reset successfully!");
    }
    
    private void ResumeGameActivities()
    {
        // Kích hoạt lại player
        if (FootballAgent.Instance != null && FootballAgent.Instance.player != null)
        {
            LightningPoly.FootballEssentials3D.Player playerComponent = FootballAgent.Instance.player.GetComponent<LightningPoly.FootballEssentials3D.Player>();
            if (playerComponent != null)
            {
                playerComponent.enabled = true;
                Debug.Log("Player movement enabled.");
            }
        }
        
        // Kích hoạt lại AI agent
        if (FootballAgent.Instance != null)
        {
            FootballAgent.Instance.enabled = true;
            Debug.Log("AI agent enabled.");
        }
        
        // Kích hoạt lại bóng
        if (FootballAgent.Instance != null && FootballAgent.Instance.ball != null)
        {
            Rigidbody ballRb = FootballAgent.Instance.ball.GetComponent<Rigidbody>();
            if (ballRb != null)
            {
                ballRb.isKinematic = false; // Bật lại vật lý bóng
                Debug.Log("Ball physics enabled.");
            }
        }
        
        Debug.Log("All game activities resumed.");
    }

    private void Start() 
    {
        // Cập nhật 3D Scoreboard ban đầu
        UpdateScoreUI();
        
        if (FootballAgent.Instance != null)
        {
            FootballAgent.Instance.StartCoroutine(FootballAgent.Instance.WaitAndEndEpisode());
        }
    }
} 
