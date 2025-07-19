using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
public class UIManager : MonoBehaviour
{
    public static UIManager Instance;
    public TextMeshProUGUI countdownText;

    public TextMeshProUGUI titleText;
    public TextMeshProUGUI commentText;
    public TextMeshProUGUI matchTimerText;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void ReturnHome()
    {
       SceneManager.LoadScene("Menu");
    }

    public void ResetGame()
    {
       SceneManager.LoadScene("GamePlay");
    }
    
    public void SetCountdown(int seconds)
    {
        if (countdownText != null)
            countdownText.text = seconds > 0 ? "Start in " + seconds.ToString() + "..." : "";
    }

    public void SetCountdown(string text)
    {
        if (countdownText != null)
            countdownText.text = text;
    }

    public void SetComment(string comment)
    {
        if (commentText != null)
            commentText.text = comment;
    }

    public void SetTitle(string comment)
    {
        if (titleText != null)
            titleText.text = comment;
    }

    public void SetMatchTimer(float seconds)
    {
        if (matchTimerText != null)
        {
            int min = Mathf.FloorToInt(seconds / 60f);
            int sec = Mathf.FloorToInt(seconds % 60f);
            matchTimerText.text = $"{min:00}:{sec:00}";
        }
    }
} 