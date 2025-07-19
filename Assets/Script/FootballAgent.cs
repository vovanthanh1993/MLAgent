using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using System.Collections;
using LightningPoly.FootballEssentials3D;
public class FootballAgent : Agent
{
    public static FootballAgent Instance;
    
    [Header("Football Settings")]
    public Transform ball;
    public Transform goalOpponent; // Khung thành đối thủ
    public Transform goalOwn;      // Khung thành của mình
    public float moveSpeed = 5f;
    public float kickForce = 10f;
    public float kickRange = 2f;
    public LayerMask ballLayer = 1;
    public Transform player; // Kéo thả player vào Inspector
    
    [Header("Rewards")]
    public float goalReward = 10f;
    public float kickReward = 1f;
    public float distancePenalty = 0.01f;
    public float timePenalty = 0.001f;
    
    private Rigidbody agentRb;
    private Rigidbody ballRb;
    private Vector3 startPosition;
    private Vector3 ballStartPosition;
    private Vector3 goalOpponentStartPos;
    private Vector3 goalOwnStartPos;
    private Vector3 playerStartPosition;
    
    // Tracking
    private bool hasKicked = false;
    private float episodeStartTime;
    private bool isTouchingBall = false;
    private bool isWaiting = false;

    public bool isTraining = false;
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject == ball.gameObject)
        {
            isTouchingBall = true;
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject == ball.gameObject)
        {
            isTouchingBall = false;
            hasKicked = false;
        }
    }
    
    public override void Initialize()
    {
        agentRb = GetComponent<Rigidbody>();
        ballRb = ball.GetComponent<Rigidbody>();
        startPosition = transform.localPosition;
        ballStartPosition = ball.localPosition;
        goalOpponentStartPos = goalOpponent.localPosition;
        goalOwnStartPos = goalOwn.localPosition;
        playerStartPosition = player.localPosition;
    }
    
    public override void OnEpisodeBegin()
    {
        // Reset tracking
        hasKicked = false;
        episodeStartTime = Time.time;
        if (isTraining) ManualResetPositions();
    }
    
    public override void CollectObservations(VectorSensor sensor)
    {
        // Agent position (3)
        sensor.AddObservation(transform.localPosition);
        // Ball position & velocity (6)
        sensor.AddObservation(ball.localPosition);
        sensor.AddObservation(ballRb.linearVelocity);
        // Opponent goal position (3)
        sensor.AddObservation(goalOpponent.localPosition);
        // Own goal position (3)
        sensor.AddObservation(goalOwn.localPosition);
        // Distance to ball (1)
        sensor.AddObservation(Vector3.Distance(transform.localPosition, ball.localPosition));
        // Distance from ball to opponent goal (1)
        sensor.AddObservation(Vector3.Distance(ball.localPosition, goalOpponent.localPosition));
        // Distance from ball to own goal (1)
        sensor.AddObservation(Vector3.Distance(ball.localPosition, goalOwn.localPosition));
        // Direction to ball (3)
        Vector3 directionToBall = (ball.localPosition - transform.localPosition).normalized;
        sensor.AddObservation(directionToBall);
        // Direction from ball to opponent goal (3)
        Vector3 directionToGoalOpponent = (goalOpponent.localPosition - ball.localPosition).normalized;
        sensor.AddObservation(directionToGoalOpponent);
        // Direction from ball to own goal (3)
        Vector3 directionToGoalOwn = (goalOwn.localPosition - ball.localPosition).normalized;
        sensor.AddObservation(directionToGoalOwn);
        // Tổng số quan sát: 3 + 6 + 3 + 3 + 1 + 1 + 1 + 3 + 3 + 3 = 27
    }
    
    public override void OnActionReceived(ActionBuffers actions)
    {
        if (isWaiting) return;
        // Continuous actions: [moveX, moveZ, kick]
        float moveX = Mathf.Clamp(actions.ContinuousActions[0], -1f, 1f);
        float moveZ = Mathf.Clamp(actions.ContinuousActions[1], -1f, 1f);
        float kick = Mathf.Clamp(actions.ContinuousActions[2], 0f, 1f);
        // Di chuyển agent
        Vector3 movement = new Vector3(moveX, 0, moveZ) * moveSpeed * Time.deltaTime;
        transform.localPosition += movement;

        // Đá bóng nếu nhấn kick, chưa đá, và đang va chạm với bóng
        if (kick > 0.5f && !hasKicked && isTouchingBall)
        {
            KickBall();
        }
        // Penalties
        float distanceToBall = Vector3.Distance(transform.localPosition, ball.localPosition);
        AddReward(-distancePenalty * distanceToBall);
        AddReward(-timePenalty);
    }
    
    private void KickBall()
    {
        // Đá về phía goal đối thủ
        Vector3 kickDirection = (goalOpponent.localPosition - ball.localPosition).normalized;
        ballRb.AddForce(kickDirection * kickForce, ForceMode.Impulse);
        hasKicked = true;
        AddReward(kickReward);
    }
    
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        ActionSegment<float> continuousActions = actionsOut.ContinuousActions;
        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");
        continuousActions[0] = moveX;
        continuousActions[1] = moveZ;
        float kick = Input.GetKey(KeyCode.Space) ? 1f : 0f;
        continuousActions[2] = kick;
    }
    
    public void OnBallScored(bool isOpponentGoal)
    {
        if (isOpponentGoal)
        {
            AddReward(goalReward);
            Debug.Log("GOAL! Bóng đã vào lưới đối thủ!");
        }
        else
        {
            AddReward(-goalReward);
            Debug.Log("OWN GOAL! Bóng đã vào lưới nhà!");
        }

        ballRb.linearVelocity = Vector3.zero;
        ballRb.angularVelocity = Vector3.zero;
        AudioManager.Instance.PlaySFX(0);
        StartCoroutine(WaitAndEndEpisode());
    }

    void Awake()
    {
        Application.runInBackground = true;
        
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public IEnumerator WaitAndEndEpisode()
    {

        if(!isTraining) {
            player.GetComponent<Player>().enabled = false;
            isWaiting = true;
            float countdown = 10f;
            while (countdown > 0)
            {
                UIManager.Instance.SetCountdown(Mathf.CeilToInt(countdown));
                yield return new WaitForSeconds(1f);
                countdown -= 1f;
                if(countdown == 9) {
                    ManualResetPositions();
                }
            }

            UIManager.Instance.SetCountdown(0);
            isWaiting = false;
            player.GetComponent<Player>().enabled = true;
        }
        
        EndEpisode();
    }

    private void ManualResetPositions()
    {
        // Reset agent
        transform.localPosition = startPosition;

        // Reset player (nếu có)
        if (player != null) {
            player.localPosition = playerStartPosition;
        }

        // Reset vật lý bóng
        if (isTraining)
        {
            // Khi training: random position trong khoảng x: (-1.8, 1.8), z: (-1.1, 1.1)
            float randomX = Random.Range(-1.8f, 1.8f);
            float randomZ = Random.Range(-1.1f, 1.1f);
            Vector3 randomBallPosition = new Vector3(randomX, ballStartPosition.y, randomZ);
            
            ball.localPosition = randomBallPosition;
        }
        else
        {
            ball.localPosition = ballStartPosition;
        }
        
        ballRb.linearVelocity = Vector3.zero;
        ballRb.angularVelocity = Vector3.zero;
    }
} 