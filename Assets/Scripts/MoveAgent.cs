using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class MoveAgent : Agent
{
    public float moveSpeed = 5f;
    private Rigidbody2D rb;
    public Transform goal;

    public override void Initialize()
    {
        rb = GetComponent<Rigidbody2D>();
        Debug.Log("Agent initialized");
    }

    public override void OnEpisodeBegin()
    {
        Debug.Log("Episode initialized");
        // Reset position each episode
        transform.localPosition = Vector3.zero;
        rb.linearVelocity = Vector2.zero;

        transform.localPosition = new Vector3(
            Random.Range(-4f, 4f),
            0.5f,
            Random.Range(-4f, 4f)
        );

        Debug.Log("Episode started, goal at " + goal.localPosition);
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        Debug.Log("Observations Collected");
        Vector3 toGoal = goal.localPosition - transform.localPosition;
        sensor.AddObservation(toGoal);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        Debug.Log("Step called");
        float moveX = actions.ContinuousActions[0];
        float moveY = actions.ContinuousActions[1];

        rb.linearVelocity = new Vector2(moveX, moveY) * moveSpeed;

        Debug.Log($"Step called: moveX={moveX}, moveY={moveY}");
        float distanceToGoal = Vector3.Distance(transform.localPosition, goal.localPosition);

        AddReward(0.01f * (1f - distanceToGoal / 10f));
    }
}