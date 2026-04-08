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

        goal.localPosition = new Vector3(
            Random.Range(-4f, 4f),
            0.5f,
            Random.Range(-4f, 4f)
        );

        Debug.Log("Episode started, goal at " + goal.localPosition);
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        Debug.Log("Observations Collected");
        // Observe the agent’s position
        // sensor.AddObservation(transform.localPosition);
        // sensor.AddObservation(goal.localPosition);
        Vector3 toGoal = goal.localPosition - transform.localPosition;
        sensor.AddObservation(toGoal);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        Debug.Log("Step called");
        // If no actions from Python, fallback to random actions
        float moveX = actions.ContinuousActions[0];
        float moveY = actions.ContinuousActions[1];

        rb.linearVelocity = new Vector2(moveX, moveY) * moveSpeed;

        Debug.Log($"Step called: moveX={moveX}, moveY={moveY}");
        AddReward(-0.001f); // tiny negative reward per step to encourage movement
    }

    // public override void OnActionReceived(ActionBuffers actions)
    // {
    //     float moveX = actions.ContinuousActions[0];
    //     float moveY = actions.ContinuousActions[1];

    //     rb.linearVelocity = new Vector2(moveX, moveY) * moveSpeed;

    //     // Small reward for staying alive / moving
    //     AddReward(2f);
    //     // float distanceToGoal = Vector3.Distance(transform.localPosition, goal.localPosition);

    //     // // Reward for moving closer to the goal
    //     // AddReward(0.01f * (1f - distanceToGoal / 10f));
    //     Debug.Log("Reward given for moving: " + 2f);
    // }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        Debug.Log(" Heuristics initialized");
        var c = actionsOut.ContinuousActions;
        c[0] = Input.GetAxisRaw("Horizontal");
        c[1] = Input.GetAxisRaw("Vertical");
    }

    // private void OnTriggerEnter2D(Collider2D collision)
    // {
    //     if (collision.CompareTag("Player"))
    //     {
    //         AddReward(1f);
    //         Debug.Log("Stopped");
    //         EndEpisode();
    //     }
    // }   

    // private void OnCollisionEnter2D(Collision2D collision)
    // {
    //     if (collision.gameObject.CompareTag("Wall"))
    //     {
    //         AddReward(-0.5f);
    //     }
    // }
}