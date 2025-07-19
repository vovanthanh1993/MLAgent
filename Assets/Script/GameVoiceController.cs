using UnityEngine;
using UnityEngine.SceneManagement;

public class GameVoiceController : MonoBehaviour
{

    public void SetVoiceCommand(string command)
    {
        command = command.ToLower().Trim();
    
        if (command.Contains("start") || command.Contains("bắt đầu")) 
        {
            LoadGameplayScene();
        }
        else if (command.Contains("quit") || command.Contains("thoát") || command.Contains("exit") || 
                 command.Contains("dừng") || command.Contains("stop") || command.Contains("kết thúc") ||
                 command.Contains("tắt") || command.Contains("turn off") || command.Contains("đóng"))
        {
            QuitGame();
        } 
    }
    
    private void LoadGameplayScene()
    {
        UIManager.Instance.SetTitle("Game is starting...");
        SceneManager.LoadScene("GamePlay");
    }
    
    private void QuitGame()
    {
        
        // Thoát game
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
} 