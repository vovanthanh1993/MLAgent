using UnityEngine;

public class GoalDetector : MonoBehaviour
{
    public FootballAgent agent; // Kéo thả reference agent vào đây trong Inspector

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Ball"))
        {
            if (agent != null)
            {
                Debug.Log("Tag của goal này là: " + gameObject.tag);
                bool isOpponentGoal = gameObject.CompareTag("GoalOpponent");
                agent.OnBallScored(isOpponentGoal);
                GameManager.Instance.UpdateScore(isOpponentGoal);
            }
        }
    }
} 