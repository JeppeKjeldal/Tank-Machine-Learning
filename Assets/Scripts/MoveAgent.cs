using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class MoveAgent : Agent
{
    public float moveSpeed = 5f;
    private Rigidbody2D rb;
    public Transform goal;
    private float lastDistance;

    public override void Initialize()
    {
        rb = GetComponent<Rigidbody2D>();
        // Debug.Log("Agent initialized");
    }

    public override void OnEpisodeBegin()
    {
        // Debug.Log("Episode initialized");
        // Reset position each episode
        lastDistance = Vector3.Distance(transform.localPosition, goal.localPosition);
        transform.localPosition = Vector3.zero;
        rb.linearVelocity = Vector2.zero;

        transform.localPosition = new Vector3(
            Random.Range(-4f, 4f),
            0.5f,
            Random.Range(-4f, 4f)
        );

        // Debug.Log("Episode started, goal at " + goal.localPosition);
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // Debug.Log("Observations Collected");
        // Observe the agent’s position
        sensor.AddObservation(transform.localPosition);
        sensor.AddObservation(goal.localPosition);
        Vector3 toGoal = goal.localPosition - transform.localPosition;
        sensor.AddObservation(toGoal);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        float moveX = actions.ContinuousActions[0];
        float moveY = actions.ContinuousActions[1];

        rb.linearVelocity = new Vector2(moveX, moveY) * moveSpeed;

        float distance = Vector3.Distance(transform.localPosition, goal.localPosition);

        // Dense reward: reward reduction in distance since last step
        float progress = lastDistance - distance;
        AddReward(progress * 2f);
        lastDistance = distance;

        // Optional small penalty for existing (stops idling)
        AddReward(-0.001f);

        // Success condition
        if (distance < 0.5f)
        {
            AddReward(5f);
            EndEpisode();
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            AddReward(1000f);
            Debug.Log("collided with player");
            EndEpisode();
        }

        if (collision.gameObject.CompareTag("Wall"))
        {
            AddReward(-1000f);
            Debug.Log("collided with wall, ending.");
            EndEpisode();
        }
    }   
}